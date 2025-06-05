using System;
using Xunit;

namespace TestDubRosh
{
    public class AuthenticationTests
    {
        [Fact]
        public void ValidManagerLogin_ShouldReturnTrue()
        {
            // Arrange
            string username = "Петрова Ольга Николаевна";
            string password = "manager2023";
            var authService = new AuthenticationService();

            // Act
            bool result = authService.AuthenticateManager(username, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void InvalidManagerLogin_ShouldReturnFalse()
        {
            // Arrange
            string username = "НесуществующийЛогин";
            string password = "НеверныйПароль";
            var authService = new AuthenticationService();

            // Act
            bool result = authService.AuthenticateManager(username, password);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public void ValidChiefDoctorLogin_ShouldReturnTrue()
        {
            // Arrange
            string username = "Соколов Михаил Андреевич";
            string password = "Doc$2023";
            var authService = new AuthenticationService();

            // Act
            bool result = authService.AuthenticateChiefDoctor(username, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void ValidDoctorLogin_ShouldReturnTrue()
        {
            // Arrange
            string username = "Иванов Иван Иванович";
            string password = "LFK_2023!";
            var authService = new AuthenticationService();

            // Act
            bool result = authService.AuthenticateDoctor(username, password);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public void EmptyCredentials_ShouldReturnFalse()
        {
            // Arrange
            string username = "";
            string password = "";
            var authService = new AuthenticationService();

            // Act
            bool result = authService.AuthenticateManager(username, password);

            // Assert
            Assert.False(result);
        }
    }
}