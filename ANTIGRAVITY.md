# Instructions propres à Antigravity — Image Generator App

Lis intégralement `AGENTS.md` et `.editorconfig` avant d'agir. `AGENTS.md` contient toutes les règles communes et prévaut sur ce complément; ne les duplique pas ici.

## Méthode de travail

1. Reformule mentalement le résultat attendu et repère les décisions non documentées.
2. Inspecte les fichiers directement concernés, leurs appelants et leurs tests avant de modifier quoi que ce soit.
3. Pour l'interface, établis d'abord le layout, les états et le comportement au redimensionnement.
4. Applique un changement chirurgical et cohérent avec les frontières existantes.
5. Après chaque échec causé par ton changement, lis le diagnostic complet et corrige la cause démontrée. N'enchaîne pas des essais spéculatifs.
6. Si la correction déborde la portée initiale ou exige une décision d'architecture, arrête-toi et demande l'accord du propriétaire.
7. Termine avec les validations et le compte rendu exigés par `AGENTS.md`.

## Discipline de raisonnement

- Distingue les faits observés dans le dépôt, les exigences de l'utilisateur et les hypothèses.
- Ne présente jamais une hypothèse comme un comportement confirmé.
- Vérifie les effets adjacents pertinents sans élargir la modification à un nettoyage général.
- Préfère les contrats centralisés et les abstractions déjà présentes aux cas spéciaux ajoutés localement.
- Lorsqu'une règle commune doit changer, modifie `AGENTS.md` avec l'autorisation requise au lieu d'ajouter une règle concurrente ici.
