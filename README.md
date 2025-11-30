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
