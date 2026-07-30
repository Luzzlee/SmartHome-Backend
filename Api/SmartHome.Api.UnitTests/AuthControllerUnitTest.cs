using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmartHome.Api.Models;
using SmartHome.Slices.Auth.Services;

namespace SmartHome.Api.UnitTests {

    [TestFixture]
    public class AuthControllerUnitTest {
        private Mock<IAuthService> _authServiceMock = null!;
        private Mock<IAuthenticationService> _authenticationServiceMock = null!;
        private AuthController _controller = null!;

        [SetUp]
        public void Setup() {
            _authServiceMock = new Mock<IAuthService>();
            _authenticationServiceMock = new Mock<IAuthenticationService>();

            var services = new ServiceCollection();
            services.AddSingleton(_authenticationServiceMock.Object);
            var serviceProvider = services.BuildServiceProvider();

            _controller = new AuthController(_authServiceMock.Object) {
                ControllerContext = new ControllerContext {
                    HttpContext = new DefaultHttpContext { RequestServices = serviceProvider }
                }
            };
        }

        [Test]
        public async Task Login_WithValidCredentials_ShouldReturnOk() {
            _authServiceMock.Setup(s => s.ValidateCredentials("admin", "correct-password")).Returns(true);
            _authServiceMock.Setup(s => s.ConfiguredUsername).Returns("admin");

            var result = await _controller.Login(new LoginRequest { Username = "admin", Password = "correct-password" });

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task Login_WithInvalidCredentials_ShouldReturnUnauthorized() {
            _authServiceMock.Setup(s => s.ValidateCredentials("admin", "wrong-password")).Returns(false);

            var result = await _controller.Login(new LoginRequest { Username = "admin", Password = "wrong-password" });

            Assert.That(result, Is.TypeOf<UnauthorizedResult>());
        }

        [Test]
        public void Me_WhenUserIsAuthenticated_ShouldReturnOk() {
            var identity = new ClaimsIdentity(new[] { new Claim(ClaimTypes.Name, "admin") }, "TestAuth");
            _controller.ControllerContext.HttpContext.User = new ClaimsPrincipal(identity);

            var result = _controller.Me();

            Assert.That(result, Is.TypeOf<OkObjectResult>());
        }
    }
}
