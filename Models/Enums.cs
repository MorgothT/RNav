using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper_v1.Models;

public enum ConnectionType
{
    Serial,
    UDP,
    TCP
}
public enum DegreeFormat
{
    Deg,
    Min,
    Sec
}

public enum MapMode
{
    Navigate,
    Measure,
    Target
}