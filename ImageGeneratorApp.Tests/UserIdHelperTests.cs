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
    public class UserIdHelperTests
    {
        [Fact]
        public void GetOpaqueUserId_WithSpecificName_ReturnsDeterministicHash()
        {
            // Arrange
            string name = "test_user";

            // Act
            string hash1 = UserIdHelper.GetOpaqueUserId(name);
            string hash2 = UserIdHelper.GetOpaqueUserId(name);

            // Assert
            hash1.Should().NotBeNullOrWhiteSpace();
            hash1.Should().Be(hash2);
            hash1.Should().NotContain(name);
        }

        [Fact]
        public void GetOpaqueUserId_DifferentNames_ReturnDifferentHashes()
        {
            // Arrange
            string name1 = "user_one";
            string name2 = "user_two";

            // Act
            string hash1 = UserIdHelper.GetOpaqueUserId(name1);
            string hash2 = UserIdHelper.GetOpaqueUserId(name2);

            // Assert
            hash1.Should().NotBe(hash2);
        }

        [Fact]
        public void GetOpaqueUserId_NullName_DoesNotThrow()
        {
            // Act
            Action act = () => UserIdHelper.GetOpaqueUserId(null);

            // Assert
            act.Should().NotThrow();
        }
    }
}