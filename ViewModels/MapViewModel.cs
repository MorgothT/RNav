using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Mapper_v1.Contracts.ViewModels;
using Mapper_v1.Core;
using Mapper_v1.Core.Models;
using Mapper_v1.Core.Models.DataStruct;
using Mapper_v1.Core.Projections;
using Mapper_v1.Helpers;
using Mapper_v1.Models;
using Mapper_v1.Views;
using Mapsui;
using Mapsui.Projections;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace Mapper_v1.ViewModels;

public partial class MapViewModel : ObservableObject, INavigationAware
{
    public WktProjections Projection = new WktProjections();

    [ObservableProperty]
    private MapSettings mapSettings = new();
    [ObservableProperty]
    private MobileSettings mobileSettings = new();
    [ObservableProperty]
    private Dictionary<string, object> data = new();
    [ObservableProperty]
    private bool measurementMode = false;
    [ObservableProperty]
    private bool targetMode = false;
    [ObservableProperty]
    private MapMode currentMapMode;
    [ObservableProperty]
    private ObservableCollection<Mobile> mobiles;
    [ObservableProperty]
    private ObservableCollection<DataViewItem> dataViewItems = new();

    private PropertyChangedEventHandler MobileDataChanged;

    private string ProjectCRS;
    private Target currentTarget;

    public event Action<Guid> PositionChanged;
    public event Action<Guid> HeadingChanged;
    public MapViewModel()
    {
        InitMobiles();
        InitDataView();
        //InitDataView_Old();
        InitProjection();
    }

    private void InitProjection()
    {
        int epsg = int.Parse(MapSettings.CurrentProjection.Split(':')[1]);
        ProjectCRS = $"EPSG:{epsg}";

        // Set Position conversion delegates for ENToLatLon and LatLonToEN
        Position.LatLonToEN = (lon, lat) =>
        {
            MPoint p = new MPoint(lon, lat);
            Projection.Project("EPSG:4326", ProjectCRS, p);
            return (p.X, p.Y);
        };
        Position.ENToLatLon = (east, north) =>
        {
            MPoint p = new MPoint(east, north);
            Projection.Project(ProjectCRS, "EPSG:4326", p);
            return (p.Y, p.X); // Mapsui.MPoint: X=lon, Y=lat
        };
    }
    private void InitDataView()
    {
        // Load the data view items from the settings
        LoadDataViewItems();
    }
    private void InitMobiles()
    {
        try
        {
            Mobiles = MobileSettings.Mobiles;
            // there must be at least 1 mobile
            if (Mobiles.Count < 1) throw new Exception("There must be at least 1 mobile !");
            // there must be only 1 primery
            if (Mobiles.Count(m => m.IsPrimery) > 1)
            {
                // remove primery from all 
                foreach (var mobile in Mobiles)
                {
                    mobile.IsPrimery = false;
                }
            }
            if (Mobiles.Count(m => m.IsPrimery) == 0)
            {
                // make the 1st primery
                Mobiles.First().IsPrimery = true;
            }

            // INIT Mobiles devices
            foreach (var mobile in Mobiles)
            {
                mobile.InitDevices();
                MobileDataChanged = (sender, e) =>
                {
                    // Handle property changes in the mobile's data
                    if (e.PropertyName == nameof(Position))
                    {
                        OnMobilePositionChanged(mobile.Id, mobile.Data.Position);
                        // Notify UI about position change
                    }
                    else if (e.PropertyName == nameof(Motion))
                    {
                        OnMobileHeadingChanged(mobile.Id, mobile.Data.Motion.Heading);
                    }
                    // Update the data view for this mobile
                    UpdateDataView(mobile.Id);
                };
                mobile.Data.PropertyChanged += MobileDataChanged;
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Error: {ex.Message}");
        }
    }

    private void UpdateDataView(Guid id)
    {
        // Find the mobile by ID
        Mobile mobile = Mobiles.FirstOrDefault(m => m.Id == id);
        foreach (var item in DataViewItems)
        {
            //item.FontSize = MapSettings.FontSize;
            if (item.MobileId == id)
            {
                // Support nested property names, e.g. "Position.Latitude"
                object current = mobile.Data;
                var parts = item.Name.Split('.').Skip(1);
                foreach (var part in parts)
                {
                    if (current == null) break;
                    var prop = current.GetType().GetProperty(part);
                    if (prop == null) { current = null; break; }
                    current = prop.GetValue(current);
                }
                item.Value = FormatData(item, current);
            }
            if (item.MobileId == DataViewItem.SystemId)
            {
                object current = null;
                // Handle system-wide properties

                //TODO: subscribe to View enabling logging
                //if (item.Name == "LOGGING")
                //{
                //    //item.Value = ;
                //}
                
                // handle Traget properties
                if (item.Name.StartsWith("Target "))
                {
                    if (currentTarget is null)
                    {
                        current = double.NaN; // or "N/A" for string properties
                    }
                    else if(mobile.IsPrimery)
                    {
                        MPoint vesselPoint = new MPoint(mobile.Data.Position.East, mobile.Data.Position.North);
                        switch (item.Name)
                        {
                            case "Target X":
                                current = currentTarget.X;
                                break;
                            case "Target Y":
                                current = currentTarget.Y;
                                break;
                            case "Target Dist":
                                current = vesselPoint.Distance(currentTarget);
                                break;
                            case "Target Brg":
                                current = vesselPoint.CalcBearing(currentTarget);
                                break;
                            case "Target Name":
                                current = currentTarget.Name;
                                break;
                        }
                    }
                }
                if (current is not null)
                {
                    item.Value = FormatData(item, current);
                }
            }
        }
    }

    private void OnMobileHeadingChanged(Guid mobileId, double heading)
    {
        HeadingChanged?.Invoke(mobileId);
    }

    private void OnMobilePositionChanged(Guid mobileId, Position position)
    {
        TimedPoint currentLocation = new TimedPoint(position.East, position.North, Mobiles.Single(m=>m.Id == mobileId).Data.Time.DateTime.ToLocalTime());
        // Log the location to a file
        LogLocation(mobileId, currentLocation);
        // Notify the view
        PositionChanged?.Invoke(mobileId);
    }

    /// <summary>
    /// Logs the location of a mobile device to a daily log file.
    /// </summary>
    /// <remarks>This method writes the location data to a log file in the directory specified by <see
    /// cref="MapSettings.LogDirectory"/>. The log file is named using the current date in the format "yyyyMMdd.log". If
    /// the log directory does not exist, it is created.</remarks>
    /// <param name="mobileId">The unique identifier of the mobile device whose location is being logged.</param>
    /// <param name="gpsPoint">The GPS coordinates of the mobile device, represented as an <see cref="MPoint"/> object.</param>
    private void LogLocation(Guid mobileId, TimedPoint point)
    {
        string mobileName = Mobiles.Where(m => m.Id == mobileId).FirstOrDefault()?.Name ?? "UnknownMobile";
        string logPath = @$"{MapSettings.LogDirectory}\{DateTime.Today.ToString("yyyyMMdd")}_{mobileName}.log";
        if (!Directory.Exists(MapSettings.LogDirectory))
        {
            Directory.CreateDirectory(MapSettings.LogDirectory);
        }
        //var mobileData = Mobiles.Where(m => m.IsPrimery).FirstOrDefault().Data;
        //MPoint gpsPoint = new MPoint(mobileData.Position.Longitude, mobileData.Position.Latitude);
        //ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        //// Create a new TimedPoint with the projected coordinates and the local time
        //TimedPoint point = new TimedPoint(gpsPoint.X, gpsPoint.Y, mobileData.Time.DateTime.ToLocalTime());
        File.AppendAllText(logPath, $"{point}\n");
    }

    /// <summary>
    /// Logging vessel trail to file using WGS84 -> Project projection
    /// </summary>
    private void LogLocation()
    {
        string logPath = @$"{MapSettings.LogDirectory}\{DateTime.Today.ToString("yyyyMMdd")}.log";
        if (!Directory.Exists(MapSettings.LogDirectory))
        {
            Directory.CreateDirectory(MapSettings.LogDirectory);
        }
        var mobileData = Mobiles.Where(m => m.IsPrimery).FirstOrDefault().Data;
        MPoint gpsPoint = new MPoint(mobileData.Position.Longitude, mobileData.Position.Latitude);
        ProjectionDefaults.Projection.Project("EPSG:4326", ProjectCRS, gpsPoint);
        // Create a new TimedPoint with the projected coordinates and the local time
        TimedPoint point = new TimedPoint(gpsPoint.X, gpsPoint.Y, mobileData.Time.DateTime.ToLocalTime());
        File.AppendAllText(logPath, $"{point.ToLocalTime()}\n");
    }
    private object FormatData(DataViewItem item, object current)
    {
        if (item.Type == typeof(double))
        {
            if (item.DisplayName == "Latitude" || item.DisplayName == "Longitude")
            {
                // Format Lat/Lon values to type in settings
                item.Value = ((double)current).FormatLatLong(MapSettings.DegreeFormat);
            }
            else
            {
                // Format double values to n decimal places
                item.Value = ((double)current).ToString($"F{item.DecimalPlaces}");
            }
        }
        else if (item.Type == typeof(string))
        {
            // For string values, just assign the value directly
            item.Value = current?.ToString() ?? "N/A";
        }
        else if (item.Type == typeof(float))
        {
            // Format float values to n decimal places
            item.Value = ((float)current).ToString($"F{item.DecimalPlaces}");
        }
        else if (item.Type == typeof(int))
        {
            // Format int values as whole numbers
            item.Value = ((int)current).ToString();
        }
        else if (item.Type == typeof(bool))
        {
            // Convert bool to Yes/No
            item.Value = (bool)current ? "Yes" : "No";
        }
        else if (item.Type.BaseType == typeof(Enum))
        {
            item.Value = current.ToString().Replace('_', ' ');
        }
        else
        {
            item.Value = current?.ToString() ?? "N/A";
        }
        return item.Value;
    }
    private void LoadDataViewItems()
    {
        // Load the selected items from the default settings location
        DataViewItems = DataViewSettings.LoadDataViewSettingsFromFile();
        // Check if the loaded items are null or empty
        DataViewItems ??= [];
        if (DataViewItems.Count == 0)
        {
            // If no items are loaded, add default items
            AddDefaultDataViewItems();
            SaveDataViewItems();
        }
        ClearDataValues();
    }

    private void ClearDataValues()
    {
        foreach (var item in DataViewItems)
        {
            item.Value = null; // Clear the value for each item
        }
    }

    private void SaveDataViewItems()
    {
        DataViewSettings.SaveDataViewSettings(DataViewItems);
    }

    private void AddDefaultDataViewItems()
    {
        DataViewItems.Add(new DataViewItem("LOGGING", typeof(bool),DataViewItem.SystemId, false));

    }
    internal void OnSelectedTargetChanged(Target newTarget)
    {
        // Update the current target
        currentTarget = newTarget;
        UpdateDataView(Mobiles.Where(m => m.IsPrimery).FirstOrDefault().Id);
    }

    #region Commands
    [RelayCommand]
    private void Measure()
    {
        if (MeasurementMode)
        {
            CurrentMapMode = MapMode.Measure;
            TargetMode = false;
        }
        else if (!TargetMode) { CurrentMapMode = MapMode.Navigate; }
    }
    [RelayCommand]
    private void Target()
    {
        if (TargetMode)
        {
            CurrentMapMode = MapMode.Target;
            MeasurementMode = false;
        }
        else if (!MeasurementMode) { CurrentMapMode = MapMode.Navigate; }
    }
    [RelayCommand]
    private void OpenDataDisplayDialog()
    {
        // Create the dialog window and its ViewModel
        var dialogViewModel = new ShellDialogViewModel();
        var dialogWindow = new ShellDialogWindow(dialogViewModel);

        // Create the DataDisplayDialogPage and its ViewModel
        var dataDisplayDialogViewModel = new DataDisplayDialogViewModel(DataViewItems, MobileSettings);
        var dataDisplayDialogPage = new DataDisplayDialogPage(dataDisplayDialogViewModel);

        // Navigate the dialog's frame to the page
        dialogWindow.GetDialogFrame().Navigate(dataDisplayDialogPage);
        // Show as dialog
        dialogWindow.ShowDialog();
        DataViewItems = dataDisplayDialogViewModel.SelectedItems;
        //foreach (var item in DataViewItems)
        //{
        //    item.FontSize = MapSettings.FontSize;
        //}
        SaveDataViewItems();
    }
    #endregion

    public void OnNavigatedTo(object parameter)
    {
    }
    public void OnNavigatedFrom()
    {
        try
        {
            foreach (var mobile in Mobiles)
            {
                mobile.Dispose();
            }
        }
        catch (Exception)
        {
            throw;
        }
    }

    
}
