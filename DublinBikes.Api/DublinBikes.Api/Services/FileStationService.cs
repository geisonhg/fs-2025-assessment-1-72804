using DublinBikes.Api.Models;
using System.Text.Json;
using DublinBikes.Api.Dtos;
using Microsoft.Extensions.Caching.Memory;
using System;

namespace DublinBikes.Api.Services
{
    public class FileStationService : IStationService
    {
        private readonly List<Station> _stations;
        private readonly Random _random = new();
        private readonly IMemoryCache _cache;


        public FileStationService(IEnumerable<Station> stations, IMemoryCache cache)
        {
            _stations = stations.ToList();
            _cache = cache;
        }

        public FileStationService(IWebHostEnvironment env, IMemoryCache cache)
        {

            _cache = cache;
            var dataPath = Path.Combine(env.ContentRootPath, "Data", "dublinbike.json");

            if (!File.Exists(dataPath))
            {
                throw new FileNotFoundException("dublinbike.json not found", dataPath);
            }

            var json = File.ReadAllText(dataPath);

            var options = new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true
            };

            var stations = JsonSerializer.Deserialize<List<Station>>(json, options);

            if (stations is null || stations.Count == 0)
            {
                throw new Exception("No stations loaded from JSON.");
            }

            _stations = stations;
        }

        public IReadOnlyList<Station> GetAll() => _stations;

        public Station? GetByNumber(int number) =>
            _stations.FirstOrDefault(s => s.Number == number);

        public PagedResult<StationDto> GetStations(StationQueryParameters parameters)
        {
            var query = _stations.AsQueryable();

            // Filtros
            if (!string.IsNullOrWhiteSpace(parameters.Status))
            {
                var status = parameters.Status.Trim().ToUpperInvariant();
                if (status is "OPEN" or "CLOSED")
                {
                    query = query.Where(s => s.Status.Equals(status, StringComparison.OrdinalIgnoreCase));
                }
            }

            if (parameters.MinBikes.HasValue && parameters.MinBikes.Value >= 0)
            {
                query = query.Where(s => s.Available_Bikes >= parameters.MinBikes.Value);
            }

            if (!string.IsNullOrWhiteSpace(parameters.Q))
            {
                var term = parameters.Q.Trim().ToLowerInvariant();
                query = query.Where(s =>
                    s.Name.ToLower().Contains(term) ||
                    s.Address.ToLower().Contains(term));
            }

            // Orden
            var sort = string.IsNullOrWhiteSpace(parameters.Sort)
                ? "name"
                : parameters.Sort.Trim().ToLowerInvariant();

            var dir = string.IsNullOrWhiteSpace(parameters.Dir)
                ? "asc"
                : parameters.Dir.Trim().ToLowerInvariant();

            var desc = dir == "desc";

            query = sort switch
            {
                "availablebikes" => desc
                    ? query.OrderByDescending(s => s.Available_Bikes)
                    : query.OrderBy(s => s.Available_Bikes),

                "occupancy" => desc
                    ? query.OrderByDescending(s => s.Occupancy)
                    : query.OrderBy(s => s.Occupancy),

                "name" or _ => desc
                    ? query.OrderByDescending(s => s.Name)
                    : query.OrderBy(s => s.Name),
            };

            // Paginación
            var page = !parameters.Page.HasValue || parameters.Page.Value <= 0
                ? 1
                : parameters.Page.Value;

            var pageSize = !parameters.PageSize.HasValue || parameters.PageSize.Value <= 0 || parameters.PageSize.Value > 100
                ? 20
                : parameters.PageSize.Value;

            // 🔹 AQUÍ va el caché, ANTES de ejecutar Count+Skip+Take
            var cacheKey = $"stations:{parameters.Status}:{parameters.MinBikes}:{parameters.Q}:{sort}:{dir}:{page}:{pageSize}";

            if (_cache.TryGetValue(cacheKey, out PagedResult<StationDto>? cachedResult))
            {
                return cachedResult!;
            }

            // Solo si no hay caché hacemos el cálculo
            var totalCount = query.Count();
            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            var items = query
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
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


        public StationDto? GetStationDtoByNumber(int number)
        {
            var cacheKey = $"station:{number}";

            if (_cache.TryGetValue(cacheKey, out StationDto? cached))
            {
                return cached;
            }

            var station = GetByNumber(number);
            if (station is null) return null;

            var dto = MapToDto(station);

            _cache.Set(cacheKey, dto, TimeSpan.FromMinutes(5));

            return dto;
        }


        // --- private helper  ---

        private static readonly TimeZoneInfo DublinTimeZone =
            TimeZoneInfo.FindSystemTimeZoneById("GMT Standard Time");

        private static StationDto MapToDto(Station s)
        {
            var lastUpdateUtc = s.LastUpdateUtc;
            var lastUpdateLocal = TimeZoneInfo.ConvertTime(lastUpdateUtc, DublinTimeZone);

            return new StationDto
            {
                Number = s.Number,
                Name = s.Name,
                Address = s.Address,
                Lat = s.Position.Lat,
                Lng = s.Position.Lng,
                BikeStands = s.Bike_Stands,
                AvailableBikeStands = s.Available_Bike_Stands,
                AvailableBikes = s.Available_Bikes,
                Status = s.Status,
                Occupancy = s.Occupancy,
                LastUpdateUtc = lastUpdateUtc,
                LastUpdateLocal = lastUpdateLocal
            };
        }
        public StationsSummaryDto GetSummary()
        {
            const string cacheKey = "stations:summary";

            if (_cache.TryGetValue(cacheKey, out StationsSummaryDto? cached))
            {
                return cached!;
            }

            var totalStations = _stations.Count;
            var totalBikeStands = _stations.Sum(s => s.Bike_Stands);
            var totalAvailableBikes = _stations.Sum(s => s.Available_Bikes);

            var openStations = _stations.Count(s => s.Status.Equals("OPEN", StringComparison.OrdinalIgnoreCase));
            var closedStations = _stations.Count(s => s.Status.Equals("CLOSED", StringComparison.OrdinalIgnoreCase));

            var summary = new StationsSummaryDto
            {
                TotalStations = totalStations,
                TotalBikeStands = totalBikeStands,
                TotalAvailableBikes = totalAvailableBikes,
                OpenStations = openStations,
                ClosedStations = closedStations
            };

            _cache.Set(cacheKey, summary, TimeSpan.FromMinutes(5));

            return summary;
        }


        public StationDto? CreateStation(StationUpsertDto request)
        {
            // prevent copy by number
            if (_stations.Any(s => s.Number == request.Number))
            {
                return null; // it already exists
            }

            var status = NormaliseStatus(request.Status);

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
                Available_Bike_Stands = request.AvailableBikeStands,
                Available_Bikes = request.AvailableBikes,
                Status = status,
                Last_Update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            _stations.Add(station);
            _cache.Remove("stations:summary");
            _cache.Remove($"station:{station.Number}");


            return MapToDto(station);
        }

        public StationDto? UpdateStation(int number, StationUpsertDto request)
        {
            var station = _stations.FirstOrDefault(s => s.Number == number);
            if (station is null)
            {
                return null; // it does not exist
            }

            // we can ignore number from request, as it's in the URL
            station.Name = request.Name;
            station.Address = request.Address;
            station.Position.Lat = request.Lat;
            station.Position.Lng = request.Lng;
            station.Bike_Stands = request.BikeStands;
            station.Available_Bike_Stands = request.AvailableBikeStands;
            station.Available_Bikes = request.AvailableBikes;
            station.Status = NormaliseStatus(request.Status);
            station.Last_Update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            _cache.Remove("stations:summary");
            _cache.Remove($"station:{station.Number}");


            return MapToDto(station);
        }

        // Helper para status
        private static string NormaliseStatus(string? status)
        {
            if (string.IsNullOrWhiteSpace(status))
                return "OPEN";

            var s = status.Trim().ToUpperInvariant();
            return (s == "OPEN" || s == "CLOSED") ? s : "OPEN";
        }

        public void RandomUpdateAllStations()
        {
            foreach (var s in _stations)
            {
                // Capacidad entre 10 y 40 (puedes cambiar rangos)
                var capacity = _random.Next(10, 41);

                var availableBikes = _random.Next(0, capacity + 1);
                var availableStands = capacity - availableBikes;

                s.Bike_Stands = capacity;
                s.Available_Bikes = availableBikes;
                s.Available_Bike_Stands = availableStands;
                s.Last_Update = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            }
        }



    }
}
