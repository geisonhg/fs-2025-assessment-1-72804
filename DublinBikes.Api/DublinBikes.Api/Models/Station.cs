using System;
using Newtonsoft.Json;

namespace DublinBikes.Api.Models
{
    public class Station
    {
        // Cosmos requiere la propiedad JSON "id"
        [JsonProperty(PropertyName = "id")]
        public string Id { get; set; } = string.Empty;

        public int Number { get; set; }

        public string Contract_Name { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public string Address { get; set; } = string.Empty;

        public GeoPosition Position { get; set; } = new GeoPosition();

        public bool Banking { get; set; }

        public bool Bonus { get; set; }

        public int Bike_Stands { get; set; }

        public int Available_Bike_Stands { get; set; }

        public int Available_Bikes { get; set; }

        public string Status { get; set; } = string.Empty;

        public long Last_Update { get; set; }

        public DateTimeOffset LastUpdateUtc =>
            DateTimeOffset.FromUnixTimeMilliseconds(Last_Update);

        public double Occupancy =>
            Bike_Stands == 0 ? 0 : (double)Available_Bikes / Bike_Stands;
    }
}
