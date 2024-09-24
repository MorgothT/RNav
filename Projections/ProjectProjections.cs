using Mapper_v1.Models;
using Mapsui.Projections;

namespace Mapper_v1.Projections
{
    public static class ProjectProjections
    {
        // TODO: Add/Remove Available Projection from project
        public static readonly string ITM = "EPSG:6991";
        public static readonly string UTM_N36 = "EPSG:32636";
        //public static readonly string WGS84 = "EPSG:4326";
        //public static readonly string WorldMap = "EPSG:3857";

        public static List<string> GetProjections()
        {
            var proj = new List<string>();
            ProjectionCfg projections = new ProjectionCfg(".\\Projections.cfg");
            foreach (var projection in projections.CoordinateSystems) 
            {
                proj.Add($"{projection.Name} - {projection.Authority}:{projection.AuthorityCode}"); 
            }
            return proj;
            //var proj = new List<string>();
            //foreach (var prop in typeof(ProjectProjections).GetFields())
            //{
            //    proj.Add($"{prop.Name} - {prop.GetValue(null)}");
            //}
            //return proj;
        }
    }


}
