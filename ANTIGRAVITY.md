# Instructions pour l'Agent IA (Antigravity) - Grok Imagine App

Ce fichier complète `AGENTS.md` et contient les directives spécifiques à la réflexion, au comportement et à la méthodologie de l'agent IA (Antigravity) travaillant sur ce projet. Il s'agit des règles de vol pour l'agent.

## 1. Comportement Général et Éthique de l'Agent

- **Zéro Refactoring Non Sollicité** : Ne modifie jamais une structure existante (comme réorganiser les dossiers, renommer des méthodes publiques, ou abstraire des interfaces) à moins que ce ne soit strictement nécessaire pour accomplir la tâche ou explicitement demandé. Reste chirurgical dans tes interventions.
- **Respect IMPÉRATIF de `.editorconfig`** : Avant de générer, modifier ou formater du code, tu dois consulter et appliquer strictement les règles définies dans le fichier `.editorconfig` (ex: espaces vs tabulations, taille de l'indentation, sauts de ligne, etc.). Aucune déviation n'est tolérée.
- **Autonomie Prudente** : Si tu es confronté à une erreur de compilation ou un test en échec suite à ta modification, tu es autorisé à tenter de le corriger par toi-même de manière itérative, mais n'entre pas dans une boucle infinie de modifications aveugles.
- **Vérification Systématique** : Après toute modification de code C#, tu **DOIS** compiler (`dotnet build`) ou tester (`dotnet test --verbosity normal`) le code avant de rendre la main à l'utilisateur. Ne présume jamais que ton code compile sans avoir vérifié.

## 2. Règles strictes de développement WinForms (Code-First)

- **Sanctuaire du Designer** : Ne touche **JAMAIS** à `Form1.Designer.cs`. Toute la création et le paramétrage de l'interface doivent impérativement se trouver dans la méthode `InitializeControls()` de `Form1.cs`.
- **Calcul Spatial Mental** : Avant d'ajouter un élément UI, calcule mentalement les coordonnées (X, Y) en lisant la position des éléments adjacents. Garde des marges cohérentes (ex: 10-20 pixels d'espacement).
- **Responsive Design Manuel** : N'oublie jamais de définir la propriété `Anchor` (ou `Dock`) pour chaque nouveau contrôle afin que l'interface reste cohérente lorsque la fenêtre est redimensionnée ou maximisée.

## 3. Communication réseau et API

- **Résilience** : L'API peut renvoyer des structures d'erreur inattendues. Garde la logique de parsing JSON défensive (utiliser `TryGetProperty` plutôt que `GetProperty`).
- **Performance** : Utilise l'instance partagée statique de `HttpClient` déjà en place. Ne crée jamais de nouvelles instances de `HttpClient` avec `new` dans le flux principal pour éviter l'épuisement des sockets.

## 4. Politique Linguistique Mixte (Rappel critique)

- **Code Source** : Variables, noms de méthodes, classes, commentaires inline, et messages de commits git -> **ANGLAIS**.
- **Interface Utilisateur (UI)** : Propriété `Text` des labels, boutons, `MessageBox`, et statuts -> **FRANÇAIS**.
- **Méta-Documentation** : `AGENTS.md` et `ANTIGRAVITY.md` -> **FRANÇAIS**.

## 5. Gestion de l'état asynchrone

- L'interface ne doit pas geler pendant les appels réseau. Assure-toi que toutes les méthodes qui communiquent avec l'API sont `async` et appelées avec `await`.
- Gère correctement l'activation/désactivation des boutons (`btnGenerate.Enabled = false;`) au début de la tâche asynchrone, et n'oublie pas de les réactiver dans un bloc `finally`.

## 6. Workflow de Validation Avancé (Anti-Regression)

- **Règle de l'Étendue (Scope Rule)** : Lorsque l'utilisateur demande une modification de code, tu dois d'abord identifier avec précision les fichiers qui pourraient être affectés par ce changement (principe de moindre perturbation). Ne modifie que les fichiers strictement nécessaires à la fonctionnalité demandée.
- **Double Vérification de Cohérence** : Avant de proposer une solution ou de finaliser une étape, tu dois vérifier si cette modification n'entraîne pas une régression dans les fonctionnalités adjacentes ou déjà existantes (par exemple, si tu modifies la méthode `UpdateUI`, vérifie que les boutons continuent de s'activer/désactiver correctement).
- **Itération Contrôlée** : En cas d'erreur de compilation ou de test, tu es autorisé à effectuer des modifications correctives sur les fichiers directement touchés. Cependant, si la correction nécessite des modifications dans d'autres parties du code, tu dois impérativement soumettre ces modifications à l'utilisateur pour validation avant de les appliquer.