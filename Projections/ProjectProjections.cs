using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace Mapper_v1.Projections
{
    public static class ProjectProjections
    {
        public static readonly string ITM = "EPSG:6991";
        public static readonly string UTM_N36 = "EPSG:32636";
        //public static readonly string WGS84 = "EPSG:4326";
        //public static readonly string WorldMap = "EPSG:3857";

        public static List<string> GetProjections()
        {
            var proj = new List<string>();
            foreach (var prop in typeof(ProjectProjections).GetFields()) 
            {
                proj.Add($"{prop.Name} - {prop.GetValue(null)}");
            }
            return proj;
        }
    }
    
    
}
