using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;

namespace Mapper_v1.Models;

public partial class MobileSettings : ObservableObject
{
    [ObservableProperty]
    private ObservableCollection<Mobile> mobiles;

    private FileService fileService = new();

    public MobileSettings()
    {
        Mobiles = LoadMobilesFromFile();
        if (Mobiles is null || Mobiles.Count==0)
        {
            Mobiles = [DefaultMobile()];
            SaveSettings();
        }
    }
    ~MobileSettings()
    {
        SaveSettings();
        Trace.WriteLine("destroy mobiles");
    }


    public ObservableCollection<Mobile> LoadMobilesFromFile(string userSelectedPath = null)
    {
        string folderPath = FileService.GetApplicationFolder();
        string filename = "Mobiles.json";
        if (userSelectedPath != null)
        {
            folderPath = Path.GetDirectoryName(userSelectedPath);
            filename = Path.GetFileName(userSelectedPath);
        }
        Mobiles = fileService.Read<ObservableCollection<Mobile>>(folderPath, filename);
        return Mobiles;
    }

    private Mobile DefaultMobile()
    {
        Mobile mobile = new();
        mobile.Devices = [];
        mobile.DeviceConfigs = [];
        mobile.Name = "boaty";
        mobile.IsPrimery = true;
        return mobile;
    }

    public void SaveSettings(string userSelectedPath = null)
    {
        ValidatePrimaryMobile();
        string folderPath = FileService.GetApplicationFolder();
        string filename = "Mobiles.json";
        if (userSelectedPath != null)
        {
            folderPath = Path.GetDirectoryName(userSelectedPath);
            filename = Path.GetFileName(userSelectedPath);
        }
        fileService.Save(folderPath, filename, Mobiles);
    }

    private void ValidatePrimaryMobile()
    {
        bool hasPrimary = false;
        foreach (var mobile in Mobiles)
        {
            if (mobile.IsPrimery)
            {
                if (hasPrimary == false)
                {
                    hasPrimary = true;
                    continue;
                }
                else
                {
                    mobile.IsPrimery = false;
                }
            }
        }
        // Orders the Mobiles so primery is first
        //Mobiles = [.. Mobiles.OrderByDescending(m => m.IsPrimery)];
    }

    internal void SaveSettings(ObservableCollection<Mobile> mobiles)
    {
        Mobiles = mobiles;
        SaveSettings();
    }
}
