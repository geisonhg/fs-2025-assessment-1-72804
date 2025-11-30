# Dublin Bikes API – Full Stack Assignment

This project is my solution for the Full Stack 2025 assessment.  
The goal is to build a REST API for Dublin Bikes stations, with two versions:

- **V1**: reads data from a local JSON file.
- **V2**: reads and writes data using **Azure Cosmos DB**.

The API is built with **.NET 8 Minimal APIs**.

---

## Project structure

Main projects:

- `DublinBikes.Api` – Minimal API, endpoints, services.
- `DublinBikes.Tests` – xUnit tests for the service and the API.

Main services:

- `FileStationService` (V1) – uses `dublinbike.json` file.
- `CosmosStationService` (V2) – uses Azure Cosmos DB.

Background service:

- `StationUpdateBackgroundService` – periodically updates station data in memory.

---

## How to run the API

From the solution root:

```bash
dotnet build
dotnet run --project DublinBikes.Api

By default it runs on https://localhost:7232 (or similar, Visual Studio can show the exact URL).

Swagger UI is available in Development environment at:

https://localhost:7232/swagger

Configuration
V1 – File data

V1 loads all stations from:

DublinBikes.Api/Data/dublinbike.json


The data is stored in memory and then used by FileStationService.

V2 – Cosmos DB

V2 uses Azure Cosmos DB.
You need to configure this section in appsettings.json:

"Cosmos": {
  "Endpoint": "https://YOUR-COSMOS-ACCOUNT.documents.azure.com:443/",
  "Key": "YOUR-COSMOS-KEY",
  "DatabaseId": "DublinBikesDb",
  "ContainerId": "Stations"
}


The models are mapped so V1 and V2 return the same DTO (StationDto), including:

Location (lat, lng)

Status

Bikes / stands

Occupancy

LastUpdateLocal in Europe/Dublin time zone.

API endpoints
V1 – /api/v1 (file-based)

GET /api/v1/stations
Query params:

status – filter by status (e.g. OPEN, CLOSED)

minBikes – minimum available bikes

q – search text in name or address

sort – name, availableBikes, occupancy

dir – asc or desc

page – page number (1-based)

pageSize – items per page

GET /api/v1/stations/{number}
Returns a single station by number.

200 OK if found

404 Not Found if not found

GET /api/v1/stations/summary
Returns:

total stations

open / closed stations

total bike stands

total available bikes

POST /api/v1/stations
Creates a new station.

201 Created if created

409 Conflict if the station number already exists

PUT /api/v1/stations/{number}
Updates an existing station.

200 OK if updated

404 Not Found if the station does not exist

V2 – /api/v2 (Cosmos DB)

Same contract as V1, but using CosmosStationService:

GET /api/v2/stations

GET /api/v2/stations/{number}

GET /api/v2/stations/summary

POST /api/v2/stations

PUT /api/v2/stations/{number}

This allows to compare:

V1: file + in-memory data.

V2: real database (Cosmos DB) with the same DTOs and behavior.

Caching

Both services use IMemoryCache:

Cached list results for 5 minutes (based on query parameters).

Cached single station lookups.

Cached summary results.

This reduces repeated queries, especially for Cosmos DB.

Background updates

StationUpdateBackgroundService runs periodically and updates station data in memory.
It uses the service interface IStationService, so it can work with the file-based data.

Running tests

From the solution root:

dotnet test


The tests check:

Service logic (filtering, sorting, paging).

One integration test calling the API using WebApplicationFactory<Program>.

At the moment all tests pass (4/4).

Postman

There is a Postman collection (optional for the assignment) to test:

GET /api/v1/stations (status 200, JSON)

GET /api/v2/stations

Other endpoints like GET by number, summary, POST, PUT.

Typical environment variable:

baseUrl = https://localhost:7232

Example request:

GET {{baseUrl}}/api/v1/stations?status=OPEN&minBikes=5&page=1&pageSize=10

Notes

Built with .NET 8.

Uses Minimal APIs and dependency injection.

Time zone for LastUpdateLocal is Europe/Dublin.

V1 and V2 share the same DTOs so clients do not need to change when switching data source.
