using CommunityToolkit.Mvvm.ComponentModel;
using InvernessPark.Utilities.NMEA;
using InvernessPark.Utilities.NMEA.Sentences;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Mapper_v1.Models;

public partial class DataViewItems:ObservableObject
{
	[ObservableProperty]
	private List<DataViewItem> availableDataItems;
    [ObservableProperty]
    private List<DataViewItem> currentDataItems;

    private List<string> availableProps = new();

    public DataViewItems(VesselData vessel)
    {
        //TODO: Populate list of items to view
    }
}
