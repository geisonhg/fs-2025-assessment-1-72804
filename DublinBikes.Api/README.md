# DublinBikes API – .NET 8 Web API

Full Stack Assignment 1 – JSON-backed API for Dublin Bikes.

This project implements a .NET 8 Web API that:

- Loads the provided `dublinbike.json` file at startup.
- Exposes versioned endpoints under `/api/v1/...`.
- Supports searching, filtering, sorting and paging.
- Exposes summary/aggregate information about all stations.
- Uses an in-memory cache so that query results are cached for 5 minutes.
- Runs a background service that simulates live updates of station availability.
- Includes unit tests for the service layer.

> **Note:** Version 2 (`/api/v2/...`) will reuse the same endpoint shapes but load data from Azure CosmosDB instead of the local JSON file.

---

## 1. Technology stack

- **.NET**: .NET 8
- **API style**: Minimal APIs
- **Language**: C#
- **Testing**: xUnit
- **Caching**: `IMemoryCache`
- **Background processing**: `BackgroundService` (`StationUpdateBackgroundService`)

Project structure:

- `DublinBikes.Api` – Web API project
  - `Data/dublinbike.json` – source JSON for the stations
  - `Models` – domain models (`Station`, `GeoPosition`, ...)
  - `Dtos` – DTOs (`StationDto`, `StationUpsertDto`, `StationsSummaryDto`, `PagedResult<T>`, ...)
  - `Services` – service layer (`FileStationService`, `IStationService`, `StationUpdateBackgroundService`, ...)
  - `Program.cs` – minimal API endpoint configuration and DI setup
- `DublinBikes.Tests` – xUnit test project
  - `StationTestData` – sample in-memory data for tests
  - `StationServiceTests` – unit tests for filtering/sorting/paging

---

## 2. How to run the API

### Prerequisites

- .NET 8 SDK installed
- Visual Studio 2022 (or VS Code) with .NET 8 support

### Steps

1. Clone the repository and switch to the correct branch:

   ```bash
   git clone https://github.com/<your-user>/fs-2025-assessment-1-72804.git
   cd fs-2025-assessment-1-72804
   git checkout feature/dublin-bikes-v1
