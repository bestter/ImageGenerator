# Analyse du Projet : Grok Imagine App

Ce fichier fournit un contexte aux agents IA travaillant sur ce projet.

**Version** : 1.2  
**Dernière mise à jour** : 15 mai 2026  
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

1. Lire intégralement `AGENTS.md` (et `ANTIGRAVITY.md` si présent).
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

---

## 📌 Vue d’ensemble du Projet

- **Nom du projet** : Grok Imagine App
- **Type d’application** : Application de bureau (Windows Forms)
- **Langage principal** : C# (.NET 10)
- **Framework cible** : `net10.0-windows10.0.22621.0`
- **Objectif** : Fournir une interface graphique utilisateur (GUI) pour interagir avec l’API de génération et d’édition d’images de xAI (modèles `grok-imagine-image` et `grok-imagine-image-quality`).

## 📁 Structure du Répertoire
L'application suit une structure standard de projet Windows Forms :

- **`Form1.cs`** : C'est le fichier central contenant la logique métier et l'initialisation de l'interface graphique.
  - Gère les contrôles de l'interface (clés API, prompt, sélection de modèle, résolution, aspect ratio).
  - Implémente la communication HTTP (via `HttpClient`) avec les endpoints de xAI :
    - `https://api.x.ai/v1/images/generations` (pour la génération classique).
    - `https://api.x.ai/v1/images/edits` (pour l'édition multi-tour ou basée sur des images uploadées).
  - Gère l'affichage, la mise en cache (Base64) et la sauvegarde locale des images.
- **`Form1.Designer.cs` & `Form1.resx`** : Fichiers générés automatiquement gérant la disposition des éléments d'interface (bien que `Form1.cs` contienne une méthode personnalisée `InitializeControls()` créant l'interface par le code).
- **`Program.cs`** : Point d'entrée de l'application (contient la méthode `Main`).
- **`GrokImagineApp.csproj`** : Le fichier de définition du projet C# détaillant les dépendances et la configuration de compilation.
- **Dossiers `bin/` et `obj/`** : Dossiers contenant les binaires compilés et les fichiers temporaires de build.

## ⚙️ Fonctionnalités Clés Implémentées
1. **Génération d'images à partir d'un prompt** : Envoi de requêtes structurées (modèle, résolution, format) à l'API xAI.
2. **Support de l'édition d'images (Multi-turn)** : Possibilité de charger jusqu'à 5 images de base, ou de modifier l'image précédemment générée (via `image_url` en format base64).
3. **Paramétrage de l'API** : L'utilisateur fournit sa propre clé API au runtime (Authorization Header Bearer).
4. **Enregistrement des résultats** : L'image générée (reçue en base64) peut être téléchargée au format PNG sur la machine de l'utilisateur.

## 🛠️ Directives de développement (Pour les agents)
- **Architecture** : L'interface graphique est codée manuellement dans `InitializeControls()` (dans `Form1.cs`) plutôt que de s'appuyer exclusivement sur le Designer. Toute modification de l'UI doit idéalement se faire dans cette méthode.
- **Sécurité** : Les clés API sont stockées temporairement dans le champ de texte `txtApiKey` et passées en `Bearer token` dans l'en-tête HTTP. Il n'y a pas de sauvegarde persistante implémentée pour l'instant.
- **Dépendances** : Le projet utilise les bibliothèques standards `System.Net.Http` pour les appels d'API. Pas de dépendances externes complexes (comme RestSharp ou Newtonsoft.Json) repérées.
- **Tests** : Le projet inclut des tests unitaires dans le dossier `GrokImagineApp.Tests`. Tu dois t'assurer que tous les tests passent après chaque modification. Toute modification doit être accompagnée de tests unitaires. Toute modification d'une méthode existante doit être accompagnée de tests unitaires couvrant les cas d'utilisation existants et les nouveaux cas d'utilisation introduits par la modification.
