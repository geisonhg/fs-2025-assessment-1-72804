using System.Net;
using System.Threading.Tasks;
using DublinBikes.Api;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Hosting;
using Xunit;

namespace DublinBikes.Tests
{
    public class StationsEndpointTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly HttpClient _client;

        public StationsEndpointTests(WebApplicationFactory<Program> factory)
        {
            // Arrancamos la API en environment "Testing"
            _client = factory
                .WithWebHostBuilder(builder =>
                {
                    builder.UseEnvironment("Testing");
                })
                .CreateClient();
        }

        [Fact]
        public async Task GetStations_ReturnsOk_AndJson()
        {
            // Act
            var response = await _client.GetAsync("/api/v1/stations");

            // Assert
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var contentType = response.Content.Headers.ContentType?.MediaType;
            Assert.Equal("application/json", contentType);
        }
    }
}
