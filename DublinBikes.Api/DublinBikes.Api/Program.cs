using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;
using DublinBikes.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;
using Microsoft.Azure.Cosmos;

var builder = WebApplication.CreateBuilder(args);

// ====== Core services ======
builder.Services.AddMemoryCache();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<StationUpdateBackgroundService>();

// ====== V1: file-based service ======
builder.Services.AddSingleton<IStationService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var cache = sp.GetRequiredService<IMemoryCache>();
    return new FileStationService(config, cache);
});

// ====== V2: Cosmos ======
builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection("Cosmos"));

builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var options = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
    return new CosmosClient(options.Endpoint, options.Key);
});

builder.Services.AddSingleton<CosmosStationService>();

var app = builder.Build();

// ====== Middleware ======
var isTesting = app.Environment.IsEnvironment("Testing");

if (!isTesting)
{
    app.UseHttpsRedirection();
}

if (app.Environment.IsDevelopment() || isTesting)
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// ====== V1 endpoints (file data) ======
var v1 = app.MapGroup("/api/v1");

v1.MapGet("/stations",
    ([AsParameters] StationQueryParameters query,
     IStationService stationService) =>
    {
        var result = stationService.GetStations(query);
        return Results.Ok(result);
    });

v1.MapGet("/stations/{number:int}",
    (int number, IStationService stationService) =>
    {
        var station = stationService.GetStationDtoByNumber(number);
        return station is null ? Results.NotFound() : Results.Ok(station);
    });

v1.MapGet("/stations/summary",
    (IStationService stationService) =>
    {
        var summary = stationService.GetSummary();
        return Results.Ok(summary);
    });

v1.MapPost("/stations",
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

v1.MapPut("/stations/{number:int}",
    (int number, StationUpsertDto request, IStationService stationService) =>
    {
        var updated = stationService.UpdateStation(number, request);
        if (updated is null)
        {
            return Results.NotFound(new { message = "Station not found." });
        }

        return Results.Ok(updated);
    });

// ====== V2 endpoints (Cosmos) ======
var v2 = app.MapGroup("/api/v2");

v2.MapGet("/stations",
    ([AsParameters] StationQueryParameters query,
     CosmosStationService stationService) =>
    {
        var result = stationService.GetStations(query);
        return Results.Ok(result);
    });

v2.MapGet("/stations/{number:int}",
    (int number, CosmosStationService stationService) =>
    {
        var station = stationService.GetStationDtoByNumber(number);
        return station is null ? Results.NotFound() : Results.Ok(station);
    });

v2.MapGet("/stations/summary",
    (CosmosStationService stationService) =>
    {
        var summary = stationService.GetSummary();
        return Results.Ok(summary);
    });

v2.MapPost("/stations",
    (StationUpsertDto request, CosmosStationService stationService) =>
    {
        var created = stationService.CreateStation(request);
        if (created is null)
        {
            return Results.Conflict(new { message = "A station with this number already exists." });
        }

        var url = $"/api/v2/stations/{created.Number}";
        return Results.Created(url, created);
    });

v2.MapPut("/stations/{number:int}",
    (int number, StationUpsertDto request, CosmosStationService stationService) =>
    {
        var updated = stationService.UpdateStation(number, request);
        if (updated is null)
        {
            return Results.NotFound(new { message = "Station not found." });
        }

        return Results.Ok(updated);
    });

// ====== Admin import to Cosmos (dev / testing only) ======
if (app.Environment.IsDevelopment() || isTesting)
{
    app.MapPost("/admin/import-to-cosmos",
        async (IStationService fileService, CosmosStationService cosmosService) =>
        {
            var allStations = fileService.GetAll();
            var imported = 0;

            foreach (var s in allStations)
            {
                var dto = new StationUpsertDto
                {
                    Number = s.Number,
                    Name = s.Name,
                    Address = s.Address,
                    Lat = s.Position?.Lat ?? 0,
                    Lng = s.Position?.Lng ?? 0,
                    BikeStands = s.Bike_Stands,
                    AvailableBikes = s.Available_Bikes,
                    Status = s.Status
                };

                var created = cosmosService.CreateStation(dto);
                if (created is not null)
                {
                    imported++;
                }
            }

            return Results.Ok(new
            {
                imported,
                totalSource = allStations.Count
            });
        });
}

app.Run();

// for WebApplicationFactory<Program> in tests
public partial class Program { }
