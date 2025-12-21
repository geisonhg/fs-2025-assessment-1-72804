using System.Net.Http.Json;

namespace DublinBikes.Blazor.Services
{
    public class StationsApiClient
    {
        private readonly HttpClient _http;

        public StationsApiClient(HttpClient http)
        {
            _http = http;
        }

        // GET /api/v2/stations?...
        public async Task<PagedResult<StationDto>> GetStationsAsync(StationsQuery query, CancellationToken ct = default)
        {
            var qs = new List<string>();

            if (!string.IsNullOrWhiteSpace(query.Q))
                qs.Add($"q={Uri.EscapeDataString(query.Q)}");

            if (!string.IsNullOrWhiteSpace(query.Status) && query.Status != "ALL")
                qs.Add($"status={query.Status}");

            if (query.MinBikes.HasValue)
                qs.Add($"minBikes={query.MinBikes.Value}");

            if (!string.IsNullOrWhiteSpace(query.Sort))
                qs.Add($"sort={query.Sort}");

            if (!string.IsNullOrWhiteSpace(query.Dir))
                qs.Add($"dir={query.Dir}");

            if (query.Page > 0)
                qs.Add($"page={query.Page}");

            if (query.PageSize > 0)
                qs.Add($"pageSize={query.PageSize}");

            var url = "/api/v2/stations";
            if (qs.Count > 0)
                url += "?" + string.Join("&", qs);

            var result = await _http.GetFromJsonAsync<PagedResult<StationDto>>(url, ct);
            return result ?? new PagedResult<StationDto> { Items = new List<StationDto>() };
        }

        // GET /api/v2/stations/{number}
        public async Task<StationDto?> GetStationAsync(int number, CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<StationDto>($"/api/v2/stations/{number}", ct);
        }

        // POST /api/v2/stations
        public async Task<StationDto?> CreateStationAsync(StationUpsertDto dto, CancellationToken ct = default)
        {
            var response = await _http.PostAsJsonAsync("/api/v2/stations", dto, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<StationDto>(cancellationToken: ct);
        }

        // PUT /api/v2/stations/{number}
        public async Task<StationDto?> UpdateStationAsync(int number, StationUpsertDto dto, CancellationToken ct = default)
        {
            var response = await _http.PutAsJsonAsync($"/api/v2/stations/{number}", dto, ct);
            if (!response.IsSuccessStatusCode)
                return null;

            return await response.Content.ReadFromJsonAsync<StationDto>(cancellationToken: ct);
        }

        // DELETE /api/v2/stations/{number}  (cuando añadas el endpoint en la API)
        public async Task DeleteStationAsync(int number, CancellationToken ct = default)
        {
            var response = await _http.DeleteAsync($"/api/v2/stations/{number}", ct);
            response.EnsureSuccessStatusCode();
        }

        // GET /api/v2/stations/summary  (opcional)
        public async Task<StationsSummaryDto?> GetSummaryAsync(CancellationToken ct = default)
        {
            return await _http.GetFromJsonAsync<StationsSummaryDto>("/api/v2/stations/summary", ct);
        }
    }

    public class StationsQuery
    {
        public string? Q { get; set; }
        public string? Status { get; set; } = "ALL";  // ALL, OPEN, CLOSED
        public int? MinBikes { get; set; }

        public string? Sort { get; set; } = "name";   // name, availablebikes, occupancy
        public string? Dir { get; set; } = "asc";     // asc, desc

        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class PagedResult<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalCount { get; set; }
        public int TotalPages { get; set; }
        public List<T> Items { get; set; } = new();
    }

    // These DTOs mirror the shapes returned by your API v2
    public class StationDto
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int BikeStands { get; set; }
        public int AvailableBikes { get; set; }
        public string Status { get; set; } = string.Empty;
        public double Occupancy { get; set; }        // 0..1
        public DateTimeOffset LastUpdateLocal { get; set; }
    }

    public class StationUpsertDto
    {
        public int Number { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public double Lat { get; set; }
        public double Lng { get; set; }
        public int BikeStands { get; set; }
        public int AvailableBikes { get; set; }
        public string Status { get; set; } = "OPEN";
    }

    public class StationsSummaryDto
    {
        public int TotalStations { get; set; }
        public int OpenStations { get; set; }
        public int ClosedStations { get; set; }
        public int TotalAvailableBikes { get; set; }
        public int TotalBikeStands { get; set; }
    }
}
