using CsvHelper.Configuration.Attributes;
using System;

namespace FatumCommon.Domain
{
    [Serializable]
    public class GpsPoint
    {
        [Ignore]
        public long Id { get; set; }
        [Index(2)]
        public double Longitude { get; set; }
        [Index(1)]
        public double Latitude { get; set; }
        [Ignore] 
        public long IdFatumCsv { get; set; }
        public GpsPoint()
        {
            this.Latitude = 0;
            this.Longitude = 0;
        }
        public GpsPoint(double latitude, double longitude)
        {
            this.Latitude = latitude;
            this.Longitude = longitude;
        }

        public static GpsPoint Create(string latitude, string longitude)
        {
            var coor = new GpsPoint();
            coor.Latitude = double.Parse(latitude);
            coor.Longitude = double.Parse(longitude);
            return coor;
        }
    }
}
