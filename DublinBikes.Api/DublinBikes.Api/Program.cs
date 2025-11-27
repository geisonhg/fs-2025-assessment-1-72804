using DublinBikes.Api.Services;
using DublinBikes.Api.Dtos;
using Microsoft.AspNetCore.Mvc;



var builder = WebApplication.CreateBuilder(args);

builder.Services.AddMemoryCache();

// uploud the service to the DI container and keep it as a singleton
builder.Services.AddSingleton<IStationService, FileStationService>();

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




app.Run();
