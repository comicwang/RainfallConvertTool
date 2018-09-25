using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainfallConvertTool.Model
{
    public class RAINFALL_DAY
    {
        public string GUID { get; set; }

        public string MONITORNUM { get; set; }

        public decimal? LON { get; set; }

        public decimal? LAT { get; set; }

        public decimal? ALT { get; set; }

        public DateTime? TIME { get; set; }

        public decimal? RAINFALL_1_DAY { get; set; }

        public decimal? RAINFALL_3_DAY { get; set; }

        public decimal? RAINFALL_5_DAY { get; set; }

        public decimal? RAINFALL_7_DAY { get; set; }

        public decimal? RAINFALL_15_DAY { get; set; }

        public decimal? RAINFALL_30_DAY { get; set; }

        public int? RAINFALL_1_DAY_C {get;set;}

        public int? RAINFALL_3_DAY_C { get; set; }

        public int? RAINFALL_5_DAY_C { get; set; }

        public int? RAINFALL_7_DAY_C { get; set; }

        public int? RAINFALL_15_DAY_C { get; set; }

        public int? RAINFALL_30_DAY_C { get; set; }
    }
}
