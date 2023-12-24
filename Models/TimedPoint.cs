using CommunityToolkit.Mvvm.ComponentModel;
using InvernessPark.Utilities.NMEA.Sentences;
using Mapper_v1.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper_v1.Models
{
    public partial class TimedPoint : ObservableObject
    {
        [ObservableProperty]
        private double x;
        [ObservableProperty]    
        private double y;
        [ObservableProperty]
        private DateTime time;

        public TimedPoint() { X = 0; Y = 0; Time = DateTime.MinValue; }
        public TimedPoint(GGA nmea,GeoConverter.Converter converter)
        {
            double lon = nmea.Longitude.Degrees;
            double lat = nmea.Latitude.Degrees;
            var p = converter.Convert(new(lon, lat, 0));
            DateTime time = DateTime.Today.AddSeconds(nmea.UTC.TotalSeconds);

            X = p.X; Y = p.Y;
            Time = time;
        }

        public override string ToString()
        {
            return $"{Time:yyyy/MM/dd HH:mm:ss.fff},{X:F2},{Y:F2}";
        }
        public string ToLocalTime()
        {
            return $"{Time.ToLocalTime():yyyy/MM/dd HH:mm:ss.fff},{X:F2},{Y:F2}";
        }
    }
}
