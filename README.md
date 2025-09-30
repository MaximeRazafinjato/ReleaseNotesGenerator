# 📝 Release Notes Generator

Générateur automatique de release notes depuis Git et GitLab avec recherche intelligente de projet.

## 🎯 Fonctionnalités

- ✅ **Recherche de projet GitLab** : Cherche et sélectionne automatiquement le projet
- ✅ **Analyse des commits Git** : Parse les commits entre 2 tags
- ✅ **Catégorisation automatique** : feat, fix, refactor, docs, test, chore
- ✅ **Extraction de tickets JIRA** : Détecte automatiquement les FTELINFO-123, SAS-456, etc.
- ✅ **Intégration GitLab MRs** : Récupère les Merge Requests associées
- ✅ **Génération Markdown** : Format prêt à copier/coller
- ✅ **Copie automatique** dans le presse-papier
- ✅ **Menu interactif** avec Spectre.Console

## 📋 Prérequis

- .NET 9.0 SDK
- Token API GitLab
- ✅ **Pas besoin de Git local** - Tout est récupéré depuis GitLab !

## 🔐 Configuration

### 1. Obtenir le token API GitLab

1. Allez sur votre instance GitLab : `https://your-gitlab.com/-/profile/personal_access_tokens`
2. Créez un nouveau token avec les scopes :
   - `read_api`
   - `read_repository`
3. Copiez le token

### 2. Configurer l'outil

1. Copiez `appsettings.example.json` vers `appsettings.json`
2. Éditez `appsettings.json` avec vos paramètres :

```json
{
  "GitLab": {
    "BaseUrl": "https://your-gitlab.com",
    "ApiToken": "YOUR_GITLAB_API_TOKEN_HERE"
  }
}
```

## 🚀 Utilisation

### Mode interactif (recommandé)

```bash
cd /path/to/votre-repo
dotnet run --project ReleaseNotesGenerator

# Ou avec le projet spécifié
dotnet run --project ReleaseNotesGenerator -- --project "Ftello"
```

**L'outil va :**
1. Rechercher le projet dans GitLab (par nom ou ID)
2. Lister les tags Git disponibles
3. Proposer par défaut de comparer les 2 dernières releases
   - Si vous acceptez, utilise automatiquement ces tags
   - Si vous refusez, vous laisse choisir manuellement
4. Générer les release notes
5. Copier dans le presse-papier

### Mode ligne de commande

```bash
# Entre 2 tags avec projet GitLab (par nom)
dotnet run -- --project "Ftello" --from v1.0.0 --to v1.1.0

# Entre 2 tags avec projet GitLab (par ID)
dotnet run -- --project "123" --from v1.0.0 --to v1.1.0

# Depuis un tag jusqu'à HEAD
dotnet run -- --project "Hub3E" --from v2.0.0

# Avec sauvegarde dans un fichier
dotnet run -- --project "Commerce" --from v1.0.0 --output RELEASE_v1.1.0.md
```

### Arguments disponibles

- `--project`, `-p` : Nom ou ID du projet GitLab
  - Nom: recherche par nom (ex: "Ftello", "Hub3E")
  - ID: recherche directe par ID numérique (ex: "123")
  - L'ID se trouve dans GitLab sous le nom du projet
- `--from`, `-f` : Tag de début (requis)
- `--to`, `-t` : Tag de fin (optionnel, défaut: HEAD)
- `--output`, `-o` : Fichier de sortie (optionnel)

## 📊 Exemple de sortie

```markdown
# Release Notes - v1.1.0

**Date**: 30 Septembre 2025
**Range**: `v1.0.0` → `v1.1.0`
**Commits**: 42
**Merge Requests**: 8

## 🎉 Features
- **[FTELINFO-123]** Ajout du module de gestion des utilisateurs ([!456](https://gitlab.com/mr/456)) by @mrazafinjato
- **[SAS-789]** Intégration API HUB3E (`a1b2c3d`)

## 🐛 Bug Fixes
- **[COMMERCE-456]** Correction du calcul de TVA ([!457](https://gitlab.com/mr/457))
- Fix crash au démarrage sur Windows 11 (`d4e5f6g`)

## 🔧 Refactoring
- Simplification de la couche service (`h7i8j9k`)
- Migration vers .NET 9 (`k1l2m3n`)
```

## 📁 Structure du projet

```
ReleaseNotesGenerator/
├── Models/              # DTOs (GitCommit, GitLabProject, ReleaseNote, etc.)
├── Services/
│   ├── GitService.cs              # Opérations Git locales
│   ├── GitLabService.cs           # API GitLab
│   ├── ReleaseNoteGeneratorService.cs  # Génération du Markdown
│   └── MenuService.cs             # Menu interactif
├── Program.cs                     # Point d'entrée
├── appsettings.json              # Configuration
└── README.md                      # Ce fichier
```

## 🔍 Détection automatique

### Types de commits

Le tool détecte automatiquement le type de commit :

- `feat:`, `feature:` → 🎉 Features
- `fix:`, `bugfix:` → 🐛 Bug Fixes
- `refactor:` → 🔧 Refactoring
- `docs:`, `doc:` → 📝 Documentation
- `test:`, `tests:` → 🧪 Tests
- `chore:`, `build:`, `ci:` → ⚙️ Chore
- Autre → 📦 Other

### Tickets JIRA

Extrait automatiquement les références comme :
- `FTELINFO-123`
- `SAS-456`
- `COMMERCE-789`
- `MEDIA-012`
- Etc.

### Merge Requests

Associe automatiquement les commits aux MRs GitLab en cherchant :
- Même ticket JIRA
- Titre similaire
- Description contenant le commit

## 🛠️ Développement

### Compiler

```bash
dotnet build
```

### Publier (executable standalone)

```bash
dotnet publish -c Release -r win-x64 --self-contained
```

L'exécutable sera dans : `bin/Release/net9.0/win-x64/publish/`

## 🐛 Dépannage

### Erreur "Configuration GitLab manquante"

➡️ Vérifiez que `appsettings.json` existe et contient votre token.

### Erreur "Aucun tag Git trouvé"

➡️ Assurez-vous d'être dans un repository Git avec des tags.

### Erreur "401 Unauthorized" (GitLab)

➡️ Vérifiez :
- Votre token API GitLab
- Les scopes du token (`read_api`, `read_repository`)
- L'URL de base GitLab

### Aucun projet trouvé

➡️ Vérifiez :
- Le nom du projet (essayez une recherche plus large)
- Que vous avez accès au projet dans GitLab
- Que le token a les droits suffisants

## 📦 Packages utilisés

- `Newtonsoft.Json` (13.0.3) - Sérialisation JSON
- `RestSharp` (112.1.0) - Client HTTP REST
- `TextCopy` (6.2.1) - Copie presse-papier
- `Spectre.Console` (0.48.0) - Interface CLI interactive
- `System.CommandLine` (2.0.0-beta4) - Parsing d'arguments
- `Microsoft.Extensions.Configuration.Json` (9.0.0) - Configuration

## 💡 Conseils

1. **Mode rapide** : En mode interactif sans arguments, l'outil propose automatiquement de comparer les 2 dernières releases
2. **Recherche par ID** : Utilisez l'ID du projet pour une sélection plus rapide (visible dans GitLab)
3. **Convention de commits** : Utilisez des préfixes (`feat:`, `fix:`, etc.) pour une meilleure catégorisation
4. **Tickets JIRA** : Mentionnez toujours le ticket dans le message de commit
5. **Tags réguliers** : Tagguez régulièrement vos versions pour faciliter la génération
6. **MRs descriptives** : Remplissez bien les titres et descriptions de vos Merge Requests

## 🤝 Contribution

Les contributions sont les bienvenues ! N'hésitez pas à ouvrir une issue ou une pull request.

## 📜 Licence

MIT