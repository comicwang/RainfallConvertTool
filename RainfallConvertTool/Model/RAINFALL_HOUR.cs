using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainfallConvertTool.Model
{
    public class RAINFALL_HOUR
    {
        public string GUID { get; set; }

        public string MONITORNUM { get; set; }

        public decimal? LON { get; set; }

        public decimal? LAT { get; set; }

        public decimal? ALT { get; set; }

        public DateTime? TIME { get; set; }

        public decimal? RAINFALL_1_HOUR { get; set; }

        public decimal? RAINFALL_3_HOUR { get; set; }

        public decimal? RAINFALL_6_HOUR { get; set; }

        public decimal? RAINFALL_12_HOUR { get; set; }

        public decimal? RAINFALL_24_HOUR { get; set; }

        public decimal? RAINFALL_48_HOUR { get; set; }

        public decimal? RAINFALL_72_HOUR { get; set; }

        public int? RAINFALL_1_HOUR_C { get; set; }

        public int? RAINFALL_3_HOUR_C { get; set; }

        public int? RAINFALL_6_HOUR_C { get; set; }

        public int? RAINFALL_12_HOUR_C { get; set; }

        public int? RAINFALL_24_HOUR_C { get; set; }

        public int? RAINFALL_48_HOUR_C { get; set; }

        public int? RAINFALL_72_HOUR_C { get; set; }
    }
}
