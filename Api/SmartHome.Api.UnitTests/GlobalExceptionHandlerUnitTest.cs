using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using SmartHome.Api.ErrorHandling;

namespace SmartHome.Api.UnitTests {

    [TestFixture]
    public class GlobalExceptionHandlerUnitTest {
        private GlobalExceptionHandler _handler = null!;

        [SetUp]
        public void Setup() {
            _handler = new GlobalExceptionHandler();
        }

        private static async Task<(bool Handled, HttpContext Context, ProblemDetails? Body)> InvokeAsync(Exception exception, GlobalExceptionHandler handler) {
            var context = new DefaultHttpContext();
            context.Response.Body = new MemoryStream();

            var handled = await handler.TryHandleAsync(context, exception, CancellationToken.None);

            context.Response.Body.Seek(0, SeekOrigin.Begin);
            var body = await JsonSerializer.DeserializeAsync<ProblemDetails>(context.Response.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web));

            return (handled, context, body);
        }

        [Test]
        public async Task TryHandleAsync_WhenArgumentException_ReturnsBadRequestProblemDetails() {
            var (handled, context, body) = await InvokeAsync(new ArgumentException("invalid id"), _handler);

            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(body!.Status, Is.EqualTo(StatusCodes.Status400BadRequest));
            Assert.That(body!.Detail, Is.EqualTo("invalid id"));
        }

        [Test]
        public async Task TryHandleAsync_WhenUnexpectedException_ReturnsInternalServerErrorWithoutInternalDetails() {
            var (handled, context, body) = await InvokeAsync(new NullReferenceException("some internal detail"), _handler);

            Assert.That(handled, Is.True);
            Assert.That(context.Response.StatusCode, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(body!.Status, Is.EqualTo(StatusCodes.Status500InternalServerError));
            Assert.That(body!.Detail, Does.Not.Contain("some internal detail"));
        }
    }
}
