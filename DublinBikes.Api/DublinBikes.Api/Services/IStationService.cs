using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;


namespace DublinBikes.Api.Services
{
    public interface IStationService
    {
        IReadOnlyList<Station> GetAll();         
        Station? GetByNumber(int number);

        PagedResult<StationDto> GetStations(StationQueryParameters parameters);
        StationDto? GetStationDtoByNumber(int number);
        StationsSummaryDto GetSummary();
        StationDto? CreateStation(StationUpsertDto request);
        StationDto? UpdateStation(int number, StationUpsertDto request);
        void RandomUpdateAllStations();

    }
}
