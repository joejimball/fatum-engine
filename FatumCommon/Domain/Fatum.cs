using Dapper.Contrib.Extensions;
using FatumCommon.Enums;
using System;

namespace FatumCommon.Domain
{
    [Serializable]
    [Table("fatums")]
    public class Fatum 
    {
        public long Id { get; set; }
        public double Latitude { get; set; }
        public double Longitude { get; set; }
        public string? OpenLocationCode { get; set; }
        public string? GeoHash { get; set; }
        public string? What3Words { get; set; }
        public int? GridSize { get; set; }
        public double? Bandwidth { get; set; }
        public double Radio { get; set; }
        public long NumPuntos { get; set; }
        public EnumTypeRnd TypeRnd { get; set; }
        public DateTime CreatedAt { get; set; }
        public byte[] RandomData { get; set; }
    }
}
