using DublinBikes.Api.Dtos;
using DublinBikes.Api.Models;
using DublinBikes.Api.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Cosmos;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Options;

var builder = WebApplication.CreateBuilder(args);

// ====== Servicios básicos ======
builder.Services.AddMemoryCache();

builder.Services.AddSingleton<IStationService>(sp =>
{
    var config = sp.GetRequiredService<IConfiguration>();
    var cache = sp.GetRequiredService<IMemoryCache>();

    // Servicio V1 basado en archivo JSON
    return new FileStationService(config, cache);
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddHostedService<StationUpdateBackgroundService>();

// ====== Cosmos (V2) ======

// 1. Opciones de Cosmos desde appsettings.json -> sección "Cosmos"
builder.Services.Configure<CosmosOptions>(
    builder.Configuration.GetSection("Cosmos"));

// 2. Cliente de Cosmos como singleton
builder.Services.AddSingleton<CosmosClient>(sp =>
{
    var cosmosOptions = sp.GetRequiredService<IOptions<CosmosOptions>>().Value;
    return new CosmosClient(cosmosOptions.Endpoint, cosmosOptions.Key);
});

// 3. Servicio V2 que usará CosmosClient (ahora mismo puede ser un stub)
builder.Services.AddSingleton<CosmosStationService>();

var app = builder.Build();

// ¿estamos en modo tests?
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

// =================== endpoints v1 ===================

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

// =================== run ===================

app.Run();

// Necesario para los tests de integración
public partial class Program { }
