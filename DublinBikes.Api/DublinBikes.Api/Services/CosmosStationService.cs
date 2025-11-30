using System;
using System.Collections.Generic;
using System.Linq;
using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

namespace DublinBikes.Api.Services
{
    /// <summary>
    /// IStationService implementation backed by Azure Cosmos DB.
    /// </summary>
    public class CosmosStationService : IStationService
    {
        private readonly CosmosClient _client;
        private readonly IMemoryCache _cache;
        private readonly CosmosOptions _options;

        private Container Container =>
            _client.GetContainer(_options.DatabaseId, _options.ContainerId);

        public CosmosStationService(
            CosmosClient client,
            IOptions<CosmosOptions> options,
            IMemoryCache cache)
        {
            _client = client;
            _cache = cache;
            _options = options.Value;
        }

        // ===== IStationService basic methods (used by background job) =====

        public IReadOnlyList<Station> GetAll()
        {
            return Container
                .GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true)
                .ToList();
        }

        public Station? GetByNumber(int number)
        {
            var q = Container
                .GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true)
                .Where(s => s.Number == number);

            return q.AsEnumerable().FirstOrDefault();
        }

        public void RandomUpdateAllStations()
        {
            // No random updates are performed for the Cosmos version.
            // Method is kept only to satisfy the interface.
        }

        // ===== V2: List stations with filtering, sorting and paging =====

        public PagedResult<StationDto> GetStations(StationQueryParameters query)
        {
            var cacheKey =
                $"cosmos-stations-{query.Status}-{query.MinBikes}-{query.Q}-{query.Sort}-{query.Dir}-{query.Page}-{query.PageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResult<StationDto> cached))
            {
                return cached;
            }

            IQueryable<Station> q =
                Container.GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true);

            // Filters
            if (!string.IsNullOrWhiteSpace(query.Status))
            {
                q = q.Where(s => s.Status == query.Status);
            }

            if (query.MinBikes.HasValue)
            {
                q = q.Where(s => s.Available_Bikes >= query.MinBikes.Value);
            }

            if (!string.IsNullOrWhiteSpace(query.Q))
            {
                var text = query.Q.ToLower();
                q = q.Where(s =>
                    s.Name.ToLower().Contains(text) ||
                    s.Address.ToLower().Contains(text));
            }

            // Sorting
            var dirDesc = string.Equals(query.Dir, "desc", StringComparison.OrdinalIgnoreCase);

            q = query.Sort?.ToLower() switch
            {
                "name" => dirDesc ? q.OrderByDescending(s => s.Name) : q.OrderBy(s => s.Name),
                "availablebikes" or "available_bikes"
                    => dirDesc ? q.OrderByDescending(s => s.Available_Bikes) : q.OrderBy(s => s.Available_Bikes),
                "occupancy" => dirDesc
                    ? q.OrderByDescending(s =>
                        s.Bike_Stands == 0 ? 0 : (double)s.Available_Bikes / s.Bike_Stands)
                    : q.OrderBy(s =>
                        s.Bike_Stands == 0 ? 0 : (double)s.Available_Bikes / s.Bike_Stands),
                _ => dirDesc ? q.OrderByDescending(s => s.Number) : q.OrderBy(s => s.Number)
            };

            // Paging (convert nullable ints to concrete values)
            int page = query.Page.GetValueOrDefault(1);
            if (page <= 0) page = 1;

            int pageSize = query.PageSize.GetValueOrDefault(20);
            if (pageSize <= 0) pageSize = 20;

            var totalCount = q.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = q
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToList()
                .Select(MapToDto)
                .ToList();

            var result = new PagedResult<StationDto>
            {
                Page = page,
                PageSize = pageSize,
                TotalCount = totalCount,
                TotalPages = totalPages,
                Items = items
            };

            _cache.Set(cacheKey, result, TimeSpan.FromMinutes(5));

            return result;
        }

        // ===== V2: Get single station by number =====

        public StationDto? GetStationDtoByNumber(int number)
        {
            var key = $"cosmos-station-{number}";

            if (_cache.TryGetValue(key, out StationDto cached))
                return cached;

            var q = Container
                .GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true)
                .Where(s => s.Number == number);

            var station = q.AsEnumerable().FirstOrDefault();

            if (station == null)
                return null;

            var dto = MapToDto(station);
            _cache.Set(key, dto, TimeSpan.FromMinutes(5));
            return dto;
        }

        // ===== V2: Create =====

        public StationDto? CreateStation(StationUpsertDto request)
        {
            // Check if a station with the same number already exists
            var existing = GetStationDtoByNumber(request.Number);
            if (existing != null)
                return null;

            var station = new Station
            {
                Number = request.Number,
                Name = request.Name,
                Address = request.Address,
                Position = new GeoPosition
                {
                    Lat = request.Lat,
                    Lng = request.Lng
                },
                Bike_Stands = request.BikeStands,
                Available_Bikes = request.AvailableBikes,
                Available_Bike_Stands = request.BikeStands - request.AvailableBikes,
                Status = request.Status,
                Last_Update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Container.CreateItemAsync(station)
                     .GetAwaiter()
                     .GetResult();

            _cache.Remove($"cosmos-station-{request.Number}");

            return MapToDto(station);
        }

        // ===== V2: Update =====

        public StationDto? UpdateStation(int number, StationUpsertDto request)
        {
            IQueryable<Station> q =
                Container.GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true)
                         .Where(s => s.Number == number);

            var existing = q.AsEnumerable().FirstOrDefault();

            if (existing == null)
                return null;

            existing.Name = request.Name;
            existing.Address = request.Address;
            existing.Position = new GeoPosition
            {
                Lat = request.Lat,
                Lng = request.Lng
            };
            existing.Bike_Stands = request.BikeStands;
            existing.Available_Bikes = request.AvailableBikes;
            existing.Available_Bike_Stands = request.BikeStands - request.AvailableBikes;
            existing.Status = request.Status;
            existing.Last_Update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            Container.UpsertItemAsync(existing)
                     .GetAwaiter()
                     .GetResult();

            _cache.Remove($"cosmos-station-{number}");

            return MapToDto(existing);
        }

        // ===== V2: Summary =====

        public StationsSummaryDto GetSummary()
        {
            const string cacheKey = "cosmos-summary";

            if (_cache.TryGetValue(cacheKey, out StationsSummaryDto cached))
                return cached;

            var stations = Container
                .GetItemLinqQueryable<Station>(allowSynchronousQueryExecution: true)
                .ToList();

            var total = stations.Count;
            var open = stations.Count(s => s.Status == "OPEN");
            var closed = stations.Count(s => s.Status != "OPEN");
            var totalBikes = stations.Sum(s => s.Available_Bikes);
            var totalStands = stations.Sum(s => s.Bike_Stands);

            var summary = new StationsSummaryDto
            {
                TotalStations = total,
                OpenStations = open,
                ClosedStations = closed,
                TotalAvailableBikes = totalBikes,
                TotalBikeStands = totalStands
            };

            _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(5));
            return summary;
        }

        // ===== Mapping from domain model to DTO =====

        private static StationDto MapToDto(Station s)
        {
            var utc = DateTimeOffset.FromUnixTimeMilliseconds(s.Last_Update);

            DateTimeOffset localOffset;
            try
            {
                var tz = TimeZoneInfo.FindSystemTimeZoneById("Europe/Dublin");
                var localTime = TimeZoneInfo.ConvertTimeFromUtc(utc.UtcDateTime, tz);
                localOffset = new DateTimeOffset(localTime);
            }
            catch
            {
                // Fallback to UTC if the time zone is not available
                localOffset = utc;
            }

            var occupancy =
                s.Bike_Stands == 0
                    ? 0
                    : (double)s.Available_Bikes / s.Bike_Stands;

            return new StationDto
            {
                Number = s.Number,
                Name = s.Name,
                Address = s.Address,
                Lat = s.Position?.Lat ?? 0,
                Lng = s.Position?.Lng ?? 0,
                BikeStands = s.Bike_Stands,
                AvailableBikes = s.Available_Bikes,
                Status = s.Status,
                Occupancy = occupancy,
                LastUpdateLocal = localOffset
            };
        }
    }
}
