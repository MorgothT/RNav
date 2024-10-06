using ProjNet.CoordinateSystems;
using ProjNet.IO.CoordinateSystems;
using System.IO;

namespace Mapper_v1.Models;

public class ProjectionCfg
{
    public List<IInfo> CoordinateSystems { get; set; } = new();
    public ProjectionCfg(string path)
    {
        if (File.Exists(path) == false)
            throw new Exception($"Couldn't find projection config file: {path}");
        try
        {
            string[] lines = File.ReadAllLines(path);
            foreach (string line in lines)
            {
                // allow blank lines in the file
                if (string.IsNullOrEmpty(line)) continue;
                // allow comment lines in the file
                if (line.StartsWith(@"//")) continue;
                IInfo cs = CoordinateSystemWktReader.Parse(line);
                CoordinateSystems.Add(cs);
            }
        }
        catch (Exception ex)
        {
            throw new Exception($"Couldn't parse WKT line, {ex}");
        }
    }
}
