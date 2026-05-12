# Analyse du Projet : Grok Imagine App

Ce fichier fournit un contexte aux agents IA travaillant sur ce projet.

## 📌 Vue d'ensemble du Projet
- **Nom du projet** : Grok Imagine App
- **Type d'application** : Application de bureau (Windows Forms)
- **Langage principal** : C# (.NET)
- **Objectif** : Fournir une interface graphique utilisateur (GUI) pour interagir avec l'API de génération et d'édition d'images de xAI (modèles `grok-imagine-image` et `grok-imagine-image-pro`).

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
- **Dépendances** : Le projet utilise les bibliothèques standards `System.Net.Http` et `System.Text.Json` pour les appels d'API. Pas de dépendances externes complexes (comme RestSharp ou Newtonsoft.Json) repérées.
