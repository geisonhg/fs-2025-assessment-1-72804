using System.Net.Http.Json;
using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;

namespace DublinBikes.Blazor.Services
{
    public class StationsApiClient
    {
        private readonly HttpClient _http;

        public StationsApiClient(HttpClient http)
        {
            _http = http;
        }

        // GET list with filters/paging
        public async Task<PagedResult<StationDto>?> GetStationsAsync(StationQueryParameters query)
        {
            var url = $"/api/v2/stations" +
                      $"?status={query.Status}" +
                      $"&minBikes={query.MinBikes}" +
                      $"&q={query.Q}" +
                      $"&sort={query.Sort}" +
                      $"&dir={query.Dir}" +
                      $"&page={query.Page}" +
                      $"&pageSize={query.PageSize}";

            return await _http.GetFromJsonAsync<PagedResult<StationDto>>(url);
        }

        // GET detail
        public async Task<StationDto?> GetStationAsync(int number)
        {
            return await _http.GetFromJsonAsync<StationDto>($"/api/v2/stations/{number}");
        }

        // GET summary
        public async Task<StationsSummaryDto?> GetSummaryAsync()
        {
            return await _http.GetFromJsonAsync<StationsSummaryDto>("/api/v2/stations/summary");
        }

        // POST create
        public async Task<StationDto?> CreateStationAsync(StationUpsertDto dto)
        {
            var response = await _http.PostAsJsonAsync("/api/v2/stations", dto);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<StationDto>();
        }

        // PUT update
        public async Task<StationDto?> UpdateStationAsync(int number, StationUpsertDto dto)
        {
            var response = await _http.PutAsJsonAsync($"/api/v2/stations/{number}", dto);
            if (!response.IsSuccessStatusCode) return null;

            return await response.Content.ReadFromJsonAsync<StationDto>();
        }

        // DELETE
        public async Task<bool> DeleteStationAsync(int number)
        {
            var response = await _http.DeleteAsync($"/api/v2/stations/{number}");
            return response.IsSuccessStatusCode;
        }
    }
}
