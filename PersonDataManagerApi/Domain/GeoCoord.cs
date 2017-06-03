namespace Hdq.PersonDataManager.Api.Domain
{
    public class GeoCoord
    {
        public GeoCoord(decimal lat, decimal lon)
        {
            Lat = lat;
            Lon = lon;
        }

        public decimal Lat { get; private set; }
        public decimal Lon { get; private set; }
    }
}