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

using FluentAssertions;
using ImageGeneratorApp;
using System;
using Xunit;

namespace ImageGeneratorApp.Tests
{
    public class ImageProviderCatalogTests
    {
        [Theory]
        [InlineData(ImageProviderCatalog.NanoBananaPro, ImageProviderCatalog.StorageProviderGoogle)]
        [InlineData(ImageProviderCatalog.GptImage2, ImageProviderCatalog.StorageProviderOpenAI)]
        [InlineData(ImageProviderCatalog.GrokImagineImage, ImageProviderCatalog.StorageProviderXai)]
        [InlineData(ImageProviderCatalog.GrokImagineImageQuality, ImageProviderCatalog.StorageProviderXai)]
        [InlineData("unknown-model", ImageProviderCatalog.StorageProviderXai)]
        public void GetStorageProviderName_MapsKnownModelsAndDefaultsUnknownToXai(
            string model,
            string expected)
        {
            ImageProviderCatalog.GetStorageProviderName(model).Should().Be(expected);
        }

        [Theory]
        [InlineData(ImageProviderCatalog.GrokImagineImage, true)]
        [InlineData(ImageProviderCatalog.GrokImagineImageQuality, true)]
        [InlineData(ImageProviderCatalog.NanoBananaPro, false)]
        [InlineData(ImageProviderCatalog.GptImage2, false)]
        [InlineData("unknown-model", false)]
        public void SupportsImageEditing_OnlyGrokModels(string model, bool expected)
        {
            ImageProviderCatalog.SupportsImageEditing(model).Should().Be(expected);
        }

        [Theory]
        [InlineData(ImageProviderCatalog.GrokImagineImage, "Grok Imagine")]
        [InlineData(ImageProviderCatalog.GrokImagineImageQuality, "Grok Imagine Quality")]
        [InlineData(ImageProviderCatalog.NanoBananaPro, "Nano Banana Pro")]
        [InlineData(ImageProviderCatalog.GptImage2, "OpenAI GPT Image")]
        [InlineData("future-dall-e-4", "future-dall-e-4")]
        [InlineData("", "Unknown")]
        public void GetFriendlyGeneratorName_MapsKnownModelsAndFallsBack(string model, string expected)
        {
            ImageProviderCatalog.GetFriendlyGeneratorName(model).Should().Be(expected);
        }

        [Fact]
        public void EnsureReferenceImagesAllowed_DoesNothingWhenCountIsZero()
        {
            ImageProviderCatalog.EnsureReferenceImagesAllowed(ImageProviderCatalog.GptImage2, 0);
            ImageProviderCatalog.EnsureReferenceImagesAllowed(ImageProviderCatalog.NanoBananaPro, 0);
            ImageProviderCatalog.EnsureReferenceImagesAllowed(ImageProviderCatalog.GrokImagineImage, 2);
        }

        [Fact]
        public void EnsureReferenceImagesAllowed_ThrowsForNanoBanana()
        {
            Action act = () => ImageProviderCatalog.EnsureReferenceImagesAllowed(
                ImageProviderCatalog.NanoBananaPro, 1);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le modèle Nano Banana Pro ne supporte pas l'édition d'image.*");
        }

        [Fact]
        public void EnsureReferenceImagesAllowed_ThrowsForGptImage2()
        {
            Action act = () => ImageProviderCatalog.EnsureReferenceImagesAllowed(
                ImageProviderCatalog.GptImage2, 1);
            act.Should().Throw<ArgumentException>()
                .WithMessage("*Le modèle GPT Image 2 ne supporte pas l'édition d'image dans cette application*");
        }

        [Fact]
        public void EnsureReferenceImagesAllowed_DoesNotThrowForUnknownModel()
        {
            ImageProviderCatalog.EnsureReferenceImagesAllowed("unknown-model", 1);
        }
    }
}
