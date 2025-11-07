## Het probleem
Tijdens het vak Verdieping Software wilde ik game-progressie **centraal en veilig** opslaan. Lokale saves werken niet over devices heen en zijn kwetsbaar. Mijn oplossing: een REST API met authenticatie en versleutelde database.

## Technische keuzes
- **Backend:** Node.js/Express (lightweight, snel te prototypen).
- **Database:** SQLite (simpel, geen aparte server nodig).
- **Auth:** JWT (stateless, 2 uur expiratie).
- **Encryptie:** AES-256-GCM voor data-at-rest; sleutel via `.env`.
- **Client:** Unity (C#) met UnityWebRequest en TMP UI.

## Architectuur
```
Unity → HTTPS → Express API → SQLite
         ↓
      JWT verify → AES decrypt
```

## Uitdagingen & oplossingen

### 1. Profielnamen met spaties/speciale tekens gaven 404
**Probleem:** Unity stuurde `"Profile+Name"` → server zocht naar letterlijk `"profile+name"`.  
**Oplossing:** Normalisatie-functie:
- Trim, lowercase, URL-decode, `+` → spatie.
- Max 32 chars, regex whitelist `[a-z0-9 _-]`.

### 2. Data onleesbaar na server-restart
**Probleem:** Random `ENCRYPTION_KEY` bij elke start.  
**Oplossing:** Vaste key in `.env` (64 hex = 32 bytes). `.env.example` toegevoegd met instructies.

### 3. Onduidelijke fouten in Unity UI
**Oplossing:** `infoText` met herstelstappen:
```csharp
if (response.error.Contains("401")) {
    infoText.text = "<color=red>Token expired. Please login again.</color>";
}
```

## Security best practices
- **Wachtwoorden:** bcrypt (12 rounds), nooit plaintext.
- **Sessies:** JWT in `Authorization: Bearer <token>`, geen localStorage in web (hier Unity PlayerPrefs, relatief veilig).
- **Versleuteling:** AES-256-GCM met IV + auth tag → beschermt tegen tampering.
- **Secrets:** `.gitignore` blokkeert `.env`, DB, certs; `.env.example` documenteert vereiste variabelen.

## Code highlight: Encrypt/decrypt
```js
function encrypt(plaintext) {
  const iv = crypto.randomBytes(16);
  const cipher = crypto.createCipheriv('aes-256-gcm', ENCRYPTION_KEY, iv);
  let encrypted = cipher.update(plaintext, 'utf8', 'hex');
  encrypted += cipher.final('hex');
  const tag = cipher.getAuthTag();
  return `${iv.toString('hex')}:${tag.toString('hex')}:${encrypted}`;
}
```

## Resultaat
- ✅ Centrale, veilige opslag per gebruiker.
- ✅ Robuuste naamafhandeling (geen 404's meer).
- ✅ Duidelijke UX bij fouten.
- ✅ Definition of Done: getest op 2 machines, gedocumenteerd, secrets safe.


## Links
- [GitHub Repo](https://github.com/Emiel28133/server-saving)
- [Trello Board](https://trello.com/b/YZ5GvW4C/verdieping-software)
