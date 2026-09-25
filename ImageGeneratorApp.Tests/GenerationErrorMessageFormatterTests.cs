namespace ImageGeneratorApp.Tests
{
    public class GenerationErrorMessageFormatterTests
    {
        [Theory]
        [InlineData("Une erreur de connexion réseau est survenue. Impossible de joindre l'API.", 0)]
        [InlineData("Une erreur de connexion réseau est survenue. Impossible de joindre l'API.", 503)]
        [InlineData("L'image générée dépasse la taille maximale autorisée.", 0)]
        [InlineData("L'image générée dépasse la taille maximale autorisée.", 200)]
        [InlineData("La réponse de l'API ne contient pas d'image valide.", 0)]
        [InlineData("La réponse de l'API est malformée.", 0)]
        public void GetDisplayMessage_UsesKnownApplicationMessage(string message, int statusCode)
        {
            ImageGeneratorException exception = new(message, statusCode);

            GenerationErrorMessageFormatter.GetDisplayMessage(exception).Should().Be(message);
        }

        [Theory]
        [InlineData(400, "La demande a été refusée. Vérifiez les paramètres de génération.")]
        [InlineData(401, "Authentification refusée. Vérifiez votre clé API.")]
        [InlineData(403, "Accès refusé par le fournisseur. Vérifiez les autorisations de votre compte.")]
        [InlineData(429, "La limite de requêtes est atteinte. Réessayez plus tard.")]
        [InlineData(500, "Le service du fournisseur est temporairement indisponible. Réessayez plus tard.")]
        [InlineData(599, "Le service du fournisseur est temporairement indisponible. Réessayez plus tard.")]
        public void GetDisplayMessage_MapsHttpStatusWithoutExposingProviderMessage(int statusCode, string expectedMessage)
        {
            ImageGeneratorException exception = new("Provider details: secret-key-123", statusCode);

            GenerationErrorMessageFormatter.GetDisplayMessage(exception).Should().Be(expectedMessage);
        }

        [Theory]
        [InlineData(0)]
        [InlineData(200)]
        [InlineData(404)]
        [InlineData(600)]
        public void GetDisplayMessage_UsesGenericFallbackForUnknownErrors(int statusCode)
        {
            ImageGeneratorException exception = new("Provider details: secret-key-123", statusCode);

            GenerationErrorMessageFormatter.GetDisplayMessage(exception)
                .Should().Be("Une erreur est survenue lors de la communication avec l'API.");
        }

        [Theory]
        [InlineData(0, false)]
        [InlineData(200, false)]
        [InlineData(299, false)]
        [InlineData(300, true)]
        [InlineData(401, true)]
        [InlineData(599, true)]
        [InlineData(600, false)]
        public void HasHttpErrorStatus_RecognizesOnlyHttpFailures(int statusCode, bool expected)
        {
            GenerationErrorMessageFormatter.HasHttpErrorStatus(statusCode).Should().Be(expected);
        }
    }
}