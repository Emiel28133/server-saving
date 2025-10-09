/**
 * Secure Server – JWT auth + AES-256-GCM encrypted player profiles per user
 * Features:
 *  - Users table (username + bcrypt password hash)
 *  - Players table (user_id + normalized profile name + encrypted JSON {money, level})
 *  - Registration/Login -> JWT (2h expiry)
 *  - Save / Load / List profiles (auth required)
 *  - Profile name normalization (case-insensitive, spaces allowed, safe charset)
 *  - Optional claim endpoint (if you later add legacy/migrated rows)
 *  - dotenv for secrets (ENCRYPTION_KEY + JWT_SECRET)
 *
 * IMPORTANT:
 *  1. Create a .env file (copy from .env.example).
 *  2. Use a 64 hex char ENCRYPTION_KEY (32 bytes).
 *  3. Keep ENCRYPTION_KEY stable or you won't be able to decrypt existing data.
 */

require('dotenv').config(); // Load environment variables from .env early

const express = require("express");
const sqlite3 = require("sqlite3").verbose();
const cors = require("cors");
const bcrypt = require("bcrypt");
const jwt = require("jsonwebtoken");
const crypto = require("crypto");

const app = express();
app.use(express.json());
app.use(cors());

// -----------------------------------------------------------------------------
// Secrets & Keys
// -----------------------------------------------------------------------------
const JWT_SECRET = process.env.JWT_SECRET || "dev_jwt_secret_change_me_immediately";

// Load ENCRYPTION_KEY from env (hex). Must be 32 bytes.
let ENCRYPTION_KEY;
(function initEncryptionKey() {
    const hex = process.env.ENCRYPTION_KEY;
    if (!hex) {
        ENCRYPTION_KEY = crypto.randomBytes(32);
        console.warn("WARNING: Using RANDOM ENCRYPTION_KEY (data unreadable after restart).");
        console.warn("Generate one: node -e \"console.log(require('crypto').randomBytes(32).toString('hex'))\"");
        console.warn(`Example to put in .env: ENCRYPTION_KEY=${ENCRYPTION_KEY.toString("hex")}`);
        return;
    }
    try {
        ENCRYPTION_KEY = Buffer.from(hex, "hex");
        if (ENCRYPTION_KEY.length !== 32) {
            throw new Error("Bad length");
        }
    } catch {
        ENCRYPTION_KEY = crypto.randomBytes(32);
        console.warn("INVALID ENCRYPTION_KEY supplied. Generated a random temporary one.");
    }
})();

// -----------------------------------------------------------------------------
// SQLite Setup
// -----------------------------------------------------------------------------
const db = new sqlite3.Database("playerdata.db");

// Create / normalize schema
function initSchema() {
    db.serialize(() => {
        db.run(`CREATE TABLE IF NOT EXISTS users (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      username TEXT UNIQUE,
      password_hash TEXT
    )`);

        db.run(`CREATE TABLE IF NOT EXISTS players (
      id INTEGER PRIMARY KEY AUTOINCREMENT,
      user_id INTEGER,
      name TEXT,
      data TEXT,
      UNIQUE(user_id, name)
    )`);

        // One-off normalization for existing names (lowercase + replace '+' with space + trim)
        db.run(
            `UPDATE players
         SET name = lower(replace(trim(name), '+', ' '))
       WHERE name LIKE '%+%' OR name != lower(name) OR name != trim(name)`
        );
    });
}
initSchema();

// -----------------------------------------------------------------------------
// Helpers (encryption, normalization, auth)
// -----------------------------------------------------------------------------
function encrypt(plain) {
    const iv = crypto.randomBytes(16);
    const cipher = crypto.createCipheriv("aes-256-gcm", ENCRYPTION_KEY, iv);
    let enc = cipher.update(plain, "utf8", "hex");
    enc += cipher.final("hex");
    const tag = cipher.getAuthTag();
    return `${iv.toString("hex")}:${tag.toString("hex")}:${enc}`;
}

function decrypt(blob) {
    const [ivHex, tagHex, dataHex] = blob.split(":");
    const iv = Buffer.from(ivHex, "hex");
    const tag = Buffer.from(tagHex, "hex");
    const decipher = crypto.createDecipheriv("aes-256-gcm", ENCRYPTION_KEY, iv);
    decipher.setAuthTag(tag);
    let dec = decipher.update(dataHex, "hex", "utf8");
    dec += decipher.final("utf8");
    return dec;
}

// Normalize profile names: handle '+', decode URI components, trim, lowercase, restrict charset
function normalizeName(raw) {
    if (typeof raw !== "string") return null;
    let s = raw.replace(/\+/g, " ");
    try { s = decodeURIComponent(s); } catch { /* ignore bad encoding */ }
    s = s.trim().toLowerCase();
    if (s.length === 0 || s.length > 32) return null;
    // Allow letters, digits, spaces, underscore, hyphen
    if (!/^[a-z0-9 _-]+$/.test(s)) return null;
    return s;
}

function authenticate(req, res, next) {
    const header = req.headers.authorization;
    if (!header) return res.status(401).json({ error: "Missing token" });
    const parts = header.split(" ");
    if (parts.length !== 2 || parts[0] !== "Bearer") {
        return res.status(401).json({ error: "Invalid auth header" });
    }
    try {
        const payload = jwt.verify(parts[1], JWT_SECRET);
        req.user = payload; // { user_id, username, iat, exp }
        next();
    } catch {
        return res.status(401).json({ error: "Invalid token" });
    }
}

// -----------------------------------------------------------------------------
// Routes
// -----------------------------------------------------------------------------
app.get("/", (_req, res) => {
    res.json({ ok: true, service: "server-saving", version: "secure-1.2" });
});

// Register new user
app.post("/auth/register", async (req, res) => {
    const { username, password } = req.body || {};
    if (!username || !password) return res.status(400).json({ error: "Missing credentials" });
    try {
        const hash = await bcrypt.hash(password, 12);
        db.run(
            "INSERT INTO users (username, password_hash) VALUES (?, ?)",
            [username.trim(), hash],
            function (err) {
                if (err) return res.status(400).json({ error: "Username taken" });
                res.json({ success: true });
            }
        );
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
        const token = jwt.sign(
            { user_id: user.id, username: user.username },
            JWT_SECRET,
            { expiresIn: "2h" }
        );
        res.json({ token });
    });
});

// Save / update encrypted player data
app.post("/player/:name", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    const nameKey = normalizeName(req.params.name);
    if (!nameKey) return res.status(400).json({ error: "Invalid profile name" });

    let { money, level } = req.body || {};
    money = Number.isFinite(+money) ? +money : 0;
    level = Number.isFinite(+level) ? +level : 1;

    const encrypted = encrypt(JSON.stringify({ money, level }));

    db.run(
        `INSERT INTO players (user_id, name, data)
     VALUES (?, ?, ?)
     ON CONFLICT(user_id, name) DO UPDATE SET data=excluded.data`,
        [user_id, nameKey, encrypted],
        (err) => {
            if (err) return res.status(500).json({ error: err.message });
            console.log(`Saved profile: user_id=${user_id} name=${nameKey}`);
            res.json({ success: true });
        }
    );
});

// Load a player profile (decrypt)
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
                res.status(500).json({ error: "Decryption error" });
            }
        }
    );
});

// List all profile names for this user
app.get("/players", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    db.all(
        "SELECT name FROM players WHERE user_id = ? ORDER BY name",
        [user_id],
        (err, rows) => {
            if (err) return res.status(500).json({ error: err.message });
            res.json(rows.map(r => r.name));
        }
    );
});

// (Optional) Delete a profile
app.delete("/player/:name", authenticate, (req, res) => {
    const user_id = req.user.user_id;
    const nameKey = normalizeName(req.params.name);
    if (!nameKey) return res.status(400).json({ error: "Invalid profile name" });

    db.run(
        "DELETE FROM players WHERE user_id = ? AND name = ?",
        [user_id, nameKey],
        function (err) {
            if (err) return res.status(500).json({ error: err.message });
            if (this.changes === 0) return res.status(404).json({ error: "Not found" });
            res.json({ success: true, deleted: nameKey });
        }
    );
});

// Start server
const PORT = process.env.PORT || 3000;
app.listen(PORT, () => {
    console.log(`Server running on http://localhost:${PORT}`);
});