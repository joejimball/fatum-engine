using System;
using System.IO;

namespace FatumCommon.Domain
{
    [Serializable]
    public class FatumCsv
    {
        public long Id { get; set; }
        public double Radio { get; set; }
        public double MaxDenseLat { get; set; }
        public double MaxDenseLon{ get; set; }


        //public GpsPoint MaxDenseCoordenada { get; set; }
        //public List<GpsPoint> Coordenadas { get; set; }

        public static FatumCsv Load(string path)
        {
            FatumCsv fatumFileCsv = new FatumCsv();
            var filename = Path.GetFileName(path);
            var arr = filename.Replace('@', '.').Split('_');
            fatumFileCsv.MaxDenseLat = double.Parse(arr[0]);
            fatumFileCsv.MaxDenseLon = double.Parse(arr[1]);
            fatumFileCsv.Radio = Convert.ToInt32(arr[2]);
            return fatumFileCsv;
        }
    }
}
