namespace DublinBikes.Api.Dtos
{
    public class StationUpsertDto
    {
        public int Number { get; set; }

        public string Name { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;

        public double Lat { get; set; }
        public double Lng { get; set; }

        public int BikeStands { get; set; }
        public int AvailableBikeStands { get; set; }
        public int AvailableBikes { get; set; }

        public string Status { get; set; } = "OPEN";
    }
}
