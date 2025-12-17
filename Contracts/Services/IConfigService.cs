using Mapper_v1.Models;

namespace Mapper_v1.Contracts.Services
{
    public interface IConfigService
    {
        // TODO: implement ObservableObject on all configs (dataview) 
        MapSettings MapConfig { get; }
        MobileSettings MobileConfig { get; }
        //DataViewSettings DataView { get; }
        void Save();
    }
}
