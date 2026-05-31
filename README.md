# Among Us: The Director's Cut - Guide Complet

## 📋 Prérequis
- .NET SDK 6.0 ou supérieur
- Among Us installé via Steam
- BepInEx 5 installé dans le dossier Among Us

---

## 🚀 Étape 1: Configuration Initiale

### 1.1 Définir la variable d'environnement `AmongUsPath`
Ouvrez un PowerShell et exécutez la commande suivante (remplacez le chemin par votre chemin réel):
```powershell
[Environment]::SetEnvironmentVariable("AmongUsPath", "C:\Program Files (x86)\Steam\steamapps\common\Among Us", "User")
```
Fermez et rouvrez VS Code pour que la variable soit prise en compte.

### 1.2 Vérifier la structure de votre dossier Among Us
Votre dossier Among Us doit contenir:
```
Among Us/
├── BepInEx/
│   ├── core/
│   │   ├── 0Harmony.dll
│   │   └── BepInEx.dll
│   └── plugins/
└── Among Us_Data/
    └── Managed/
        └── Assembly-CSharp.dll
```

---

## 🔨 Étape 2: Compilation

1. Ouvrez un terminal dans le dossier du projet (`c:\Users\anish\Desktop\AU_TheDirectorsCut`)
2. Exécutez la commande:
   ```powershell
   dotnet build
   ```

Si la compilation réussit, la DLL sera automatiquement copiée dans `AmongUsPath\BepInEx\plugins\` !

---

## ✅ Étape 3: Vérification du déploiement

1. Lancez Among Us
2. Vérifiez la console BepInEx (fichier `Among Us/BepInEx/LogOutput.log` ou console si activée)
3. Recherchez la ligne:
   ```
   [Info   : AU_TheDirectorsCut] Loaded successfully!
   ```

---

## 🧪 Étape 4: Test du Mod

### Comment tester avec deux instances:

1. **Instance 1 (Hôte):**
   - Lancez Among Us normalement
   - Créez une partie privée
   - Notez le code de la partie

2. **Instance 2 (Client):**
   - Créez un raccourci de `Among Us.exe`
   - Ajoutez ` -screen-fullscreen 0` à la fin de la cible pour lancer en fenêtré
   - Lancez cette instance
   - Rejoignez la partie via le code

3. **Testez les fonctionnalités:**
   - Faites mourir un joueur (ça devient le Réalisateur)
   - Le Réalisateur tape les commandes dans le chat:
     - `/cut`: Déclenche le Cut (6 secondes, ne bougez pas!)
     - `/swap 0 1`: Échange les positions des joueurs 0 et 1
     - `/hyper`: Active le hyperdrive (vitesse ×3 pendant 10s)
     - `/blind 0`: Aveugle le joueur 0

---

## 📝 Commandes du Réalisateur

| Commande | Description |
|----------|-------------|
| `/cut` | Déclenche le "1, 2, 3 Soleil" - bougez et vous mourez! |
| `/swap [ID1] [ID2]` | Téléporte deux joueurs l'un à la place de l'autre |
| `/hyper` | Augmente la vitesse de tous les joueurs ×3 pendant 10 secondes |
| `/blind [ID]` | Aveugle un joueur (réduit son champ de vision) |

---

## 🏗 Architecture du Projet

- **AU_TheDirectorsCut.csproj**: Fichier projet avec références et événements post-build
- **Plugin.cs**: Point d'entrée BepInEx et initialisation Harmony
- **DirectorCore.cs**: Gestion de l'état du réalisateur, interception du chat, logique de jeu
- **NetworkManager.cs**: Communication RPC et manipulation des clients vanilla
