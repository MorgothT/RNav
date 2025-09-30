using Mapper_v1.Core.Models;
using Mapper_v1.Core.Services;
using System.Collections.ObjectModel;
using System.IO;

namespace Mapper_v1.Models
{
    public static class DataViewSettings
    {
        private static FileService fileService = new FileService();
        public static ObservableCollection<DataViewItem> LoadDataViewSettingsFromFile(string filePath = null)
        {
            var settings = new ObservableCollection<DataViewItem>();
            // Load settings from a file
            // If filePath is null, use a default path or prompt the user to select a file
            string folderPath = FileService.GetApplicationFolder();
            string filename = "DataViewSettings.json";
            if (filePath != null)
            {
                folderPath = Path.GetDirectoryName(filePath);
                filename = Path.GetFileName(filePath);
            }
            settings = fileService.Read<ObservableCollection<DataViewItem>>(folderPath, filename);

            return settings;
        }

        public static void SaveDataViewSettings(ObservableCollection<DataViewItem> settings, string filePath = null )
        {
            // Save settings to a file
            string folderPath = FileService.GetApplicationFolder();
            string filename = "DataViewSettings.json";
            if (filePath != null)
            {
                folderPath = Path.GetDirectoryName(filePath);
                filename = Path.GetFileName(filePath);
            }
            fileService.Save(folderPath, filename, settings);
        }
    }
}
