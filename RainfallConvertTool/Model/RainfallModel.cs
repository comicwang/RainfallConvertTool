using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace RainfallConvertTool.Model
{
    public class RainfallModel
    {
        public RainfallModel(string MONITORNUM,decimal? lon,decimal? lat,decimal? alt,DateTime date,decimal? rainfall,int controller)
        {
            this.GUID = Guid.NewGuid().ToString();
            this.MONITORNUM = MONITORNUM;
            this.LON = lon;
            this.LAT = lat;
            this.ALT = alt;
            this.RecordDate = date;
            this.RAINFALL = rainfall;
            this.Controller = controller;
        }
        public string GUID { get; set; }

        public string MONITORNUM { get; set; }
        public decimal? LON { get; set; }
        public decimal? LAT { get; set; }
        public decimal? ALT { get; set; }
        public DateTime RecordDate { get; set; }
        public int Controller { get; set; }
        public decimal? RAINFALL { get; set; }
    }
}
