# Analyse du Projet : Grok Imagine App

Ce fichier fournit un contexte aux agents IA travaillant sur ce projet.

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Ne modifie jamais les fichiers AGENTS.md et ANTIGRAVITY.md sans autorisation explicite**
   Ces fichiers sont la source de vérité pour l'agent IA et doivent être respectés à la lettre.

2. **Minimalisme extrême**
   Priorise toujours un code fonctionnel, durable et facilement maintenable. Évite toute sur-ingénierie. **Aucune nouvelle dépendance** (npm ou NuGet) ne doit être ajoutée sans validation explicite (même pour des utilitaires « petits »).

3. **Demande avant d’improviser**
   Si une fonctionnalité, un pattern ou une décision d’architecture n’est pas clairement documenté dans `AGENTS.md` ou `ANTIGRAVITY.md` → **pose la question** au lieu de deviner.

4. **Respecte .editorconfig + langue dans le code**
   Avant de générer ou de modifier du code, analyse et respecte **impérativement** les règles du fichier `.editorconfig`. Tous les commentaires de code, messages de commit et documentation technique doivent être rédigés **en anglais**, à l'exception des fichiers `AGENTS.md` et `ANTIGRAVITY.md` qui doivent rester en français.

## 🎨 Stratégie UI/UX (Windows Forms – Design-First)

Pour toute modification de l'interface utilisateur, l'agent doit impérativement suivre cette approche hiérarchique :

1. **Édition via `InitializeControls()` (Priorité 1)**
   - Toute modification de layout, ajout/suppression de contrôles ou ajustement de propriétés visuelles doit se faire dans la méthode `InitializeControls()` située dans `Form1.cs`.
   - Le fichier `Form1.Designer.cs` contient uniquement le code standard généré par le Designer (`InitializeComponent()`) et ne doit **pas** être utilisé pour le layout.
   - **Règle stricte** : La logique métier et les gestionnaires d'événements restent dans `Form1.cs`, mais séparés visuellement de `InitializeControls()`.

2. **Design-First, pas Code-First**
   - Avant de générer du code de formulaire, l'agent doit **imaginer** le design complet (positionnement, tailles, ancrages) pour s'assurer qu'il est cohérent et fonctionnel.
   - Utiliser des outils visuels ou des croquis mentaux pour valider l'emplacement de chaque contrôle avant de générer le code correspondant.

3. **Mise à l'échelle et résolution (Scaling)**
   - L'application doit être entièrement redimensionnable et s'adapter à différentes résolutions d'écran.
   - Les contrôles doivent utiliser des ancrages (`Anch`) et des positionnements relatifs pour garantir une mise à l'échelle correcte.


## 📌 Vue d'ensemble du Projet
- **Nom du projet** : Grok Imagine App
- **Type d'application** : Application de bureau (Windows Forms)
- **Langage principal** : C# (.NET 10)
- **Framework cible** : `net10.0-windows10.0.22621.0`
- **Objectif** : Fournir une interface graphique utilisateur (GUI) pour interagir avec l'API de génération et d'édition d'images de xAI (modèles `grok-imagine-image` et `grok-imagine-image-quality`).

## 📁 Structure du Répertoire
L'application suit une structure de solution .NET multi-projets :

### Projet principal (`GrokImagineApp.csproj`)
- **`Form1.cs`** : Fichier de l'interface graphique contenant l'initialisation des contrôles (via `InitializeControls()`) et la logique d'interaction utilisateur (gestion des événements, affichage des images, sauvegarde).
  - Gère les contrôles de l'interface (clés API, prompt, sélection de modèle, résolution, aspect ratio, édition multi-tour).
  - Délègue la communication API au client `GrokImagineClient`.
  - Gère l'affichage, la mise en cache (Base64) et la sauvegarde locale des images.
- **`Form1.Designer.cs` & `Form1.resx`** : Fichiers générés automatiquement par le Designer. L'interface réelle est construite par code dans `InitializeControls()` (dans `Form1.cs`). Toute modification de l'UI doit se faire dans cette méthode.
- **`GrokImagineClient.cs`** : Client HTTP encapsulant toute la logique de communication avec les endpoints de l'API xAI :
  - `https://api.x.ai/v1/images/generations` (pour la génération classique).
  - `https://api.x.ai/v1/images/edits` (pour l'édition multi-tour ou basée sur des images uploadées).
  - Gère la validation des entrées (clé API, prompt), la sérialisation JSON des requêtes et le parsing robuste des réponses d'erreur.
- **`GrokImagineRequest.cs`** : Modèle de données (DTO) pour la requête API, avec attributs `[JsonPropertyName]` et `[JsonIgnore]` pour la sérialisation.
- **`GrokImagineException.cs`** : Exception personnalisée incluant un `StatusCode` HTTP, utilisée pour propager les erreurs API de façon structurée.
- **`UserIdHelper.cs`** : Utilitaire de sécurité générant un identifiant utilisateur opaque (hash SHA-256 avec sel) à partir du nom d'identité Windows, pour protéger les informations personnelles (PII).
- **`Program.cs`** : Point d'entrée de l'application (contient la méthode `Main`).
- **`GrokImagineApp.csproj`** : Fichier de définition du projet C# ciblant `net10.0-windows10.0.22621.0` avec `EnableWindowsTargeting` pour le build cross-plateforme en CI.
- **`GrokImagineApp.slnx`** : Fichier de solution regroupant le projet principal et le projet de tests.
- **Dossiers `bin/` et `obj/`** : Dossiers contenant les binaires compilés et les fichiers temporaires de build.

### Projet de tests (`GrokImagineApp.Tests/`)
- **Framework de test** : xUnit avec Moq (mocking) et FluentAssertions (assertions expressives).
- **`GrokImagineClientTests.cs`** : Tests unitaires pour `GrokImagineClient`, couvrant :
  - Appels aux endpoints corrects (generations vs edits).
  - Validation des entrées (clé API vide, prompt vide, caractères interdits).
  - Parsing des erreurs API (message en string, message en objet JSON, JSON malformé).
  - Extraction correcte du Base64 depuis la réponse.
- **`UserIdHelperTests.cs`** : Tests unitaires pour `UserIdHelper` (déterminisme, unicité, gestion du null).
- **`GlobalUsings.cs`** : Imports globaux (`Xunit`, `Moq`, `FluentAssertions`).
- **`GrokImagineApp.Tests.csproj`** : Projet de tests référençant directement les fichiers source du projet principal via `<Compile Include="..\" />` (pas de référence projet classique).

### CI/CD (`.github/workflows/`)
- **`codeql.yml`** : Pipeline GitHub Actions pour l'analyse CodeQL (sécurité) déclenchée sur push/PR vers `main`. Inclut le setup .NET 10 et le build manuel pour le projet WinForms.

## ⚙️ Fonctionnalités Clés Implémentées
1. **Génération d'images à partir d'un prompt** : Envoi de requêtes structurées (modèle, résolution, format) à l'API xAI via `GrokImagineClient`.
2. **Support de l'édition d'images (Multi-turn)** : Possibilité de charger jusqu'à 5 images de base, ou de modifier l'image précédemment générée (via `image_url` en format base64).
3. **Paramétrage de l'API** : L'utilisateur fournit sa propre clé API au runtime (Authorization Header Bearer).
4. **Enregistrement des résultats** : L'image générée (reçue en base64) peut être téléchargée au format PNG sur la machine de l'utilisateur.
5. **Protection des PII** : L'identifiant utilisateur envoyé à l'API est un hash opaque (SHA-256) du nom Windows, jamais le nom réel.
6. **Gestion robuste des erreurs API** : Parsing sécurisé du JSON d'erreur supportant les cas où `error.message` est une chaîne ou un objet.

## 🛠️ Directives de développement (Pour les agents)
- **Architecture** : L'interface graphique est codée manuellement dans `InitializeControls()` (dans `Form1.cs`) plutôt que de s'appuyer exclusivement sur le Designer. Toute modification de l'UI doit idéalement se faire dans cette méthode.
- **Séparation des responsabilités** : La logique HTTP/API est dans `GrokImagineClient.cs`, pas dans `Form1.cs`. Toute nouvelle fonctionnalité d'interaction avec l'API doit être ajoutée dans le client, et non directement dans le formulaire.
- **Sécurité** : Les clés API sont stockées temporairement dans le champ de texte `txtApiKey` et passées en `Bearer token` dans l'en-tête HTTP. Il n'y a pas de sauvegarde persistante implémentée pour l'instant. La validation des clés API inclut le rejet des caractères de retour à la ligne (protection CRLF injection).
- **Dépendances** : Le projet principal utilise uniquement les bibliothèques standards .NET (`System.Net.Http`, `System.Text.Json`). Le projet de tests utilise des dépendances externes : `xUnit`, `Moq`, `FluentAssertions`.
- **HttpClient** : Une instance statique partagée de `HttpClient` est utilisée dans `Form1.cs` pour éviter l'épuisement de sockets (socket exhaustion). `GrokImagineClient` reçoit son `HttpClient` par injection de dépendances (constructeur).

## ✅ Politique de tests et Pull Requests

### Règles obligatoires avant toute PR
1. **Tous les tests doivent passer** : Avant de soumettre une Pull Request, exécuter `dotnet test` à la racine du projet et s'assurer que **100% des tests passent sans échec ni erreur**.
2. **Aucune régression tolérée** : Une PR qui casse un test existant doit être corrigée avant le merge. Aucun test en échec ne doit être ignoré ou désactivé pour contourner un problème.

### Règles de couverture des changements
3. **Tout changement de logique métier doit être accompagné de tests** : Toute modification dans `GrokImagineClient.cs`, `UserIdHelper.cs`, `GrokImagineRequest.cs` ou `GrokImagineException.cs` doit être reflétée par des tests unitaires nouveaux ou mis à jour dans le projet `GrokImagineApp.Tests`.
4. **Nouveaux cas limites** : Si un bug est corrigé, un test reproduisant le bug doit être ajouté pour empêcher toute régression future.
5. **Mise à jour des tests existants** : Si le comportement d'une méthode testée change (signature, valeurs de retour, messages d'erreur), les tests correspondants doivent être mis à jour en conséquence.
6. **Commande de vérification** : Toujours exécuter la commande suivante avant de finaliser une PR :
   ```bash
   dotnet test --verbosity normal
   ```
