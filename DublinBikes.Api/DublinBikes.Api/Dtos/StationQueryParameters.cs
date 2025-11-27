namespace DublinBikes.Api.Dtos
{
    public class StationQueryParameters
    {
        public string? Status { get; set; }
        public int? MinBikes { get; set; }
        public string? Q { get; set; }

        public string? Sort { get; set; }
        public string? Dir { get; set; }

        public int? Page { get; set; }
        public int? PageSize { get; set; }

    }
}
