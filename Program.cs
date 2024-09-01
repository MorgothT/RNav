using ExCSS;
using System.Drawing;
using System.Drawing.Text;
using System.Runtime.InteropServices;
using System.Windows;
using Velopack;
using Velopack.Sources;
using Windows.Networking;

namespace Mapper_v1
{
    public class Program
    {
        [STAThread]
        public static void Main(string[] args)
        {
            try
            {
                if (IsFontInstalled("Rnav Regular"))
                {
                    AddFontResource(@"./Fonts/RNav.ttf");
                }
                VelopackApp.Build()
                                .WithFirstRun(v => MessageBox.Show("Thanks for installing RNav !"))
                                .SetAutoApplyOnStartup(false)
                                .WithAfterInstallFastCallback(v => AddFontResource(@"./Fonts/RNav.ttf"))
                                .WithBeforeUninstallFastCallback(v => RemoveFontResource(@"./Fonts/RNav.ttf"))
                                .Run();
                _ = UpdateMyApp();
                var app = new App();
                app.InitializeComponent();
                app.Run();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Unhandeled exception: {ex}");
            }
        }

        private static bool IsFontInstalled(string name)
        {
            using (InstalledFontCollection fontsCollection = new InstalledFontCollection())
            {
                return fontsCollection.Families
                    .Any(x => x.Name.Equals(name, StringComparison.CurrentCultureIgnoreCase));
            }
        }

        [DllImport("gdi32.dll", EntryPoint = "AddFontResourceW", SetLastError = true)]
        public static extern int AddFontResource([In][MarshalAs(UnmanagedType.LPWStr)]
                                         string lpFileName);
        [DllImport("gdi32.dll", EntryPoint = "RemoveFontResourceW", SetLastError = true)]
        public static extern int RemoveFontResource([In][MarshalAs(UnmanagedType.LPWStr)]
                                            string lpFileName);

        private static async Task UpdateMyApp()
        {
            var mgr = new UpdateManager("https://github.com/MorgothT/RNav");
            var newVersion = await mgr.CheckForUpdatesAsync();
            if (newVersion == null) 
                return;
            await mgr.DownloadUpdatesAsync(newVersion);
            var result = MessageBox.Show($"Version {newVersion.TargetFullRelease} is available.{Environment.NewLine}Do you wish to restart the application ?",
                                        "New version found",
                                        button: MessageBoxButton.YesNo,
                                        icon: MessageBoxImage.Question,
                                        defaultResult: MessageBoxResult.No);
            if (result == MessageBoxResult.Yes)
                mgr.ApplyUpdatesAndRestart(newVersion);
            else
                mgr.WaitExitThenApplyUpdates(newVersion.TargetFullRelease, restart: false);

        }
        //        private static async Task UpdateMyApp()
        //        {
        //            try
        //            {
        //#pragma warning disable CS0618 // Type or member is obsolete
        //                using (var mgr = await UpdateManager.GitHubUpdateManager("https://github.com/MorgothT/RNav"))
        //                //using (var mgr = await UpdateManager(new GithubSource("https://github.com/MorgothT/RNav")))
        //                {
        //                    var newVersion = await mgr.UpdateApp();

        //                    // optionally restart the app automatically, or ask the user if/when they want to restart

        //                    if (newVersion != null)
        //                    {
        //                        var result = MessageBox.Show($"Version {newVersion.Version} is available.{Environment.NewLine}Do you wish to restart the application ?",
        //                                        "New version found",
        //                                        button: MessageBoxButton.YesNo,
        //                                        icon: MessageBoxImage.Question,
        //                                        defaultResult: MessageBoxResult.No);
        //                        if (result == MessageBoxResult.Yes) UpdateManager.RestartApp();
        //                    }
        //                    else
        //                    {
        //                        MessageBox.Show("RNav is running the latest version");
        //                    }

        //                }
        //#pragma warning restore CS0618 // Type or member is obsolete
        //            }
        //            catch (Exception ex)
        //            {
        //                MessageBox.Show(ex.Message);
        //            }
        //        }
    }
}
