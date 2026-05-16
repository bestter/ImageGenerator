using System;
using FluentAssertions;
using GrokImagineApp;
using Xunit;

namespace GrokImagineApp.Tests
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
