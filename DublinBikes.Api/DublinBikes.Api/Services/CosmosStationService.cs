using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;


namespace DublinBikes.Api.Services;

public class CosmosStationService : IStationService
{
    public PagedResult<StationDto> GetStations(StationQueryParameters parameters)
        => throw new NotImplementedException();

    public StationDto? GetStationDtoByNumber(int number)
        => throw new NotImplementedException();

    public StationDto? CreateStation(StationUpsertDto request)
        => throw new NotImplementedException();

    public StationDto? UpdateStation(int number, StationUpsertDto request)
        => throw new NotImplementedException();

    public StationsSummaryDto GetSummary()
        => throw new NotImplementedException();

    public IReadOnlyList<Station> GetAll()
    {
        throw new NotImplementedException();
    }

    public Station? GetByNumber(int number)
    {
        throw new NotImplementedException();
    }

    public void RandomUpdateAllStations()
    {
        throw new NotImplementedException();
    }
}
