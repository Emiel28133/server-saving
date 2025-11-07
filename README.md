# 🔐 Server Saving — Secure Game Save System

Een veilige backend voor het centraal opslaan en laden van game-progressie, gebouwd met Unity (C#) en Node.js. Ontwikkeld tijdens het vak **Verdieping Software**.

## ✨ Features

- **JWT Authenticatie**: Veilige login met 2 uur token expiratie
- **Data Encryptie**: AES-256-GCM voor data-at-rest beveiliging
- **Wachtwoord Hashing**: bcrypt met 12 salt rounds
- **Profile Management**: Meerdere game-profielen per gebruiker
- **Unity Client**: TextMeshPro UI met volledige error handling
- **Cross-platform**: Werkt op Windows, macOS, Linux

## 🏗️ Tech Stack

**Frontend (Unity Client):**
- Unity 2021.3+ LTS
- C# met UnityWebRequest
- TextMeshPro (TMP)

**Backend (Server):**
- Node.js + Express
- SQLite database
- JWT (jsonwebtoken)
- bcrypt + crypto (AES-256-GCM)

---

## 🚀 Installatie

### 1️⃣ Server Setup

```bash
# Clone repository
git clone https://github.com/Emiel28133/server-saving.git
cd server-saving/server

# Install dependencies
npm install

# Create .env file
touch .env
```

Vul `.env` in met:
```env
ENCRYPTION_KEY=<genereer-64-hex-characters>
JWT_SECRET=<random-string>
PORT=3000
```

> **Genereer een veilige ENCRYPTION_KEY:**
> ```bash
> node -e "console.log(require('crypto').randomBytes(32).toString('hex'))"
> ```

Start de server:
```bash
npm start
# Output: Server running on http://localhost:3000
```

### 2️⃣ Unity Setup

1. Open het Unity project (`server-saving/`)
2. Open scene: `Assets/Scenes/SampleScene.unity`
3. Selecteer het GameObject met `SecureServerClientTMP` component
4. In de Inspector: stel **Base URL** in op `http://127.0.0.1:3000`
5. Klik Play!

---

## 🎮 Gebruik

### In Play Mode (Unity):

1. **Register**: Vul username + password in → Klik "Register"
2. **Login**: Klik "Login" → Zie "Login success!"
3. **Save**: Vul profile name, money, level in → Klik "Save"
4. **Load**: Klik "Load" → Data verschijnt in velden
5. **List**: Klik "List Profiles" → Zie alle profielen in console

### API Endpoints:

| Method | Endpoint | Beschrijving |
|--------|----------|-------------|
| POST | `/auth/register` | Nieuwe gebruiker aanmaken |
| POST | `/auth/login` | Inloggen (krijgt JWT token) |
| POST | `/player/:name` | Profiel opslaan (auth vereist) |
| GET | `/player/:name` | Profiel laden (auth vereist) |
| GET | `/players` | Alle profielen van user (auth vereist) |
| DELETE | `/player/:name` | Profiel verwijderen (auth vereist) |

---



## 🔒 Security Features

### Data-at-Rest Encryptie
```javascript
// AES-256-GCM met IV + Authentication Tag
function encrypt(plaintext) {
  const iv = crypto.randomBytes(16);
  const cipher = crypto.createCipheriv('aes-256-gcm', ENCRYPTION_KEY, iv);
  let encrypted = cipher.update(plaintext, 'utf8', 'hex');
  encrypted += cipher.final('hex');
  const tag = cipher.getAuthTag();
  return `${iv.toString('hex')}:${tag.toString('hex')}:${encrypted}`;
}
```

### JWT Sessies
- 2 uur expiratie
- Bearer token in `Authorization` header
- Stateless (geen session storage op server)

### Wachtwoord Beveiliging
- bcrypt hashing (12 rounds)
- Nooit plaintext in database

---

## 🐛 Troubleshooting

| Probleem | Oplossing |
|----------|----------|
| "Connection refused" | Server draait niet → `cd server && npm start` |
| "401 Unauthorized" | Token expired (2h) → Login opnieuw |
| "404 Not Found" | Controleer Base URL in Unity Inspector |
| "Invalid profile name" | Gebruik alleen letters, cijfers, spaties, `-`, `_` |
| Data onleesbaar na restart | Gebruik vaste `ENCRYPTION_KEY` in `.env` |

---



**Made with ☕ during Verdieping Software 2024-2025**
