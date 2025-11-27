using System.Linq;
using DublinBikes.Api.Dtos;
using DublinBikes.Api.Services;
using Microsoft.Extensions.Caching.Memory;
using Xunit;

namespace DublinBikes.Tests
{
    public class StationServiceTests
    {
        // Helper para crear el servicio con datos de prueba
        private FileStationService CreateService()
        {
            var stations = StationTestData.CreateSampleStations();
            var memoryCache = new MemoryCache(new MemoryCacheOptions());
            return new FileStationService(stations, memoryCache);
        }

        [Fact]
        public void GetStations_Filters_By_Status_And_MinBikes()
        {
            // Arrange
            var service = CreateService();
            var query = new StationQueryParameters
            {
                Status = "OPEN",
                MinBikes = 6
            };

            // Act
            var result = service.GetStations(query);

            // Assert
            Assert.Equal(1, result.TotalCount);          // just Gamma
            Assert.Single(result.Items);
            Assert.Equal("Gamma Avenue", result.Items.First().Name);
        }

        [Fact]
        public void GetStations_Searches_By_Name_Or_Address()
        {
            // Arrange
            var service = CreateService();
            var query = new StationQueryParameters
            {
                Q = "Street"
            };

            // Act
            var result = service.GetStations(query);

            // Assert
            Assert.Equal(1, result.TotalCount);
            Assert.Single(result.Items);
            Assert.Equal("Alpha Street", result.Items.First().Name);
        }

        [Fact]
        public void GetStations_Sorts_By_AvailableBikes_And_Pages()
        {
            // Arrange
            var service = CreateService();
            var query = new StationQueryParameters
            {
                Sort = "availableBikes",
                Dir = "desc",
                Page = 1,
                PageSize = 2
            };

            // Act
            var result = service.GetStations(query);

            // Assert
            Assert.Equal(3, result.TotalCount);     
            Assert.Equal(2, result.Items.Count);     

            Assert.Equal("Gamma Avenue", result.Items[0].Name); // 10 bikes
            Assert.Equal("Alpha Street", result.Items[1].Name); // 5 bikes
        }
    }
}
