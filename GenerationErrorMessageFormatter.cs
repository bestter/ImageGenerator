namespace ImageGeneratorApp
{
    internal static class GenerationErrorMessageFormatter
    {
        internal static string GetDisplayMessage(ImageGeneratorException exception)
        {
            return exception.Message switch
            {
                "Une erreur de connexion réseau est survenue. Impossible de joindre l'API." =>
                    "Une erreur de connexion réseau est survenue. Impossible de joindre l'API.",
                "L'image générée dépasse la taille maximale autorisée." =>
                    "L'image générée dépasse la taille maximale autorisée.",
                "La réponse de l'API ne contient pas d'image valide." =>
                    "La réponse de l'API ne contient pas d'image valide.",
                "La réponse de l'API est malformée." =>
                    "La réponse de l'API est malformée.",
                _ => exception.StatusCode switch
                {
                    400 => "La demande a été refusée. Vérifiez les paramètres de génération.",
                    401 => "Authentification refusée. Vérifiez votre clé API.",
                    403 => "Accès refusé par le fournisseur. Vérifiez les autorisations de votre compte.",
                    429 => "La limite de requêtes est atteinte. Réessayez plus tard.",
                    >= 500 and <= 599 => "Le service du fournisseur est temporairement indisponible. Réessayez plus tard.",
                    _ => "Une erreur est survenue lors de la communication avec l'API."
                }
            };
        }

        internal static bool HasHttpErrorStatus(int statusCode)
        {
            return statusCode is >= 300 and <= 599;
        }
    }
}