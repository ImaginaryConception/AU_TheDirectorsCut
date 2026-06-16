<img width="1456" height="720" alt="thedirectorscutlogo" src="https://github.com/user-attachments/assets/11fa7f71-8d82-4357-86ab-deba3fa3d4a0" />
<div align="center">

# 🎬 THE DIRECTOR'S CUT

**An Among Us host-only mod — the first player to die becomes the Director.**
**Un mod Among Us *host-only* — le premier joueur éliminé devient le Réalisateur.**

`BepInEx 6 (IL2CPP)` · `HarmonyX 2.4.2` · `.NET 6` · `Vanilla-client compatible`

</div>

---

🇬🇧 **English** · [🇫🇷 Version française](#-version-française)

---

## 🎥 What is this?

When the **first player dies**, they don't leave — they take over as the **Director**. From beyond the grave they stop playing Among Us and start *directing* it: secret orders, theatrical events, traps, identity swaps…

**The key point:** only the **host** installs the mod. Everyone else — including the Director — plays on a completely **unmodded (vanilla) client**. The mod intercepts chat commands on the host and replicates effects to all players using vanilla network calls.

**Designed for private lobbies.** Several features (forced kills, vote traps, rapid chat) assume a private server **without anti-cheat**.

---

## ✅ Requirements

| | |
|---|---|
| **Game** | Among Us (Steam recommended — 32-bit / x86 build) |
| **Loader** | BepInEx **6.x IL2CPP** (`x86` build to match Among Us) |
| **Who installs** | **Host only.** Other players need nothing. |
| **Server** | Private lobby (no anti-cheat) recommended |
| **OS** | Windows (tested) |

> ⚠️ Among Us is a **32-bit** app — download the **`win-x86`** BepInEx IL2CPP build, not x64.

---

## 📦 Installation (host)

### 1. Install BepInEx 6 (IL2CPP)
1. Download the latest **BepInEx 6 IL2CPP `win-x86`** build: 👉 https://github.com/BepInEx/BepInEx/releases
2. Open your Among Us folder (the one containing `Among Us.exe`) — **Steam:** right-click the game → *Manage* → *Browse local files*.
3. Extract the **entire contents** of the zip into that folder (you should see `BepInEx/`, `doorstop_config.ini`, `winhttp.dll`).

### 2. Generate the interop assemblies
1. **Launch Among Us once.** BepInEx generates the IL2CPP interop DLLs.
2. Reach the main menu, then **close the game** — this creates `BepInEx/interop/`.

### 3. Install the mod
1. Drop **`AU_TheDirectorsCut.dll`** into `<Among Us>/BepInEx/plugins/`.
2. **Launch Among Us.** The BepInEx console should show:
   ```
   [DirectorCore] Initialisé.
   [NetworkManager] Initialisé.
   ```

### 4. Play
1. **Host a lobby** (you must be the host for the mod to do anything).
2. New players receive a private welcome message **instantly**.
3. Start a game. The **first death** becomes the Director. 🎬
4. Press **Delete (Suppr)** any time as host to open the **Admin panel**.

---

## 🛠️ Build from source (developers)

Requires the **.NET 6 SDK**, and `BepInEx/interop/` must exist (launch the game once with BepInEx).

```bash
setx AmongUsPath "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
dotnet build -c Release /p:AmongUsPath="C:\path\to\Among Us"
```

On success the DLL is **auto-copied** to `BepInEx/plugins/` (the `PostBuild` target). **Dependencies:** `BepInEx.Unity.IL2CPP 6.0.0-be.697`, `Lib.Harmony 2.4.2`, `Il2CppInterop.Runtime 1.5.1`.

**Source layout:** `Plugin.cs` (entry) · `DirectorCore.cs` (rules, commands, effects) · `ChatManager.cs` (chat/bot identity) · `NetworkManager.cs` (vanilla RPC helpers) · `ScriptManager.cs` (secret orders) · `Directives.cs` (Director directives) · `AdminUI.cs` (host IMGUI panel) · `ModMessages.cs` (all text).

---

## 🎮 Rules

- The **first player to die** becomes the **Director** — **one per game**, locked in until the next game. They are privately told they're the Director.
- Only the **host** runs the mod; everyone else is **vanilla**.
- **Players are identified by letters** (A, B, C…) — run `/players` to see them.
- Most director commands are **in-game only**; the order/vote ones are **meeting only**. Each has a **cooldown**.
- **The host can be targeted and killed too** (e.g. `/cut`, `/kill`).

### 🔒 Confidentiality model
- **Command feedback** (confirmations, effect banners, cooldown notices) is shown **only to the Director** who issued it.
- **Order info** (`/action`, `/loc`, `/vote`, `/stalker`, `/ultimatum`) is sent **only to the targeted player(s)**.
- **Public to everyone:** eliminations (`X moved — eliminated!`), order success/failure, `/voiceover`, and the `/roulette` announcements.
- Messages are signed by the bot **The Director's Cut** (blue name + distinct avatar color), and chat is **instant** (no delay) with full **bold / colors / line-breaks** visible to all players.

---

## ⌨️ Commands

### 🟢 Public (everyone, anywhere)
| Command | Effect |
|---|---|
| `/help` | Full command list (host also sees the **Admin** section) |
| `/welcome` | Welcome message |
| `/gg` | Previous-game stats (alive / eliminated) |
| `/players` | List players with their letter IDs |
| `/join` · `/discord` | Discord invite |
| `/hcut` `/hdarkness` `/hfreeze` `/haction` `/helpaction` `/hloc` `/hvote` `/hrandomcolors` `/hcolorblind` `/hshuffle` `/hswap` `/hteleportall` `/htp` `/hvoiceover` `/hspotlight` `/hmarathon` `/hroulette` `/hbodyswap` `/hultimatum` `/hstalker` | Detailed help per command |

### 🎬 Director — in-game effects
| Command | Effect | Cooldown |
|---|---|---|
| `/randomcolors` | Random unique color for everyone | 20s |
| `/cut` | Reactor sabotage alert (2s) → no-movement freeze (5s): **everyone who moves dies!** | 30s |
| `/darkness` | Total darkness across the whole map (10s) | 35s |
| `/freeze A` | Freezes the target in place (8s) | 30s |
| `/colorblind` | Everyone turns grey & names are hidden (25s, auto-restored) | 40s |
| `/shuffle` | Randomly shuffles everyone's positions | 20s |
| `/swap A B` | Swaps the positions of two players | 15s |
| `/teleportall A` | Teleports everyone to player A | 20s |
| `/tp A B` | Teleport player A to player B's position | 10s |

### 🎬 Director — meeting only
| Command | Effect | Cooldown |
|---|---|---|
| `/action A [A-D]` | Secret script for a player — **A**=NoReport, **B**=SkipVote, **C**=NoVents, **D**=VoteFirst. Disobey = eliminated. | 20s |
| `/loc A ZONE` | Forbid a player from a zone (Skeld). Zones: B=Admin, C=Electrical, D=Storage, E=Security, F=Reactor, G=UpperEngine, H=LowerEngine, I=Medbay, J=Comms, K=Shields, L=O2, M=Navigation, N=Weapons | 20s |
| `/vote A B` | Force player A to vote for B | 20s |

### 🎭 Director — Directives (the fun stuff)
**In-game:**
| Command | Effect | Cooldown |
|---|---|---|
| `/voiceover <text>` | **The Voice-Off** — anonymous, theatrical message shown huge to everyone | 8s |
| `/spotlight A` | Everyone goes dark **except** player A (20s) | 30s |
| `/marathon` | Speed boost for everyone (15s) | 30s |
| `/roulette` | A random living player is dramatically eliminated | 45s |
| `/bodyswap A B` | Swaps two players' identities (color + name) — total confusion | 30s |

**Meeting only:**
| Command | Effect |
|---|---|
| `/stalker A B` | **The Obsessive** — A must stay within 3m of B all round (both privately warned). Stray too long → eliminated. |
| `/ultimatum A [s]` | An impostor must make a kill within the delay (default 60s, configurable in seconds); if they kill no one, their role is revealed to everyone (name in red) and an emergency meeting is auto-called. |

### 🛡️ Admin (host only — others get "Host only!")
| Command | Effect |
|---|---|
| `/start` · `/stop` | Start / stop the game |
| `/setdirector [A]` | Set the Director (yourself if no ID) |
| `/rename A NEW_NAME` | Rename a player |
| `/kill A` | Eliminate a player |
| `/endmeeting` | Force the current meeting to end |
| **Delete (Suppr) key** | Open the **Admin panel**: buttons for all of the above + a per-player list (Kill / Set Director / Rename) |

> ❌ **Not included — "Slasher" mode** (black-and-white screen with red blood). This is a client-side camera/shader effect and is **impossible without a mod on every player's PC**, which breaks the host-only design.

---

## ⚙️ Configuration (`DirectorOptions`)

| Option | Default | Description |
|---|---|---|
| `AnnounceInChat` | `true` | Relay relevant actions in chat |
| `ChatManager.BotCosmetics` | `true` | Give the bot a blue name **and** a distinct avatar color (set `false` to keep only the blue name) |

---

## 🧩 How it works (technical)

- **Host-only, vanilla-compatible:** only the host is patched (HarmonyX); effects are replicated with **vanilla RPCs** (`SendChat`, `SetName`, `SetColor`, `MurderPlayer`, `SnapTo`) and per-client `GameOptions` (vision, speed, kill cooldown).
- **Instant chat:** the chat pump drains its whole queue every frame — no artificial delay. Welcome/GG messages are immediate too.
- **Rich text for everyone:** the colored/bold/multi-line text is sent over the network so **all** players see the formatting (vanilla clients render TMP rich text). A safety cap (~1200 bytes) protects the Hazel packet.
- **Bot identity:** the host is briefly renamed/recolored to **The Director's Cut** (blue) around each chat RPC; the chat bubble bakes the name & avatar, so the host's real identity is restored immediately after.
- **Proximity** (`/stalker`): the host knows every position, so this is tracked host-side each frame.
- **End-of-game snapshot:** alive/dead lists are captured on `ShipStatus.OnDestroy` to feed `/gg`.

---

## 💬 Discord

Join the community: **`https://discord.gg/EVbPNEWDZd`**
Contacts: `imaginaryconception` · `kalinina_sn`

*(In-game `/discord` currently shows the contacts above. Send the host your invite link and it can be added to the message.)*

---
---

<a name="-version-française"></a>
# 🇫🇷 Version française

## 🎥 Le concept

Quand le **premier joueur meurt**, il ne quitte pas la partie — il devient le **Réalisateur**. Depuis l'au-delà, il ne joue plus à Among Us, il le *met en scène* : ordres secrets, événements théâtraux, pièges, échanges d'identité…

**Le point clé :** seul l'**hôte** installe le mod. Tous les autres — y compris le Réalisateur — jouent sur un client **totalement vanilla (non moddé)**. Le mod intercepte les commandes côté hôte et réplique les effets à tous via des appels réseau vanilla.

**Conçu pour les lobbies privés.** Plusieurs fonctions (kills forcés, pièges de vote, chat rapide) supposent un **serveur privé sans anti-cheat**.

---

## ✅ Prérequis

| | |
|---|---|
| **Jeu** | Among Us (Steam recommandé — version 32 bits / x86) |
| **Loader** | BepInEx **6.x IL2CPP** (build `x86`) |
| **Qui installe** | **L'hôte seulement.** Les autres n'ont rien à faire. |
| **Serveur** | Lobby privé (sans anti-cheat) recommandé |
| **OS** | Windows (testé) |

> ⚠️ Among Us est une appli **32 bits** — prends le build BepInEx IL2CPP **`win-x86`**, pas le x64.

---

## 📦 Installation (hôte)

### 1. Installer BepInEx 6 (IL2CPP)
1. Télécharge le dernier build **BepInEx 6 IL2CPP `win-x86`** : 👉 https://github.com/BepInEx/BepInEx/releases
2. Ouvre le dossier d'Among Us (celui qui contient `Among Us.exe`) — **Steam :** clic droit → *Gérer* → *Parcourir les fichiers locaux*.
3. Extrais **tout le contenu** du zip dans ce dossier (tu dois voir `BepInEx/`, `doorstop_config.ini`, `winhttp.dll`).

### 2. Générer les assemblies interop
1. **Lance Among Us une fois** — BepInEx génère les DLL interop IL2CPP.
2. Atteins le menu principal, puis **ferme le jeu** — cela crée `BepInEx/interop/`.

### 3. Installer le mod
1. Place **`AU_TheDirectorsCut.dll`** dans `<Among Us>/BepInEx/plugins/`.
2. **Lance Among Us.** La console BepInEx doit afficher :
   ```
   [DirectorCore] Initialisé.
   [NetworkManager] Initialisé.
   ```

### 4. Jouer
1. **Héberge un lobby** (tu dois être l'hôte pour que le mod agisse).
2. Les nouveaux joueurs reçoivent un message de bienvenue privé **instantanément**.
3. Lance une partie. La **première mort** devient le Réalisateur. 🎬
4. Appuie sur **Suppr (Delete)** en tant qu'hôte pour ouvrir le **panneau Admin**.

---

## 🛠️ Compiler depuis les sources (développeurs)

Nécessite le **SDK .NET 6**, et `BepInEx/interop/` doit exister (lance le jeu une fois avec BepInEx).

```bash
setx AmongUsPath "C:\Program Files (x86)\Steam\steamapps\common\Among Us"
dotnet build -c Release /p:AmongUsPath="C:\chemin\vers\Among Us"
```

En cas de succès, la DLL est **copiée automatiquement** dans `BepInEx/plugins/` (cible `PostBuild`). **Dépendances :** `BepInEx.Unity.IL2CPP 6.0.0-be.697`, `Lib.Harmony 2.4.2`, `Il2CppInterop.Runtime 1.5.1`.

**Organisation du code :** `Plugin.cs` (entrée) · `DirectorCore.cs` (règles, commandes, effets) · `ChatManager.cs` (chat / identité du bot) · `NetworkManager.cs` (helpers RPC vanilla) · `ScriptManager.cs` (ordres secrets) · `Directives.cs` (directives du Réalisateur) · `AdminUI.cs` (panneau IMGUI hôte) · `ModMessages.cs` (tous les textes).

---

## 🎮 Règles

- Le **premier joueur éliminé** devient le **Réalisateur** — **un seul par partie**, définitif jusqu'à la partie suivante. Il est prévenu en privé qu'il est le Réalisateur.
- Seul l'**hôte** fait tourner le mod ; tous les autres sont en **vanilla**.
- **Les joueurs sont identifiés par des lettres** (A, B, C…) — tape `/players`.
- La plupart des commandes du Réalisateur sont **en partie uniquement** ; celles d'ordre/vote sont **en réunion uniquement**. Chacune a un **cooldown**.
- **L'hôte peut être ciblé et tué** lui aussi (ex. `/cut`, `/kill`).

### 🔒 Modèle de confidentialité
- Les **retours de commande** (confirmations, bannières d'effet, cooldowns) ne s'affichent qu'**au Réalisateur** qui les a lancés.
- Les **infos d'ordre** (`/action`, `/loc`, `/vote`, `/stalker`, `/ultimatum`) ne vont qu'**au(x) joueur(s) ciblé(s)**.
- **Publics pour tous :** les éliminations (`X a bougé — éliminé !`), les succès/échecs d'ordre, `/voiceover`, et les annonces de `/roulette`.
- Les messages sont signés par le bot **The Director's Cut** (pseudo bleu + couleur d'avatar distincte), et le chat est **instantané** (sans délai) avec **gras / couleurs / sauts de ligne** visibles par tous.

---

## ⌨️ Commandes

### 🟢 Publiques (tout le monde, partout)
| Commande | Effet |
|---|---|
| `/help` | Liste complète (l'hôte voit aussi la section **Admin**) |
| `/welcome` | Message de bienvenue |
| `/gg` | Stats de la partie précédente (vivants / éliminés) |
| `/players` | Liste les joueurs et leurs ID-lettres |
| `/join` · `/discord` | Invitation Discord |
| `/hcut` `/hdarkness` `/hfreeze` `/haction` `/helpaction` `/hloc` `/hvote` `/hrandomcolors` `/hcolorblind` `/hshuffle` `/hswap` `/hteleportall` `/htp` `/hvoiceover` `/hspotlight` `/hmarathon` `/hroulette` `/hbodyswap` `/hultimatum` `/hstalker` | Aide détaillée par commande |

### 🎬 Réalisateur — effets en partie
| Commande | Effet | Cooldown |
|---|---|---|
| `/randomcolors` | Couleur unique aléatoire pour tous | 20s |
| `/cut` | Alerte sabotage (2s) → arrêt complet (5s) : **tous ceux qui bougent meurent !** | 30s |
| `/darkness` | Noir TOTAL sur toute la map (10s) | 35s |
| `/freeze A` | Bloque la cible sur place (8s) | 30s |
| `/colorblind` | Tout le monde en gris & noms masqués (25s, restauré auto) | 40s |
| `/shuffle` | Mélange aléatoirement les positions de tous | 20s |
| `/swap A B` | Échange les positions de deux joueurs | 15s |
| `/teleportall A` | Téléporte tout le monde vers le joueur A | 20s |
| `/tp A B` | Téléporte le joueur A sur la position du joueur B | 10s |

### 🎬 Réalisateur — réunion uniquement
| Commande | Effet | Cooldown |
|---|---|---|
| `/action A [A-D]` | Script secret — **A**=NoReport, **B**=SkipVote, **C**=NoVents, **D**=VoteFirst. Désobéir = éliminé. | 20s |
| `/loc A ZONE` | Interdit une zone à un joueur (Skeld). Zones : B=Admin, C=Electrical, D=Storage, E=Security, F=Réacteur, G=UpperEngine, H=LowerEngine, I=Medbay, J=Comms, K=Shields, L=O2, M=Navigation, N=Weapons | 20s |
| `/vote A B` | Force le joueur A à voter pour B | 20s |

### 🎭 Réalisateur — Directives (le fun)
**En partie :**
| Commande | Effet | Cooldown |
|---|---|---|
| `/voiceover <texte>` | **La Voix Off** — message anonyme et théâtral affiché en grand à tous | 8s |
| `/spotlight A` | Tout le monde dans le noir **sauf** A (20s) | 30s |
| `/marathon` | Boost de vitesse pour tous (15s) | 30s |
| `/roulette` | Un joueur vivant au hasard est éliminé avec suspense | 45s |
| `/bodyswap A B` | Échange les identités (couleur + pseudo) de deux joueurs | 30s |

**En réunion :**
| Commande | Effet |
|---|---|
| `/stalker A B` | **L'Obsessionnel** — A doit rester à moins de 3m de B toute la manche (les deux sont prévenus). S'éloigner trop longtemps → éliminé. |
| `/ultimatum A [s]` | Un imposteur doit faire un kill dans le délai (défaut 60s, configurable en secondes) ; s'il ne tue personne, son rôle est révélé à tous (pseudo en rouge) et une réunion d'urgence est déclenchée automatiquement. |

### 🛡️ Admin (hôte uniquement — sinon « Hôte seulement ! »)
| Commande | Effet |
|---|---|
| `/start` · `/stop` | Lance / arrête la partie |
| `/setdirector [A]` | Désigne le Réalisateur (toi-même si pas d'ID) |
| `/rename A NOUVEAU_NOM` | Renomme un joueur |
| `/kill A` | Élimine un joueur |
| `/endmeeting` | Force la fin de la réunion en cours |
| **Touche Suppr (Delete)** | Ouvre le **panneau Admin** : boutons pour tout ce qui précède + une liste par joueur (Kill / Réalisateur / Renommer) |

> ❌ **Non inclus — mode « Slasher »** (écran noir & blanc avec sang rouge). C'est un effet de caméra/shader côté client, **impossible sans mod installé chez chaque joueur**, ce qui casserait le principe host-only.

---

## ⚙️ Configuration (`DirectorOptions`)

| Option | Défaut | Description |
|---|---|---|
| `AnnounceInChat` | `true` | Relaie les actions pertinentes dans le chat |
| `ChatManager.BotCosmetics` | `true` | Donne au bot un pseudo bleu **et** une couleur d'avatar distincte (mets `false` pour ne garder que le pseudo bleu) |

---

## 🧩 Fonctionnement (technique)

- **Host-only, compatible vanilla :** seul l'hôte est patché (HarmonyX) ; les effets sont répliqués via des **RPC vanilla** (`SendChat`, `SetName`, `SetColor`, `MurderPlayer`, `SnapTo`) et des `GameOptions` par client (vision, vitesse, cooldown de kill).
- **Chat instantané :** la pompe de chat vide toute sa file à chaque frame — aucun délai artificiel. Welcome/GG sont immédiats aussi.
- **Rich text pour tous :** le texte coloré/gras/multi-lignes est envoyé sur le réseau pour que **tous** les joueurs voient le formatage (les clients vanilla rendent le rich text TMP). Un plafond de sécurité (~1200 octets) protège le paquet Hazel.
- **Identité du bot :** l'hôte est brièvement renommé/recoloré en **The Director's Cut** (bleu) autour de chaque RPC de chat ; la bulle fige le nom & l'avatar, donc l'identité réelle de l'hôte est restaurée juste après.
- **Proximité** (`/stalker`) : l'hôte connaît toutes les positions, donc c'est suivi côté hôte à chaque frame.
- **Snapshot de fin de partie :** les listes vivants/morts sont capturées sur `ShipStatus.OnDestroy` pour alimenter `/gg`.

---

## 💬 Discord

Rejoins la communauté : **`https://discord.gg/EVbPNEWDZd`**
Contacts : `imaginaryconception` · `kalinina_sn`

*(En jeu, `/discord` affiche actuellement les contacts ci-dessus. Donne-moi ton lien d'invitation et je l'ajoute au message.)*

---

<div align="center">

🎥 *Lights, camera… action! / Lumière, caméra… action !*

</div>
