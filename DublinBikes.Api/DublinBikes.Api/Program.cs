using DublinBikes.Api.Dtos;
using DublinBikes.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IStationService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var cache = sp.GetRequiredService<IMemoryCache>();

    return new FileStationService(config, cache); 
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<StationUpdateBackgroundService>();


var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ENDPOINTS
app.MapGet("/api/v1/stations",
    ([AsParameters] StationQueryParameters query,
     IStationService stationService) =>
    {
        var result = stationService.GetStations(query);
        return Results.Ok(result);
    });

app.MapGet("/api/v1/stations/{number:int}",
    (int number, IStationService stationService) =>
    {
        var station = stationService.GetStationDtoByNumber(number);
        return station is null ? Results.NotFound() : Results.Ok(station);
    });

app.MapGet("/api/v1/stations/summary",
    (IStationService stationService) =>
    {
        var summary = stationService.GetSummary();
        return Results.Ok(summary);
    });

app.MapPost("/api/v1/stations",
    (StationUpsertDto request, IStationService stationService) =>
    {
        var created = stationService.CreateStation(request);

        if (created is null)
        {
            return Results.Conflict(new { message = "A station with this number already exists." });
        }

        var url = $"/api/v1/stations/{created.Number}";
        return Results.Created(url, created);
    });

app.MapPut("/api/v1/stations/{number:int}",
    (int number, StationUpsertDto request, IStationService stationService) =>
    {
        var updated = stationService.UpdateStation(number, request);

        if (updated is null)
        {
            return Results.NotFound(new { message = "Station not found." });
        }

        return Results.Ok(updated);
    });

var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
app.UseHttpsRedirection();
}

// swagger, etc...
if (app.Environment.IsDevelopment() || isTesting)
{
app.UseSwagger();
app.UseSwaggerUI();
}

// =================== endpoints v1 / v2 ===================

app.Run();

// to WebApplicationFactory<Program>
public partial class Program { }

