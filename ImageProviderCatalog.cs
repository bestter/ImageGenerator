// AI Image generator. A program to generate image from AI API.
// Copyright (C) 2026  Martin Labelle
//
// This program is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
//
// This program is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
//
// You should have received a copy of the GNU General Public License
// along with this program.  If not, see <https://www.gnu.org/licenses/>.

using System;

namespace ImageGeneratorApp
{
    public static class ImageProviderCatalog
    {
        public const string GrokImagineImage = "grok-imagine-image";
        public const string GrokImagineImageQuality = "grok-imagine-image-quality";
        public const string NanoBananaPro = "nano-banana-pro";
        public const string GptImage2 = "gpt-image-2";

        public const string StorageProviderXai = "xAI";
        public const string StorageProviderGoogle = "Google";
        public const string StorageProviderOpenAI = "OpenAI";

        public static string GetStorageProviderName(string model)
        {
            if (model == NanoBananaPro)
                return StorageProviderGoogle;
            if (model == GptImage2)
                return StorageProviderOpenAI;
            return StorageProviderXai;
        }

        public static bool SupportsImageEditing(string model)
        {
            return model is GrokImagineImage or GrokImagineImageQuality;
        }

        public static string GetFriendlyGeneratorName(string modelId)
        {
            if (string.IsNullOrWhiteSpace(modelId))
                return "Unknown";

            return modelId.Trim() switch
            {
                GrokImagineImage => "Grok Imagine",
                GrokImagineImageQuality => "Grok Imagine Quality",
                NanoBananaPro => "Nano Banana Pro",
                GptImage2 => "OpenAI GPT Image",
                var trimmed => trimmed
            };
        }

        public static void EnsureReferenceImagesAllowed(string model, int imageCount)
        {
            if (imageCount <= 0)
                return;

            string? message = model switch
            {
                NanoBananaPro => "Le modèle Nano Banana Pro ne supporte pas l'édition d'image.",
                GptImage2 => "Le modèle GPT Image 2 ne supporte pas l'édition d'image dans cette application.",
                _ => null
            };

            if (message != null)
                throw new ArgumentException(message, nameof(imageCount));
        }
    }
}
