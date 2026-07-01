using Mapper_v1.Models;

namespace Mapper_v1.Contracts.Services
{
    public interface IConfigService
    {
        // NOT IMPLAMENTED
        MapSettings MapConfig { get; set; }
        MobileSettings MobileConfig { get; set; }
        //DataViewSettings DataView { get; }
        //void Save();
        Task InitializeAsync();
        Task SaveMapSettingsAsync();
        Task SaveMobileSettingsAsync();
        Task LoadMapSettingsFromFileAsync(string filePath);
        Task LoadMobilesFromFileAsync(string filePath);
    }
}
