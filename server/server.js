const express = require("express");
const sqlite3 = require("sqlite3").verbose();
const app = express();
app.use(express.json());

const db = new sqlite3.Database("playerdata.db");

db.serialize(() => {
    db.run(`CREATE TABLE IF NOT EXISTS players (
    name TEXT PRIMARY KEY,
    money INTEGER,
    level INTEGER
  )`);
});

// GET speler
app.get("/player/:name", (req, res) => {
    const { name } = req.params;
    db.get("SELECT * FROM players WHERE name = ?", [name], (err, row) => {
        if (err) return res.status(500).json({ error: err.message });

        if (!row) {
            // Nieuwe speler aanmaken met default data
            const defaultPlayer = { name, money: 0, level: 1 };
            db.run("INSERT INTO players (name, money, level) VALUES (?, ?, ?)",
                [name, defaultPlayer.money, defaultPlayer.level],
                (err2) => {
                    if (err2) return res.status(500).json({ error: err2.message });
                    res.json(defaultPlayer);
                }
            );
        } else {
            res.json(row);
        }
    });
});

// POST speler (opslaan)
app.post("/player/:name", (req, res) => {
    const { name } = req.params;
    const { money, level } = req.body;

    db.run(
        "INSERT INTO players (name, money, level) VALUES (?, ?, ?) ON CONFLICT(name) DO UPDATE SET money=excluded.money, level=excluded.level",
        [name, money, level],
        (err) => {
            if (err) return res.status(500).json({ error: err.message });
            res.json({ message: "Saved", player: { name, money, level } });
        }
    );
});

app.listen(3000, () => console.log("Server running on http://localhost:3000"));
