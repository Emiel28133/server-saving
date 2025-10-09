const express = require("express");
const sqlite3 = require("sqlite3").verbose();
const cors = require("cors");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const crypto = require("crypto");

const app = express();
app.use(express.json());
app.use(cors());

// Secrets (use env vars in production)
const JWT_SECRET = process.env.JWT_SECRET || "dev_jwt_secret_change_me";

// Stable 32-byte encryption key (hex via ENCRYPTION_KEY env recommended)
let ENCRYPTION_KEY = process.env.ENCRYPTION_KEY
    ? Buffer.from(process.env.ENCRYPTION_KEY, "hex")
    : crypto.randomBytes(32);
if (!process.env.ENCRYPTION_KEY) {
    console.warn("WARNING: Using random ENCRYPTION_KEY for this run. Set ENCRYPTION_KEY env to persist encrypted data.");
    console.warn(`Example (save safely): ENCRYPTION_KEY=${ENCRYPTION_KEY.toString("hex")}`);
}

const db = new sqlite3.Database("playerdata.db");

// Crypto helpers
function encrypt(text) {
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipheriv("aes-256-gcm", ENCRYPTION_KEY, iv);
    let encrypted = cipher.update(text, "utf8", "hex");
    encrypted += cipher.final("hex");
    const tag = cipher.getAuthTag();
    return `${iv.toString("hex")}:${tag.toString("hex")}:${encrypted}`;
}
function decrypt(data) {
    const [ivHex, tagHex, encrypted] = data.split(":");
    const iv = Buffer.from(ivHex, "hex");
    const tag = Buffer.from(tagHex, "hex");
    const decipher = crypto.createDecipheriv("aes-256-gcm", ENCRYPTION_KEY, iv);
    decipher.setAuthTag(tag);
    let decrypted = decipher.update(encrypted, "hex", "utf8");
    decrypted += decipher.final("utf8");
    return decrypted;
}

// Normalize profile names: '+' -> space, URL-decode, trim, lowercase, basic charset check
function normalizeName(raw) {
    if (typeof raw !== "string") return null;
    // Treat '+' as space for backward compatibility, then decode percent-encoding
    let s = raw.replace(/\+/g, " ");
    try { s = decodeURIComponent(s); } catch { /* keep as-is if badly encoded */ }
    s = s.trim().toLowerCase();
    if (s.length === 0 || s.length > 32) return null;
    // Allowed: letters, numbers, space, underscore, hyphen
    if (!/^[a-z0-9 _-]+$/.test(s)) return null;
    return s;
}

// Init schema and migrate
function initSchema() {
    db.serialize(() => {
        db.run(`CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT UNIQUE,
      password_hash TEXT
    )`);

        // Create secure players table if missing
        db.run(`CREATE TABLE IF NOT EXISTS players (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER,
      name TEXT,
      data TEXT,
      UNIQUE(user_id, name)
    )`);

        // One-off normalization: convert '+' to spaces and lowercase/trim existing names
        db.run(
            `UPDATE players
       SET name = lower(replace(trim(name), '+', ' '))
       WHERE name LIKE '%+%' OR name != lower(name) OR name != trim(name)`
        );
    });
}
initSchema();

// Auth middleware
function authenticate(req, res, next) {
    const auth = req.headers.authorization;
    if (!auth) return res.status(401).json({ error: "Missing token" });
    const parts = auth.split(" ");
    if (parts.length !== 2 || parts[0] !== "Bearer") return res.status(401).json({ error: "Invalid auth header" });
    try {
        const payload = jwt.verify(parts[1], JWT_SECRET);
        req.user = payload;
        next();
    } catch {
        return res.status(401).json({ error: "Invalid token" });
    }
}

app.get("/", (_req, res) => res.json({ ok: true, service: "server-saving", version: "secure-1.1" }));

// Register
app.post("/auth/register", async (req, res) => {
    const { username, password } = req.body || {};
    if (!username || !password) return res.status(400).json({ error: "Missing credentials" });
    try {
        const hash = await bcrypt.hash(password, 12);
        db.run("INSERT INTO users (username, password_hash) VALUES (?, ?)", [username.trim(), hash], function (err) {
            if (err) return res.status(400).json({ error: "Username taken" });
            res.json({ success: true });
        });
    } catch {
        res.status(500).json({ error: "Registration error" });
    }
});

// Login
app.post("/auth/login", (req, res) => {
    const { username, password } = req.body || {};
    if (!username || !password) return res.status(400).json({ error: "Missing credentials" });

    db.get("SELECT * FROM users WHERE username = ?", [username.trim()], async (err, user) => {
        if (err || !user) return res.status(401).json({ error: "Invalid credentials" });
        const valid = await bcrypt.compare(password, user.password_hash);
        if (!valid) return res.status(401).json({ error: "Invalid credentials" });
        const token = jwt.sign({ user_id: user.id, username: user.username }, JWT_SECRET, { expiresIn: "2h" });
        res.json({ token });
    });
});

// Save (encrypted) - requires login
app.post("/player/:name", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    const nameKey = normalizeName(req.params.name);
    if (!nameKey) return res.status(400).json({ error: "Invalid profile name" });

    let { money, level } = req.body || {};
    money = Number.isFinite(+money) ? +money : 0;
    level = Number.isFinite(+level) ? +level : 1;

    const payload = encrypt(JSON.stringify({ money, level }));

    db.run(
        `INSERT INTO players (user_id, name, data)
     VALUES (?, ?, ?)
     ON CONFLICT(user_id, name) DO UPDATE SET data=excluded.data`,
        [user_id, nameKey, payload],
        (err) => {
            if (err) return res.status(500).json({ error: err.message });
            console.log(`Saved user_id=${user_id} name=${nameKey}`);
            res.json({ success: true });
        }
    );
});

// Load (decrypt) - requires login
app.get("/player/:name", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    const nameKey = normalizeName(req.params.name);
    if (!nameKey) return res.status(400).json({ error: "Invalid profile name" });

    db.get(
        "SELECT data FROM players WHERE user_id = ? AND name = ?",
        [user_id, nameKey],
        (err, row) => {
            if (err) return res.status(500).json({ error: err.message });
            if (!row) return res.status(404).json({ error: "Not found" });
            try {
                const data = JSON.parse(decrypt(row.data));
                res.json(data);
            } catch {
                return res.status(500).json({ error: "Decryption error" });
            }
        }
    );
});

// List profiles for the logged-in user (already normalized)
app.get("/players", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    db.all("SELECT name FROM players WHERE user_id = ? ORDER BY name", [user_id], (err, rows) => {
        if (err) return res.status(500).json({ error: err.message });
        res.json(rows.map((r) => r.name));
    });
});

app.listen(3000, () => console.log("Server running on http://localhost:3000"));