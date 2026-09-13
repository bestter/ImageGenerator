# Instructions pour les agents IA — Image Generator App

Ce fichier est la source de vérité commune pour les agents IA qui travaillent dans ce dépôt.

**Version** : 2.1.0
**Dernière mise à jour** : 11 septembre 2026
**Propriétaire** : Martin Labelle (@bestter)

---

## Golden Rules – Règles Absolues (Ne jamais transgresser)

1. **Ne modifie jamais les RÈGLES ET INSTRUCTIONS des fichiers AGENTS.md, ANTIGRAVITY.md et .editorconfig sans autorisation explicite**
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

## Portée et sources de vérité

- Le projet est une application Windows Forms en C# ciblant .NET 10. Le framework exact, les versions et les dépendances se trouvent dans les fichiers `.csproj`; ne les recopier ici que lorsqu'ils constituent une règle durable.
- `ImageProviderCatalog.cs` est la source de vérité pour les identifiants de modèles, les fournisseurs de stockage et les capacités disponibles.
- `ImageGeneratorClient.cs` et les DTO associés définissent les contrats HTTP réellement implémentés.
- Les tests décrivent les cas limites attendus. Le `README.md` décrit le produit pour les utilisateurs; ne duplique pas son contenu ici.
- `ANTIGRAVITY.md` ajoute uniquement des consignes propres à Antigravity. En cas de divergence, `AGENTS.md` prévaut.

## Flux de travail obligatoire

1. Lire intégralement `AGENTS.md`, `.editorconfig` et, pour Antigravity, `ANTIGRAVITY.md`.
2. Vérifier `git status` et préserver les changements existants qui ne font pas partie de la tâche.
3. Lire le code et les tests directement concernés avant de proposer ou d'implémenter une solution.
4. Confirmer le design avant toute modification d'interface. Poser une question si un choix fonctionnel, visuel ou architectural demeure ouvert.
5. Faire le plus petit changement cohérent; éviter les refactorisations, renommages ou nettoyages non demandés.
6. Ajouter ou adapter les tests lorsque le comportement change.
7. Exécuter la validation appropriée et rapporter exactement les commandes lancées et leurs résultats.

Ne crée pas de commit, ne pousse rien et ne publie rien sans demande explicite.

## Architecture et responsabilités

- `Form1.cs` orchestre l'interface, son état et les interactions utilisateur. Il délègue les appels réseau, la persistance et le traitement d'images aux services spécialisés.
- `ImageProviderCatalog.cs` centralise les modèles et leurs capacités. N'éparpille pas de nouvelles comparaisons de chaînes dans l'interface.
- `ImageGeneratorClient.cs` construit les requêtes propres à chaque fournisseur, applique l'authentification et interprète les réponses. Il reçoit un `HttpClient`; conserve cette injection et l'instance partagée utilisée par l'application.
- `ImageGeneratorJsonContext.cs` et les modèles de requête/réponse gardent la sérialisation typée et source-generated.
- `TemplateParser.cs`, `TemplateRepository.cs` et les formulaires de gabarits forment le sous-système de gabarits.
- `HistoryOrchestrator.cs`, les services de traitement d'images et les dépôts d'historique forment le sous-système d'historique local.
- `ApiKeyStorageHelper.cs` est responsable de la persistance sécurisée des clés API.

Respecte ces frontières. Si la tâche exige de les déplacer, obtenir l'accord du propriétaire avant de continuer.

## Interface Windows Forms

- Imaginer d'abord le layout complet : hiérarchie visuelle, dimensions, marges, états et comportement au redimensionnement.
- Créer et configurer les contrôles dans `InitializeControls()` de `Form1.cs`. Ne jamais modifier manuellement `Form1.Designer.cs` pour le layout.
- Garder la logique métier et les gestionnaires d'événements séparés visuellement de `InitializeControls()`.
- Utiliser `Anchor`, `Dock` et des positions relatives appropriées afin que chaque fenêtre demeure utilisable lorsqu'elle est redimensionnée.
- Faire découler l'activation des fonctions des capacités de `ImageProviderCatalog`, notamment pour les images de référence et le multi-turn.
- Conserver une interface utilisateur en français, moderne, claire et accessible. Vérifier les états normal, désactivé, chargement, succès et erreur.
- Libérer correctement les objets `Image`, `Bitmap`, flux, minuteries et autres ressources `IDisposable` remplacées ou possédées par un formulaire.

## Réseau, fournisseurs et sérialisation

- Garder tous les appels aux API dans `ImageGeneratorClient.cs`; l'interface ne construit ni URL, ni en-tête d'authentification, ni charge utile fournisseur.
- Vérifier la documentation officielle actuelle avant de changer un endpoint, un modèle ou un format de requête/réponse.
- Réutiliser le `HttpClient` injecté. Ne pas créer un nouveau client par requête.
- Propager l'annulation et conserver des délais d'attente bornés.
- Traiter les réponses externes comme non fiables : parsing défensif, validation des champs, limites de taille et erreurs compréhensibles.
- Ne jamais envoyer une vraie requête réseau depuis les tests.

## Asynchronisme et réactivité

- Utiliser `async`/`await` de bout en bout pour le réseau et les entrées-sorties; ne pas bloquer le thread UI avec `.Result`, `.Wait()` ou du travail lourd.
- Réserver `Task.Run` au travail CPU ou bloquant qui ne peut pas être rendu asynchrone naturellement.
- Modifier les contrôles WinForms sur le thread UI.
- Pendant une opération, empêcher les soumissions concurrentes et restaurer l'état de l'interface dans un bloc `finally`, y compris après annulation ou erreur.
- Les opérations secondaires comme l'historique peuvent échouer sans masquer un résultat principal réussi, mais l'échec doit être géré explicitement.

## Sécurité et données locales

- Ne jamais journaliser, afficher dans une erreur, mettre en métadonnées ou committer une clé API ou un autre secret.
- Conserver le chiffrement DPAPI par utilisateur et les limites de taille lors de la lecture des clés stockées.
- Pour les écritures dans un dossier connu, isoler le nom avec `Path.GetFileName`, recomposer le chemin avec `Path.Combine` et normaliser avec `Path.GetFullPath` lorsque nécessaire. Ne pas employer `string.StartsWith` comme preuve de confinement.
- Garder les protections contre les fichiers surdimensionnés, le TOCTOU, les chemins malicieux et l'épuisement mémoire.
- Traiter comme non fiables les données des API, de SQLite, des métadonnées et des fichiers sélectionnés par l'utilisateur.

## Langue, style et dépendances

- Respecter strictement `.editorconfig`.
- Écrire en anglais les identifiants, commentaires de code, messages de commit et documents techniques, sauf `AGENTS.md` et `ANTIGRAVITY.md`, qui restent en français.
- Garder en français les textes visibles dans l'interface : libellés, boutons, statuts et messages d'erreur.
- Ne pas introduire de code mort, d'avertissement de compilation ni de changement de formatage sans rapport avec la tâche.
- Toute nouvelle dépendance NuGet ou autre exige une autorisation explicite. Réutiliser d'abord la bibliothèque standard et les dépendances déjà approuvées.

## Tests et validation

- Les tests se trouvent dans `ImageGeneratorApp.Tests` et utilisent xUnit v3.
- Pour les appels HTTP, injecter un `HttpMessageHandler` simulé; couvrir l'URL, la méthode, l'authentification, la charge utile, le succès, les erreurs fournisseur, l'annulation et le JSON invalide selon la portée du changement.
- Tout nouveau comportement ou changement d'une API publique exige des tests nominaux et des tests d'erreur pertinents.
- Pour une modification de code, exécuter au minimum :
  1. `dotnet build ImageGeneratorApp.csproj`
  2. `dotnet run --project ImageGeneratorApp.Tests/ImageGeneratorApp.Tests.csproj`
  3. `git diff --check`
- Pour une modification exclusivement documentaire, `git diff --check` et une relecture de cohérence suffisent, sauf demande contraire.
- Ne jamais annoncer « aucun avertissement » ou « tous les tests passent » sans résultat observé pendant la tâche.

## Livraison

Dans le compte rendu final :

- résumer les changements et les décisions importantes;
- nommer les fichiers modifiés;
- indiquer les validations réellement exécutées;
- signaler clairement tout risque, hypothèse, test omis ou décision encore requise.


## Commit
- Donne toujours une description claire des changements
- Signe tes commits avec le nom de ton outil (ex: Antigravity, Vs Code, Claude Code, Codex, etc.) et le nom de ton modèle exact: (ex: Gemini 3.7 Flash, ChatGpt 6 Astra, etc.).
