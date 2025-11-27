using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using System.Collections.Generic;
using DublinBikes.Api.Models;

namespace DublinBikes.Tests
{
    internal static class StationTestData
    {
        public static List<Station> CreateSampleStations()
        {
            return new List<Station>
            {
                new Station
                {
                    Number = 1,
                    Name = "Alpha Street",
                    Address = "Alpha",
                    Position = new GeoPosition { Lat = 53.35, Lng = -6.26 },
                    Bike_Stands = 10,
                    Available_Bikes = 5,
                    Available_Bike_Stands = 5,
                    Status = "OPEN",
                    Last_Update = 0
                },
                new Station
                {
                    Number = 2,
                    Name = "Beta Road",
                    Address = "Beta",
                    Position = new GeoPosition { Lat = 53.36, Lng = -6.27 },
                    Bike_Stands = 20,
                    Available_Bikes = 0,
                    Available_Bike_Stands = 20,
                    Status = "CLOSED",
                    Last_Update = 0
                },
                new Station
                {
                    Number = 3,
                    Name = "Gamma Avenue",
                    Address = "Gamma",
                    Position = new GeoPosition { Lat = 53.37, Lng = -6.28 },
                    Bike_Stands = 15,
                    Available_Bikes = 10,
                    Available_Bike_Stands = 5,
                    Status = "OPEN",
                    Last_Update = 0
                }
            };
        }
    }
}
