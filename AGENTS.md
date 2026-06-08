# Analyse du Projet : Image Generator App

Ce fichier fournit un contexte aux agents IA travaillant sur ce projet.

**Version** : 1.8
**Dernière mise à jour** : 27 mai 2026
**Propriétaire** : Martin Labelle (@bestter)

---

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Ne modifie jamais les fichiers AGENTS.md, ANTIGRAVITY.md et .editorconfig sans autorisation explicite**
   Ces fichiers sont la source de vérité pour l’agent IA.
   **Toute modification nécessite une autorisation claire et explicite du propriétaire du projet** (exemple : « Tu peux réécrire AGENTS.md » ou « Mets à jour la section X »). Sans cette autorisation, tu n’y touches pas.

2. **Minimalisme extrême**
   Priorise toujours un code fonctionnel, durable et facilement maintenable.
   **Aucune nouvelle dépendance** (NuGet ou autre) ne doit être ajoutée sans validation explicite, même pour des utilitaires « petits ».

3. **Demande avant d’improviser**
   Si une fonctionnalité, un pattern ou une décision d’architecture n’est pas clairement documenté dans `AGENTS.md` ou `ANTIGRAVITY.md` → **pose la question** au lieu de deviner.

4. **Respecte .editorconfig + langue dans le code**
   Avant de générer ou de modifier du code, analyse et respecte **impérativement** les règles du fichier `.editorconfig`.
   Tous les commentaires de code, messages de commit et documentation technique doivent être rédigés **en anglais**, à l’exception des fichiers `AGENTS.md` et `ANTIGRAVITY.md` qui doivent rester en français.

---

## 🔄 Flux de Travail Standard pour les Agents IA

Avant toute modification, suis toujours cet ordre :

1. Lire intégralement `AGENTS.md`, `.editorconfig` (et `ANTIGRAVITY.md` si présent).
2. Analyser le besoin et **imaginer le design** (surtout pour l’UI).
3. Implémenter **uniquement** selon les règles définies dans ce document.
4. Exécuter `dotnet test --verbosity normal` et obtenir **100 % de succès**.
5. Si le moindre doute existe sur l’architecture, le design ou une décision non documentée → **poser la question immédiatement**.

---

## 🎨 Stratégie UI/UX (Windows Forms – Design-First)

Pour toute modification de l’interface utilisateur, l’agent doit impérativement suivre cette approche hiérarchique :

1. **Édition via `InitializeControls()` (Priorité 1)**
   - Toute modification de layout, ajout/suppression de contrôles ou ajustement de propriétés visuelles doit se faire dans la méthode `InitializeControls()` située dans `Form1.cs`.
   - Le fichier `Form1.Designer.cs` contient **uniquement** le code standard généré par le Designer (`InitializeComponent()`) et ne doit **jamais** être modifié manuellement pour le layout.
   - **Règle stricte** : La logique métier et les gestionnaires d’événements restent dans `Form1.cs`, séparés visuellement de `InitializeControls()`.

2. **Design-First, pas Code-First**
   Avant de générer du code de formulaire, l’agent doit **imaginer** le design complet (positionnement, tailles, ancrages) pour s’assurer qu’il est cohérent et fonctionnel.

3. **Mise à l’échelle et résolution (Scaling)**
   L’application doit être entièrement redimensionnable.
   Les contrôles doivent utiliser des ancrages (`Anchor`) et des positionnements relatifs.

4. **Gestion de l'état visuel selon le modèle**
   - L'interface doit être réactive au modèle sélectionné. Si un modèle ne supporte pas certaines fonctionnalités (ex: Nano Banana Pro ne supporte pas l'édition d'image), les contrôles associés (bouton d'ajout d'images, checkbox multi-turn) doivent être dynamiquement désactivés (Enabled = false) dans l'événement de changement de sélection du ComboBox de modèle.

---

## 📌 Vue d’ensemble du Projet

- **Nom du projet** : Image Generator App
- **Type d’application** : Application de bureau (Windows Forms)
- **Langage principal** : C# (.NET 10)
- **Framework cible** : `net10.0-windows10.0.22621.0`
- **Namespace** : `ImageGeneratorApp`
- **Objectif** : Fournir une interface graphique utilisateur (GUI) multi-provider pour la génération d’images par IA, supportant actuellement :
  - **Grok Imagine (xAI)** : modèles `grok-imagine-image` et `grok-imagine-image-pro` via l’API xAI (`https://api.x.ai/v1/images/`).
  - **Nano Banana Pro (Google)** : modèle `nano-banana-pro` via l’API Gemini (`https://generativelanguage.googleapis.com/v1beta/`).

## 📁 Structure du Répertoire

L'application suit une structure modulaire séparant l'UI de la logique réseau :

- **`Form1.cs`** : Fichier gérant exclusivement la couche UI (Interface Utilisateur).
  - Gère les contrôles de l'interface (clés API, prompt, sélection de modèle, résolution, aspect ratio).
  - Gère l'affichage, la mise en cache (Base64) et la sauvegarde locale asynchrone non bloquante des images.
  - Gère le moteur de validation visuelle (bordure rouge de 2px autour du prompt sur perte de focus ou survol du bouton de génération, réinitialisation instantanée lors de la saisie).
  - Gère le bouton de génération (activation/désactivation dynamique selon l'API key, le prompt, la validité des gabarits ou l'état de génération).
  - Intègre une autocomplétion mid-string flottante au caret (`lstAutocomplete`) et un aperçu au survol via info-bulle (`toolTipGenerate`).
  - Structuré de manière modulaire : le gestionnaire de clic `BtnGenerate_Click` est subdivisé en méthodes spécialisées (`PrepareReferenceImagesAsync`, `UpdateUIWithGeneratedImage`, `HandleGenerationException`).
  - Délègue la logique métier et les appels réseau à la couche client.
- **`ImageGeneratorClient.cs`** : Implémente la communication HTTP (via `HttpClient`) avec les endpoints des différents providers et gère le parsing JSON.
  - **xAI (Grok Imagine)** : `https://api.x.ai/v1/images/generations` (génération) et `https://api.x.ai/v1/images/edits` (édition multi-tour).
  - **Google (Nano Banana Pro)** : `https://generativelanguage.googleapis.com/v1beta/models/gemini-3-pro-image-preview:generateContent`.
  - **Refactorisation propre** : La méthode principale `GenerateImageAsync` fait moins de 40 lignes et délègue les tâches à des helpers dédiés (`PrepareRequest`, `ParseErrorResponseAsync`, `ParseSuccessResponseAsync`) assurant modularité et performance.
- **`ImageGeneratorRequest.cs`** : Modèle de requête pour l'API xAI (le modèle Nano Banana utilise un format de requête distinct construit directement dans le client).
- **`ImageGeneratorResponse.cs`** : Modèle de réponse pour l'API xAI.
- **`ImageGeneratorException.cs`** : Exception personnalisée pour les erreurs API (tous providers).
- **`ImageGeneratorJsonContext.cs`** : Contexte de sérialisation JSON source-generated pour la performance.
- **`GeminiModels.cs`** : Modèles de requête/réponse spécifiques au provider Google Gemini (`GeminiRequest`, `GeminiResponse`, `GeminiContent`, `GeminiPart`, `GeminiInlineData`, `GeminiGenerationConfig`, `GeminiImageConfig`, `GeminiCandidate`).
- **`ImageUrlObject.cs`** : Modèle d'objet image de référence utilisé pour les éditions d'images (contient type et URL).
- **`ImageMetadataEmbedder.cs`** : Service responsable de l'intégration automatique des métadonnées de génération (EXIF, XMP, chunks PNG) lors de l'export des images.
- **`UserIdHelper.cs`** : Utilitaire asynchrone pour la gestion des identifiants (notamment pour la protection PII) avec lectures/écritures non bloquantes de fichiers (`GetOpaqueUserIdAsync`).
- **`Helpers/ApiKeyStorageHelper.cs`** : Service de gestion et de stockage sécurisé des clés API avec chiffrement local DPAPI (Windows). Protégé contre les vulnérabilités TOCTOU et DoS par épuisement mémoire via un flux de taille limitée (4096 octets max).
- **`DatabaseHelper.cs`** : Gère la création et l'initialisation de la base SQLite `templates.db` et configure Dapper avec un mapping global snake_case vers PascalCase (gère également la table `GenerationHistory`).
- **`TemplateModel.cs`** : Modèle entité représentant un gabarit de prompt stocké en base de données.
- **`TemplateRepository.cs`** : Gère l'accès aux données (Dapper) avec des opérations CRUD asynchrones et le suivi des statistiques d'usage.
- **`TemplateParser.cs`** : Moteur d'analyse récursif et sécurisé pour étendre les balises de templates (`{key}` ou `{key:param1}`) avec limite de 20 itérations. Lève des exceptions précises lors d'erreurs de syntaxe (`FormatException`), de récursion infinie (`InvalidOperationException`) ou de clé manquante (`KeyNotFoundException`).
- **`TemplatesManagerForm.cs`** : Vue WinForms (codée programmatiquement) de gestion de la liste des gabarits avec recherche/filtre en temps réel.
- **`TemplateEditorForm.cs`** : Dialogue WinForms (codé programmatiquement) d'ajout/édition sécurisé avec détection de collisions de clés.
- **`GenerationHistoryModel.cs`** : Modèle entité représentant un enregistrement de l'historique de génération dans la base SQLite.
- **`GenerationHistoryRepository.cs`** : Gère l'accès aux données (Dapper) pour l'historique avec opérations asynchrones (insertion, liste, recherche de prompts ou modèles).
- **`ImageProcessingService.cs`** : Gère le décodage et l'encodage d'images (SixLabors.ImageSharp) en tâche d'arrière-plan, notamment la compression en WEBP à 80% avec injection de métadonnées et la conversion compatible GDI+ (clonage Bitmap) pour PictureBox.
- **`HistoryOrchestrator.cs`** : Service unifié coordonnant la sauvegarde locale d'image en WEBP avec injection automatique de métadonnées EXIF/XMP et journalisation SQLite.
- **`HistoryViewerForm.cs`** : Vue WinForms scindée (code-first) permettant de rechercher, lister et prévisualiser de manière performante et sécurisée (concurrency token).
- **`AboutForm.cs`** : Dialogue « À propos » (code-first) affichant les informations de version, copyright et l'avis de licence GPL v3 en français, avec bouton d'ouverture directe de LICENSE.txt.
- **`Form1.Designer.cs` & `Form1.resx`** : Fichiers générés automatiquement gérant la disposition des éléments d'interface (bien que `Form1.cs` contienne une méthode personnalisée `InitializeControls()` créant l'interface par le code et incluant le bouton d'historique).
- **`Program.cs`** : Point d'entrée de l'application (contient la méthode `Main`).
- **`ImageGeneratorApp.csproj`** : Le fichier de définition du projet C# détaillant les dépendances et la configuration de compilation.
- **Dossiers `bin/` et `obj/`** : Dossiers contenant les binaires compilés et les fichiers temporaires de build.

## ⚙️ Fonctionnalités Clés Implémentées

1. **Génération d'images multi-provider** : Envoi de requêtes structurées (modèle, résolution, format) à l'API xAI (Grok Imagine) ou Google (Nano Banana Pro).
**2. Support de l'édition d'images (Multi-références et Multi-turn)**

L’édition d’images est disponible exclusivement via le endpoint `POST /v1/images/edits` et n’est supportée que par les modèles Grok Imagine (`grok-imagine-image` et `grok-imagine-image-pro`).

- **Édition avec références multiples** : Une même requête peut accepter **jusqu’à 3 images de référence**. Cela permet de combiner des sujets, transférer des styles ou composer des scènes complexes à partir de plusieurs sources visuelles.
- **Format d’entrée des images** : Les images de référence doivent être fournies soit via une **URL publique**, soit sous forme de **data URI base64** (ex. : `data:image/png;base64,...`). Selon le mode d’appel (SDK ou HTTP direct), cela se fait via le paramètre `image_url` ou via un objet `image` de type `image_url`.
- **Édition multi-turn (itérative)** : L’API supporte un flux d’édition itératif. L’image générée par une requête d’édition peut être réutilisée directement comme image d’entrée pour une requête suivante. Ce mécanisme permet un raffinement progressif (ajout de détails, corrections, changements de style, ajustements compositionnels, etc.).
- **Limitation importante** : L’édition d’images **n’est pas supportée** par le provider Google (Nano Banana Pro). Seuls les modèles Grok Imagine peuvent utiliser le endpoint `/v1/images/edits`.

1. **Paramétrage de l'API** : L'utilisateur fournit sa propre clé API au runtime. Le label du champ s'adapte au provider sélectionné (« Clé API xAI » pour Grok, « Clé Google Cloud » pour Nano Banana).
2. **Enregistrement des résultats** : L'image générée (reçue en base64) peut être téléchargée au format PNG ou JPEG sur la machine de l'utilisateur.
3. **Intégration automatique de métadonnées AI** : Lors de l'export, les métadonnées de génération (prompt, modèle, date/heure, etc.) sont automatiquement intégrées dans l'image via EXIF, XMP et chunks PNG. Cette fonctionnalité est implémentée dans `ImageMetadataEmbedder.cs` et utilise la dépendance validée SixLabors.ImageSharp.
   - **Exportation asynchrone non bloquante** : La sauvegarde et l'exportation des images s'effectuent de façon asynchrone (`BtnSave_Click` utilise `File.WriteAllBytesAsync`). De plus, le décodage Base64, l'encodage d'image ImageSharp et l'injection de métadonnées sont entièrement déportés dans un thread d'arrière-plan (`Task.Run`), garantissant une interface fluide à 100%, sans aucun gel de l'affichage.
4. **Système de Gabarits (Templates) & Autocomplétion UX** :
   - **Stockage SQLite & Dapper** : Base de données locale `templates.db` gérant l'intégrité, l'indexation et la rapidité des gabarits.
   - **Analyse Récursive Paramétrée** : Parser mid-string supportant les balises récursives `{key}` ou `{key:param1:param2}` pour injecter des variables, avec une limite de sécurité à 20 boucles.
   - **Moteur de validation syntaxique & exceptions structurées** : Le parser effectue un contrôle syntaxique rigoureux (accolades mal appairées, imbriquées ou manquantes) levant des `FormatException`. Il interdit les boucles de récursions infinies en limitant la profondeur à 20 itérations (`InvalidOperationException`) et lève des `KeyNotFoundException` en cas de clés absentes de la base SQLite.
   - **Validation visuelle dynamique UX** : Le champ de saisie du prompt `txtPrompt` est encadré d'une bordure rouge de 2 pixels en cas d'erreur de validation (détectée à la perte de focus/blur ou au survol du bouton de génération). La bordure rouge s'efface instantanément au changement de texte (`TextChanged`) pour préserver le confort de frappe de l'utilisateur.
   - **Contrôle d'activation du bouton de génération** : Le bouton de génération `btnGenerate` est automatiquement désactivé si la clé API ou le prompt est vide, si le prompt contient des erreurs de syntaxe, des clés inconnues, ou des récursions infinies. Il est également verrouillé pendant toute la durée de l'appel d'API asynchrone.
   - **Mise en cache asynchrone de l'autocomplétion** : Les clés de templates utilisées pour l'autocomplétion contextuelle au caret sont stockées en cache local et rafraîchies de façon asynchrone (`RefreshTemplateKeysCacheAsync`) dès la fermeture du gestionnaire de modèles, évitant tout appel de base de données à chaque touche pressée.
   - **UI responsive sans Designer** : Dialogues de gestion et d'édition entièrement programmés en C#, avec validation stricte de formulaires.
   - **Autocomplétion Contextuelle (UX)** : Apparition au caret d'un `ListBox` d'autocomplétion mid-string lors de la saisie de `{` avec navigation au clavier et insertion avec accolades auto-fermées.
   - **Aperçu Info-bulle (Hover)** : Info-bulle dynamique sur le bouton de génération pour prévisualiser le prompt entièrement résolu avant envoi.
5. **Système d'Historique de Génération (Local)** :
   - **Enregistrement Automatique Silencieux** : Chaque génération d'image réussie déclenche une tâche d'arrière-plan asynchrone non bloquante qui compresse, injecte les métadonnées et journalise la génération dans SQLite.
   - **Compression WEBP (Qualité 80)** : Conversion automatique des octets bruts (PNG/JPEG) reçus des API en format WEBP compressé dans le dossier `%LocalAppData%/ImageGeneratorApp/HistoryImages/` pour préserver l'espace disque.
   - **Injection de Métadonnées Standards** : Préservation complète de la traçabilité de l'image d'historique en appliquant le profil de métadonnées EXIF et XMP standardisé (prompt, modèle, date de création, etc.) via ImageSharp.
   - **Décodage WinForms GDI+ sans verrou** : Service de chargement asynchrone qui convertit le format WEBP (non géré nativement par PictureBox) en Bitmap BMP cloné, libérant instantanément les flux de fichiers sous-jacents pour éviter les blocages mémoires ou les crashes GDI+.
   - **Visualiseur Split View Premium** : Dialogue d'exploration scindé codé manuellement (code-first) offrant une recherche textuelle en temps réel (requêtes SQL `LIKE` Dapper), des bordures douces anti-aliasées (GDI+), et un formateur de métadonnées JSON avec coloration monospace (Consolas).

## Directives

- **Clé API** : Étant donné que les formats de clés API diffèrent entre xAI et Google, le champ txtApiKey doit être utilisé de manière agnostique dans l'UI, mais le client ImageGeneratorClient a l'entière responsabilité de formater le header HTTP correct selon le provider cible.
- **Tests** : Tous les tests réseau doivent utiliser Moq et un HttpMessageHandler mocké pour intercepter les requêtes HTTP. Aucun test ne doit frapper les API réelles de xAI ou de Google Cloud.

## 🛠️ Directives de développement (Pour les agents)

- **Architecture** : L'interface graphique est codée manuellement dans `InitializeControls()` (dans `Form1.cs`) plutôt que de s'appuyer exclusivement sur le Designer. Toute modification de l'UI doit idéalement se faire dans cette méthode. La logique réseau doit être maintenue séparée dans la couche client (`ImageGeneratorClient.cs` et associés).
- **Multi-provider** : Le client `ImageGeneratorClient` gère le routage vers le bon endpoint selon le modèle sélectionné. Pour ajouter un nouveau provider, étendre la logique conditionnelle dans `GenerateImageAsync()`.
- **Sécurité** : Les clés API sont stockées temporairement dans le champ de texte `txtApiKey`, passées via l'en-tête HTTP approprié (`Bearer` pour xAI, `x-goog-api-key` pour Google), et persistées de manière sécurisée (chiffrement DPAPI utilisateur via `ProtectedData`) dans LocalApplicationData via `ApiKeyStorageHelper`. Le chargement applique une protection stricte contre le TOCTOU et les fichiers malicieux surdimensionnés (limite de 4096 octets).
- **Dépendances** : Le projet utilise les bibliothèques standards `System.Net.Http` pour les appels d'API et `System.Text.Json` pour la manipulation des données JSON. Une dépendance externe validée a été ajoutée : `SixLabors.ImageSharp` (version 3.1.12) pour la gestion robuste des métadonnées EXIF/XMP/PNG. Toute nouvelle dépendance doit faire l'objet d'une validation explicite.
- **Tests** : Le projet inclut des tests unitaires dans le dossier `ImageGeneratorApp.Tests` (ex: `ImageGeneratorClientTests.cs`). Toute modification d’une méthode publique existante ou ajout de fonctionnalité doit être accompagnée de tests unitaires couvrant les cas nominaux + erreurs (API key invalide, rate limit, JSON mal formé, etc.). Objectif : 100 % des tests verts en local avant tout commit.
- **Design** : L'application doit être visuellement attrayante et moderne.
- **Extensibilité** : Pour ajouter un nouveau provider, étendre uniquement ImageGeneratorClient.GenerateImageAsync() et ajouter les tests correspondants. Pas de nouvelle dépendance sans validation explicite.
- **Qualité & Style** : Respect strict du .editorconfig. Tous les commentaires techniques en anglais. Pas de code mort, pas de warnings à la compilation.
