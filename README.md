<div align="center">

# 🎬 THE DIRECTOR'S CUT

**An Among Us host-only mod — the first player to die becomes the Director.**
**Un mod Among Us *host-only* — le premier joueur éliminé devient le Réalisateur.**

`BepInEx 6 (IL2CPP)` · `HarmonyX 2.4.2` · `.NET 6` · `Vanilla-client compatible`

</div>

---

🇬🇧 **English** · [🇫🇷 Version française](#-version-française)

---

## 🎥 What is this? / Le concept

When the **first player dies**, they don't leave — they take over as the **Director**. From beyond the grave they stop playing Among Us and start *directing* it.

**The key point:** only the **host** installs the mod. Everyone else — including the Director — plays on a completely **unmodded (vanilla) client**. The mod intercepts chat commands on the host and replicates effects to all players using vanilla network calls.

---

## ✅ Requirements

| | |
|---|---|
| **Game** | Among Us (Steam recommended — 32-bit / x86 build) |
| **Loader** | BepInEx **6.x IL2CPP** (`x86` build to match Among Us) |
| **Who installs** | **Host only.** Other players need nothing. |
| **OS** | Windows (tested) |

> ⚠️ Among Us is a **32-bit** app — download the **`win-x86`** BepInEx IL2CPP build, not x64.

---

## 📦 Installation (host)

### 1. Install BepInEx 6 (IL2CPP)
1. Download the latest **BepInEx 6 IL2CPP `win-x86`** build from the official releases:
   👉 https://github.com/BepInEx/BepInEx/releases (bleeding-edge IL2CPP builds)
2. Open your Among Us install folder (the one containing `Among Us.exe`):
   - **Steam:** right-click the game → *Manage* → *Browse local files*.
3. Extract the **entire contents** of the BepInEx zip directly into that folder. You should now see a `BepInEx/`, `doorstop_config.ini` and `winhttp.dll` next to `Among Us.exe`.

### 2. Generate the interop assemblies
1. **Launch Among Us once.** A console window opens and BepInEx generates the IL2CPP interop DLLs (this can take a minute).
2. Wait until the game reaches the main menu, then **close it**.
   - This creates `BepInEx/interop/` — required for the mod to load.

### 3. Install the mod
1. Drop **`AU_TheDirectorsCut.dll`** into:
   ```
   <Among Us>/BepInEx/plugins/
   ```
2. **Launch Among Us.** In the BepInEx console you should see:
   ```
   [DirectorCore] Initialisé.
   [NetworkManager] Initialisé.
   ```

### 4. Play
1. **Host a lobby** (you must be the host for the mod to do anything).
2. New players receive a private welcome message automatically.
3. Start a game. The **first death** becomes the Director. 🎬

---

## 🛠️ Build from source (developers)

Requires the **.NET 6 SDK**.

1. Point the build at your Among Us install. Either set an environment variable:
   ```bash
   setx AmongUsPath "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
   ```
   …or pass it on the command line (step 3).
2. Make sure you've launched the game once with BepInEx so `BepInEx/interop/` exists (the `.csproj` references those DLLs).
3. Build:
   ```bash
   dotnet build -c Release /p:AmongUsPath="C:\chemin\vers\Among Us"
   ```
4. On success the DLL is **auto-copied** to `BepInEx/plugins/` (see the `PostBuild` target). Output:
   ```
   ✅ DLL copié dans BepInEx/plugins/
   ```

**Dependencies** (`.csproj`): `BepInEx.Unity.IL2CPP 6.0.0-be.697`, `Lib.Harmony 2.4.2`, `Il2CppInterop.Runtime 1.5.1`.

---

## 🎮 Rules

- The **first player to die** becomes the **Director** — **one per game**, locked in (irreversible until the next game).
- Only the **host** runs the mod; everyone else is **vanilla**.
- **Public commands** work for anyone, any time.
- **Director directives**:
  - `/randomcolors`, `/cut`, `/darkness`, `/freeze` work for the Director **only**, **in-game only** (ignored in the lobby).
  - `/action`, `/loc`, `/vote` work for the Director **only**, **in-meeting only**.
- Every directive has a **cooldown**; if it's recharging, the remaining time is announced.

---

## ⌨️ Commands

Players are identified by **numbers** (1, 2, 3…) — run `/players` to see them.

### 🟢 Public (everyone)
| Command | Effect |
|---|---|
| `/welcome` | Welcome message |
| `/help` | Command list |
| `/gg` | Previous-game stats (alive / eliminated) |
| `/players` | List players with their number IDs |
| `/hrandomcolors` | Detailed help for `/randomcolors` |
| `/hcut` | Detailed help for `/cut` |
| `/hdarkness` | Detailed help for `/darkness` |
| `/hfreeze` | Detailed help for `/freeze` |
| `/haction` | Detailed help for `/action` |
| `/hloc` | Detailed help for `/loc` |
| `/hvote` | Detailed help for `/vote` |

### 🎬 Director directives
| Command | Effect | Duration | Cooldown |
|---|---|---|---|
| `/randomcolors` | Random unique color for all | instant | 20s |
| `/cut` | Reactor sabotage alert (2s), then no-movement freeze (5s) — anyone who moves dies! | 7s total | 30s |
| `/darkness` | Total darkness across the entire map | 10s | 35s |
| `/freeze ID` | Freezes the target player in place | 8s | 30s |
| `/action ID [A-D]` | Assign a basic secret script to a player! Scripts: A = NoReport (don't report bodies), B = SkipVote (skip the next vote), C = NoVents (don't use vents), D = VoteFirst (vote first this round). Players who disobey get eliminated! | Varies | 20s |
| `/loc ID_player ID_zone` | Forbid a player from entering a specific zone (The Skeld only). Zone IDs: 1=Cafeteria, 2=Admin, 3=Electrical,4=Storage,5=Security,6=Reactor,7=UpperEngine,8=LowerEngine,9=Medbay,10=ElectricalHallway,11=Communications,12=Shields,13=O2,14=Navigation,15=Weapons | Until round end | 20s |
| `/vote ID_player ID_target` | Force a player to vote for a specific target. | Until next meeting | 20s |

---

## ⚙️ Configuration (`DirectorOptions`)

| Option | Default | Description |
|---|---|---|
| `AnnounceInChat` | `true` | Relay actions in public chat |
| `AntiKick` | `true` | Anti-kick chat throttle (keep on) |
| `MessageWait` | `0.6s` | Delay between chat messages |

---

## 🧩 How it works (technical)

- **Host-only, vanilla-compatible**: the mod only patches the host's game (HarmonyX) and replicates effects with **vanilla RPCs** (`SendChat`, `SetName`, `SetColor`). Clients need no mod.
- **Chat as the channel**: all communication goes through chat. A queued, rate-limited **pump** (`ChatManager`) rides the game's native chat timer to avoid the server's anti-spam kick.
- **Colored vs plain text**: the network gets clean plain text; a local color map re-injects `<color>` tags on the host's screen.
- **"System" announcements**: the lowest-ID living player is briefly renamed `[ The Director's Cut ]` to sign official messages.
- **End-of-game snapshot**: alive/dead lists are captured on `ShipStatus.OnDestroy` to feed `/gg`.

---
---

<a name="-version-française"></a>
# 🇫🇷 Version française

## 🎥 Le concept

Quand le **premier joueur meurt**, il ne quitte pas la partie — il devient le **Réalisateur**. Depuis l'au-delà, il ne joue plus à Among Us, il le *met en scène*.

**Le point clé :** seul l'**hôte** installe le mod. Tous les autres — y compris le Réalisateur — jouent sur un client **totalement vanilla (non moddé)**. Le mod intercepte les commandes côté hôte et réplique les effets à tous via des appels réseau vanilla.

---

## ✅ Prérequis

| | |
|---|---|
| **Jeu** | Among Us (Steam recommandé — version 32 bits / x86) |
| **Loader** | BepInEx **6.x IL2CPP** (build `x86` pour coller à Among Us) |
| **Qui installe** | **L'hôte seulement.** Les autres joueurs n'ont rien à faire. |
| **OS** | Windows (testé) |

> ⚠️ Among Us est une appli **32 bits** — télécharge le build BepInEx IL2CPP **`win-x86`**, pas le x64.

---

## 📦 Installation (hôte)

### 1. Installer BepInEx 6 (IL2CPP)
1. Télécharge le dernier build **BepInEx 6 IL2CPP `win-x86`** depuis les releases officielles :
   👉 https://github.com/BepInEx/BepInEx/releases (builds *bleeding-edge* IL2CPP)
2. Ouvre le dossier d'installation d'Among Us (celui qui contient `Among Us.exe`) :
   - **Steam :** clic droit sur le jeu → *Gérer* → *Parcourir les fichiers locaux*.
3. Extrais **tout le contenu** du zip BepInEx directement dans ce dossier. Tu dois voir apparaître `BepInEx/`, `doorstop_config.ini` et `winhttp.dll` à côté de `Among Us.exe`.

### 2. Générer les assemblies interop
1. **Lance Among Us une fois.** Une console s'ouvre et BepInEx génère les DLL interop IL2CPP (ça peut prendre une minute).
2. Attends le menu principal, puis **ferme le jeu**.
   - Cela crée `BepInEx/interop/` — indispensable au chargement du mod.

### 3. Installer le mod
1. Place **`AU_TheDirectorsCut.dll`** dans :
   ```
   <Among Us>/BepInEx/plugins/
   ```
2. **Lance Among Us.** Dans la console BepInEx, tu dois voir :
   ```
   [DirectorCore] Initialisé.
   [NetworkManager] Initialisé.
   ```

### 4. Jouer
1. **Héberge un lobby** (tu dois être l'hôte pour que le mod agisse).
2. Les nouveaux joueurs reçoivent un message de bienvenue privé automatiquement.
3. Lance une partie. La **première mort** devient le Réalisateur. 🎬

---

## 🛠️ Compiler depuis les sources (développeurs)

Nécessite le **SDK .NET 6**.

1. Indique au build ton installation d'Among Us. Soit via une variable d'environnement :
   ```bash
   setx AmongUsPath "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
   ```
   …soit en ligne de commande (étape 3).
2. Assure-toi d'avoir lancé le jeu au moins une fois avec BepInEx pour que `BepInEx/interop/` existe (le `.csproj` référence ces DLLs).
3. Compile :
   ```bash
   dotnet build -c Release /p:AmongUsPath="C:\chemin\vers\Among Us"
   ```
4. En cas de succès, la DLL est **copiée automatiquement** dans `BepInEx/plugins/` (cible `PostBuild`) :
   ```
   ✅ DLL copié dans BepInEx/plugins/
   ```

**Dépendances** (`.csproj`) : `BepInEx.Unity.IL2CPP 6.0.0-be.697`, `Lib.Harmony 2.4.2`, `Il2CppInterop.Runtime 1.5.1`.

---

## 🎮 Règles

- Le **premier joueur éliminé** devient le **Réalisateur** — **un seul par partie**, définitif (irréversible jusqu'à la partie suivante).
- Seul l'**hôte** fait tourner le mod ; tous les autres sont en **vanilla**.
- Les **commandes publiques** fonctionnent pour tout le monde, à tout moment.
- Les **directives du Réalisateur** :
  - `/randomcolors`, `/cut`, `/darkness`, `/freeze` ne marchent que pour le Réalisateur et **uniquement en partie** (ignorées au lobby).
  - `/action`, `/loc`, `/vote` ne marchent que pour le Réalisateur et **uniquement en réunion**.
- Chaque directive a un **cooldown** ; si elle recharge, le temps restant est annoncé.

---

## ⌨️ Commandes

Les joueurs sont identifiés par des **numéros** (1, 2, 3…) — tape `/players` pour les voir.

### 🟢 Publiques (tout le monde)
| Commande | Effet |
|---|---|
| `/welcome` | Message de bienvenue |
| `/help` | Liste des commandes |
| `/gg` | Stats de la partie précédente (vivants / éliminés) |
| `/players` | Liste les joueurs et leurs ID-nums |
| `/hrandomcolors` | Aide détaillée pour `/randomcolors` |
| `/hcut` | Aide détaillée pour `/cut` |
| `/hdarkness` | Aide détaillée pour `/darkness` |
| `/hfreeze` | Aide détaillée pour `/freeze` |
| `/haction` | Aide détaillée pour `/action` |
| `/hloc` | Aide détaillée pour `/loc` |
| `/hvote` | Aide détaillée pour `/vote` |

### 🎬 Directives du Réalisateur
| Commande | Effet | Durée | Cooldown |
|---|---|---|---|
| `/randomcolors` | Couleurs aléatoires pour tous | instantané | 20s |
| `/cut` | Alerte sabotage réacteur (2s), puis arrêt complet (5s) — qui bouge meurt ! | 7s total | 30s |
| `/darkness` | Noir TOTAL sur toute la map | 10s | 35s |
| `/freeze ID` | Bloque le joueur ciblé sur place | 8s | 30s |
| `/action ID [A-D]` | Donne un script secret à un joueur ! Utilise `/action ID` pour voir la liste des scripts. Scripts : A = NoReport (ne rapporte pas de corps), B = SkipVote (passe le prochain vote), C = NoVents (ne pas utiliser les vents), D = VoteFirst (vote en premier ce round). Les joueurs qui désobéissent sont éliminés ! | Variable | 20s |
| `/loc ID_joueur ID_zone` | Interdit à un joueur d'entrer dans une zone spécifique (The Skeld seulement). IDs de zone : 1=Cafétéria, 2=Admin, 3=Electrical,4=Storage,5=Security,6=Réacteur,7=UpperEngine,8=LowerEngine,9=Medbay,10=CouloirElectrical,11=Communications,12=Shields,13=O2,14=Navigation,15=Weapons | Jusqu'à la fin du round | 20s |
| `/vote ID_joueur ID_cible` | Force un joueur à voter pour une cible spécifique. | Jusqu'à la prochaine réunion | 20s |

---

## ⚙️ Configuration (`DirectorOptions`)

| Option | Défaut | Description |
|---|---|---|
| `AnnounceInChat` | `true` | Relayer les actions dans le chat public |
| `AntiKick` | `true` | Throttle anti-kick (garder activé) |
| `MessageWait` | `0.6s` | Délai entre deux messages |

---

## 🧩 Fonctionnement (technique)

- **Host-only, compatible vanilla :** le mod ne patche que le jeu de l'hôte (HarmonyX) et réplique les effets via des **RPC vanilla** (`SendChat`, `SetName`, `SetColor`). Les clients n'ont besoin d'aucun mod.
- **Le chat comme canal :** toute la communication passe par le chat. Une **pompe** à file d'attente limitée en débit (`ChatManager`) s'appuie sur le timer de chat natif du jeu pour éviter le kick anti-spam du serveur.
- **Texte coloré vs brut :** le réseau reçoit du texte brut propre ; une color map locale réinjecte les balises `<color>` sur l'écran de l'hôte.
- **Annonces "système" :** le joueur vivant au plus petit ID est temporairement renommé `[ The Director's Cut ]` pour signer les messages officiels.
- **Snapshot de fin de partie :** les listes vivants/morts sont capturées sur `ShipStatus.OnDestroy` pour alimenter `/gg`.

---

<div align="center">

🎥 *Lights, camera… action! / Lumière, caméra… action !*

</div>
