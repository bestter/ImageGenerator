# Image Generator App

Application de bureau Windows Forms (.NET 10) pour la génération d'images par IA, supportant plusieurs providers.

## Providers supportés

| Provider | Modèle(s) | API | Édition multi-tour |
|---|---|---|---|
| **Grok Imagine (xAI)** | `grok-imagine-image`, `grok-imagine-image-pro` | `https://api.x.ai/v1/images/` | ✅ Oui (jusqu'à 5 images) |
| **Nano Banana Pro (Google)** | `nano-banana-pro` | `https://generativelanguage.googleapis.com/v1beta/` | ❌ Non |

## Fonctionnalités

- **Génération d'images multi-provider** : Sélectionnez le modèle souhaité et fournissez un prompt textuel pour générer une image.
- **Édition d'images (Multi-turn)** : Chargez jusqu'à 5 images de base ou modifiez l'image précédemment générée *(Grok Imagine uniquement)*.
- **Résolutions multiples** : Support 1k et 2k.
- **Ratios d'aspect variés** : 1:1, 16:9, 9:16, 4:3, 3:2, 20:9.
- **Sauvegarde locale** : Enregistrez l'image générée en PNG.

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

```
├── Form1.cs                      # Interface utilisateur (WinForms code-first)
├── ImageGeneratorClient.cs       # Client HTTP multi-provider
├── ImageGeneratorRequest.cs      # Modèle de requête (xAI)
├── ImageGeneratorResponse.cs     # Modèle de réponse (xAI)
├── ImageGeneratorException.cs    # Exception personnalisée (tous providers)
├── ImageGeneratorJsonContext.cs  # Sérialisation JSON source-generated
├── UserIdHelper.cs               # Gestion des identifiants (PII)
├── Program.cs                    # Point d'entrée
├── ImageGeneratorApp.csproj      # Fichier de projet
├── ImageGeneratorApp.slnx        # Fichier de solution
├── ImageGeneratorApp.Tests/      # Tests unitaires (xUnit + Moq + FluentAssertions)
│   ├── ImageGeneratorClientTests.cs
│   └── UserIdHelperTests.cs
└── content/
    └── Grok_Logomark_Dark.png    # Logo Grok
```

## Sécurité

- Les clés API sont saisies au runtime et **ne sont jamais persistées** sur le disque.
- L'identifiant utilisateur envoyé à l'API est un hash opaque (SHA-256) pour protéger les PII.
- Un `device_id` aléatoire est stocké localement dans `%LOCALAPPDATA%\GrokImagineApp\device_id.txt` comme identifiant stable.

## Licence

Copyright (C) 2026 Martin Labelle (@bestter)

Ce programme est un logiciel libre ; vous pouvez le redistribuer et/ou le modifier selon les termes de la **GNU General Public License** telle que publiée par la Free Software Foundation, soit la version 3 de la Licence, soit (à votre choix) toute version ultérieure.

Ce programme est distribué dans l'espoir qu'il sera utile, mais **SANS AUCUNE GARANTIE** ; sans même la garantie implicite de COMMERCIABILITÉ ou d'ADÉQUATION À UN USAGE PARTICULIER. Consultez la GNU General Public License pour plus de détails.

Vous devriez avoir reçu une copie de la GNU General Public License avec ce programme. Si ce n'est pas le cas, consultez <https://www.gnu.org/licenses/>.

Voir le fichier [LICENSE](gpl-3.0.txt) pour le texte complet de la licence.
