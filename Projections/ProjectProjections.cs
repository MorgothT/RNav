using Mapper_v1.Core.Projections;
using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using System.IO;
using System.Windows;

namespace Mapper_v1.Projections;

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
    public static void AddProjection(string wkt)
    {
        // Validate WKT
        try
        {
            ProjectionCfg projections = new ProjectionCfg(".\\Projections.cfg");
            IInfo proj = CoordinateSystemWktReader.Parse(wkt);
            if (projections.CoordinateSystems.Any(p => p.Name == proj.Name))
            {
                MessageBox.Show("A projection with the same name already exists.", "Duplicate Projection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }
            if (projections.CoordinateSystems.Any(p=> p.EqualParams(proj)))
            {
                MessageBox.Show("An identical projection already exists.", "Duplicate Projection", MessageBoxButton.OK, MessageBoxImage.Warning);
                return; 
            }
            using StreamWriter sw = File.AppendText(".\\Projections.cfg");
            sw.WriteLine(wkt);
        }
        catch (Exception)
        {
            MessageBox.Show("The provided WKT is not valid.", "Invalid WKT", MessageBoxButton.OK, MessageBoxImage.Error);
            return;
        }
        
    }
}

