# Image Generator App

Application de bureau Windows Forms (.NET 10) pour la génération d'images par IA, supportant plusieurs providers.

## Providers supportés

| Provider | Modèle(s) | API | Édition multi-tour |
|---|---|---|---|
| **Grok Imagine (xAI)** | `grok-imagine-image`, `grok-imagine-image-quality` | `https://api.x.ai/v1/images/` | ✅ Oui (jusqu'à 3 images) |
| **Nano Banana Pro (Google)** | `nano-banana-pro` | `https://generativelanguage.googleapis.com/v1beta/` | ❌ Non |

## Fonctionnalités

- **Génération d'images multi-provider** : Sélectionnez le modèle souhaité et fournissez un prompt textuel pour générer une image.
- **Édition d'images (Multi-turn)** : Chargez jusqu'à 3 images de base ou modifiez l'image précédemment générée *(Grok Imagine uniquement)*.
- **Résolutions multiples** : Support 1k et 2k.
- **Ratios d'aspect variés** : 1:1, 16:9, 9:16, 4:3, 3:2, 20:9.
- **Sauvegarde locale asynchrone** : Enregistrez l'image générée en PNG ou JPEG sans bloquer l'interface. Le décodage Base64, l'encodage ImageSharp et l'intégration de métadonnées sont exécutés de façon asynchrone via un thread d'arrière-plan (`Task.Run` + `File.WriteAllBytesAsync`) garantissant une réactivité maximale de l'interface graphique.
- **Intégration automatique de métadonnées AI** : Lors de l'export, les informations de génération (prompt original, modèle utilisé, date/heure, résolution, etc.) sont automatiquement intégrées dans les métadonnées de l'image (EXIF, XMP et chunks PNG tEXt/iTXt).
- **Système de Gabarits (Templates) SQLite** : Utilisez des balises `{key}` ou `{key:param1:param2}` pour factoriser vos styles, avec résolution récursive sécurisée (limite de 20 boucles) et moteur de validation syntaxique levant des exceptions dédiées (`FormatException`, `InvalidOperationException`, `KeyNotFoundException`).
- **Validation visuelle en temps réel (Bordure rouge UX)** : Une bordure rouge de 2 pixels apparaît instantanément autour du champ prompt en cas de syntaxe invalide ou de gabarit non reconnu (évaluée à la perte de focus ou au survol du bouton de génération). La bordure rouge disparaît immédiatement dès la reprise de la saisie.
- **Activation dynamique intelligente (Generating button locking)** : Le bouton de génération se désactive et se verrouille automatiquement en cas de champ vide, d'erreur syntaxique, de clé de template absente de la base, ou lorsqu'une génération asynchrone d'image est en cours.
- **Autocomplétion Mid-String au Caret** : Une liste flottante contextuelle d'autocomplétion apparaît lors de la saisie de l'accolade `{` pour insérer rapidement vos gabarits, alimentée par un cache asynchrone pour éviter tout ralentissement de la saisie.
- **Aperçu dynamique du Prompt** : Survolez le bouton de génération pour prévisualiser le prompt entièrement résolu et expansé dans une info-bulle avant de l'envoyer à l'API.
- **Historique de Génération Local (SQLite & WEBP)** : Chaque image générée avec succès déclenche une tâche d'arrière-plan asynchrone et silencieuse qui compresse l'image d'origine en **WEBP (Qualité 80%)** dans le dossier `%LocalAppData%\ImageGeneratorApp\HistoryImages\`, préservant de 5 à 10 fois le stockage par rapport aux PNG d'origine.
- **Préservation de Métadonnées Provenance** : Les images d'historique WEBP stockées sur disque intègrent les profils de métadonnées EXIF et XMP standardisés contenant le prompt, le nom du modèle, la date/heure de génération et le logiciel de création.
- **Explorateur d'Historique Premium (Split View)** : Un dialogue moderne scindé (SplitContainer) codé manuellement en code-first (Design-First) offrant une recherche textuelle filtrée par SQL `LIKE` temps réel avec Dapper, prévisualisation de l'image (décodage asynchrone sécurisé WEBP vers Bitmap GDI+), visualiseur monospace de prompt et bloc de métadonnées JSON avec indentation automatique.
- **Concurrence & Sécurité Mémoire** : Protection contre les race conditions lors d'une navigation rapide dans l'historique (les anciennes requêtes de chargement d'image sont automatiquement ignorées via un jeton de sélection unique), et libération rigoureuse des handles GDI+ pour éviter les fuites de ressources.
- **Dialogue « À propos » et licence** : Accédez aux informations de version, copyright et au texte complet de la licence GPL v3 directement depuis l'application via le menu **Aide → À propos de Générateur d'image...**. Un bouton permet d'ouvrir le fichier `LICENSE.txt` situé à côté de l'exécutable.

## Prérequis

- [.NET 10 SDK](https://dotnet.microsoft.com/download) (Windows)
- Une clé API xAI (pour Grok Imagine) et/ou une clé Google Cloud (pour Nano Banana Pro)

## Démarrage rapide

```bash
# Compiler
dotnet build ImageGeneratorApp.csproj

# Lancer
dotnet run --project ImageGeneratorApp.csproj

# Tests
dotnet test ImageGeneratorApp.Tests/ImageGeneratorApp.Tests.csproj --verbosity normal
```

## Structure du projet

```text
├── Form1.cs                      # Interface utilisateur (WinForms code-first et bouton historique)
├── DatabaseHelper.cs             # Initialisation de la base SQLite, type-mapping Dapper (gère templates et historique)
├── TemplateModel.cs              # Représentation entité d'un gabarit de prompt
├── TemplateRepository.cs         # Opérations CRUD asynchrones alimentées par Dapper
├── TemplateParser.cs             # Moteur de résolution récursif avec Regex compilées
├── TemplatesManagerForm.cs       # Dialogue de gestion des gabarits programmé en C#
├── TemplateEditorForm.cs         # Dialogue d'ajout/édition de gabarits programmé en C#
├── GenerationHistoryModel.cs     # Représentation entité d'un enregistrement d'historique
├── GenerationHistoryRepository.cs# Opérations CRUD et recherche (Dapper) pour l'historique
├── ImageProcessingService.cs     # Service de conversion WEBP, d'injection et de décodage BMP GDI+ (ImageSharp)
├── HistoryOrchestrator.cs        # Orchestrateur coordonnant la sauvegarde WEBP et l'écriture SQLite
├── HistoryViewerForm.cs          # Explorateur d'historique (split-panel, code-first) avec protection de course
├── AboutForm.cs                  # Dialogue À propos (licence GPL v3, ouverture de LICENSE.txt)
├── ImageGeneratorClient.cs       # Client HTTP multi-provider
├── ImageGeneratorRequest.cs      # Modèle de requête (xAI)
├── ImageGeneratorResponse.cs     # Modèle de réponse (xAI)
├── ImageGeneratorException.cs    # Exception personnalisée (tous providers)
├── ImageGeneratorJsonContext.cs  # Sérialisation JSON source-generated
├── GeminiModels.cs               # Modèles Gemini (requête/réponse)
├── ImageUrlObject.cs             # Objet de référence image (type + URL)
├── Helpers/
│   └── ApiKeyStorageHelper.cs      # Service de stockage sécurisé des clés API (chiffrement DPAPI)
├── ImageMetadataEmbedder.cs      # Service d'intégration de métadonnées AI (EXIF/XMP/PNG)
├── UserIdHelper.cs               # Gestion des identifiants (PII)
├── Program.cs                    # Point d'entrée
├── ImageGeneratorApp.csproj      # Fichier de projet
├── ImageGeneratorApp.slnx        # Fichier de solution
├── ImageGeneratorApp.Tests/      # Tests unitaires et d'intégration (xUnit + Moq + FluentAssertions)
│   ├── GlobalUsings.cs
│   ├── ImageGeneratorClientTests.cs
│   ├── ApiKeyStorageHelperTests.cs # Tests de stockage et chargement sécurisé des clés API
│   ├── UserIdHelperTests.cs
│   ├── DatabaseHelperTests.cs      # Tests de configuration et initialisation SQLite
│   ├── TemplateRepositoryTests.cs  # Tests de persistance et CRUD SQLite
│   ├── TemplateParserTests.cs      # Tests du moteur d'analyse et de récursion
│   ├── GenerationHistoryRepositoryTests.cs # Tests de persistance et recherche d'historique
│   └── HistoryOrchestratorTests.cs # Tests d'intégration du flux WEBP et de persistance
│   (inclut les tests pour ImageMetadataEmbedder)
└── content/
    └── Grok_Logomark_Dark.png    # Logo Grok
```

## Sécurité

- Les clés API saisies sont sauvegardées localement de manière chiffrée sur le disque via l'API DPAPI de Windows (`ProtectedData`), restreignant l'accès à l'utilisateur Windows courant.
- Le chargement des clés sur le disque est protégé contre les attaques de type TOCTOU (*Time-of-Check to Time-of-Use*) et DoS par épuisement mémoire (avec limite de taille stricte à 4096 octets).
- L'ouverture du fichier de licence dans le dialogue « À propos » est protégée contre le TOCTOU par un verrouillage via `FileStream` et exclut les chemins système absolus des dialogues d'erreur pour éviter la fuite d'informations (Path Disclosure).
- L'identifiant utilisateur envoyé à l'API est un hash opaque (SHA-256) pour protéger les PII.
- Un `device_id` aléatoire est stocké localement dans `%LOCALAPPDATA%\GrokImagineApp\device_id.txt` comme identifiant stable.

## Licence

Copyright (C) 2026 Martin Labelle (@bestter)

Ce programme est un logiciel libre ; vous pouvez le redistribuer et/ou le modifier selon les termes de la **GNU General Public License** telle que publiée par la Free Software Foundation, soit la version 3 de la Licence, soit (à votre choix) toute version ultérieure.

Ce programme est distribué dans l'espoir qu'il sera utile, mais **SANS AUCUNE GARANTIE** ; sans même la garantie implicite de COMMERCIABILITÉ ou d'ADÉQUATION À UN USAGE PARTICULIER. Consultez la GNU General Public License pour plus de détails.

Vous devriez avoir reçu une copie de la GNU General Public License avec ce programme. Si ce n'est pas le cas, consultez <https://www.gnu.org/licenses/>.

Voir le fichier [LICENSE](LICENSE.txt) pour le texte complet de la licence.

Vous pouvez également consulter les informations de licence et ouvrir le fichier `LICENSE.txt` directement depuis l'application (menu **Aide → À propos de Générateur d'image...**).
