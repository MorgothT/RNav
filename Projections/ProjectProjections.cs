using Mapper_v1.Models;

namespace Mapper_v1.Projections
{
    public static class ProjectProjections
    {
        public static List<string> GetProjections()
        {
            var proj = new List<string>();
            ProjectionCfg projections = new ProjectionCfg(".\\Projections.cfg");
            foreach (var projection in projections.CoordinateSystems) 
            {
                proj.Add($"{projection.Name} - {projection.Authority}:{projection.AuthorityCode}"); 
            }
            return proj;
        }
    }
}
