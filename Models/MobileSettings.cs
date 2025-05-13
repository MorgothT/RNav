using CommunityToolkit.Mvvm.ComponentModel;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Services;
using System.Collections.ObjectModel;
using System.Diagnostics;

namespace Mapper_v1.Models
{
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
        

        private ObservableCollection<Mobile> LoadMobilesFromFile()
        {
            string folderPath = FileService.GetApplicationFolder();
            Mobiles = fileService.Read<ObservableCollection<Mobile>>(folderPath, "Mobiles.json");
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
        
        //public MobileSettings GetSettings()
        //{
        //    string folderPath = fileService.GetApplicationFolder();
        //    Mobiles = fileService.Read<ObservableCollection<Mobile>>(folderPath, "Mobiles.json");
        //    return this;
        //    //if (string.IsNullOrEmpty(Properties.Mobile.Default.Mobiles))
        //    //{
        //    //    Mobiles = new();
        //    //    SaveSettings();
        //    //}
        //    //else
        //    //{
        //    //    Mobiles = JsonConvert.DeserializeObject<ObservableCollection<Mobile>>(Properties.Mobile.Default.Mobiles);
        //    //}
        //    //return this;

        //}

        public void SaveSettings()
        {
            ValidatePrimaryMobile();
            string folderPath = FileService.GetApplicationFolder();
            fileService.Save(folderPath, "Mobiles.json", Mobiles);
            //Properties.Mobile.Default.Mobiles = JsonConvert.SerializeObject(Mobiles);
            //Properties.Mobile.Default.Save();
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
            Mobiles = [.. Mobiles.OrderByDescending(m => m.IsPrimery)];
        }

        internal void SaveSettings(ObservableCollection<Mobile> mobiles)
        {
            Mobiles = mobiles;
            SaveSettings();
        }
    }
}
