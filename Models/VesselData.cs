﻿using CommunityToolkit.Mvvm.ComponentModel;
using InvernessPark.Utilities.NMEA;
using InvernessPark.Utilities.NMEA.Sentences;
using Mapper_v1.Helpers;
using Mapsui;

namespace Mapper_v1.Models;

public partial class VesselData : ObservableObject
{
    // TODO: Structure properties such as Location,Depth,MRU
    [ObservableProperty]
    private double latitude = double.NaN;
    [ObservableProperty]
    private double longitude = double.NaN;
    [ObservableProperty]
    private double depth = double.NaN;
    [ObservableProperty]
    private double heading = double.NaN;
    [ObservableProperty]
    private double speedInKnots = double.NaN;
    [ObservableProperty]
    private double speedFw = double.NaN;
    [ObservableProperty]
    private double speedSb = double.NaN;

    [ObservableProperty]
    private double headingOffset = 0;
    [ObservableProperty]
    private double depthOffset = 0;
    [ObservableProperty]
    private MPoint positionOffset = new(0,0);

    [ObservableProperty]
    private GGA getGGA = new();
    [ObservableProperty]
    private GSA getGSA = new();
    [ObservableProperty]
    private GST getGST = new();
    [ObservableProperty]
    private GSV getGSV = new();
    [ObservableProperty]
    private HDT getHDT = new();
    [ObservableProperty]
    private RMC getRMC = new();
    [ObservableProperty]
    private VTG getVTG = new();
    [ObservableProperty]
    private DBT getDBT = new();
    [ObservableProperty]
    private DPT getDPT = new();
    [ObservableProperty]
    private ZDA getZDA = new();

    [ObservableProperty]
    private List<TimedPoint> trail = new();

    public void Update(INmeaMessage msg)
    {
        switch (msg.GetType().Name)
        {
            case "GGA":
                GetGGA = (GGA)msg;
                UpdateLocation(typeof(GGA));
                break;
            case "GSA":
                GetGSA = (GSA)msg;
                break;
            case "GST":
                GetGST = (GST)msg;
                break;
            case "GSV":
                GetGSV = (GSV)msg;
                break;
            case "HDT":
                GetHDT = (HDT)msg;
                UpdateHeading(typeof(HDT));
                break;
            case "RMC":
                GetRMC = (RMC)msg;
                break;
            case "VTG":
                GetVTG = (VTG)msg;
                UpdateSpeed(typeof(VTG));
                break;
            case "DBT":
                GetDBT = (DBT)msg;
                UpdateDepth(typeof(DBT));
                break;
            case "DPT":
                GetDPT = (DPT)msg;
                UpdateDepth(typeof(DPT));
                break;
            case "ZDA":
                GetZDA = (ZDA)msg;
                break;
            default:
                break;
        }
    }

    private void UpdateSpeed(Type type)
    {
        if (type == typeof(VTG))
        {
            double direction = Heading - GetVTG.TrueTrackMadeGoodDegrees;
            SpeedInKnots = GetVTG.GroundSpeedKnots;
            SpeedFw = SpeedInKnots * Math.Cos(direction * double.Pi / 180);
            SpeedSb = -SpeedInKnots * Math.Sin(direction * double.Pi / 180);
        }
    }

    private void UpdateHeading(Type type)
    {
        Heading = (GetHDT.HeadingTrue + HeadingOffset) % 360;
    }

    private void UpdateDepth(Type type)
    {
        if (type == typeof(DBT))
            Depth = (GetDBT.DepthInFeet * 0.3048) + DepthOffset;
        if (type == typeof(DPT))
            Depth = GetDPT.DepthInMeters + DepthOffset; // if offset in DPT and MapSettings is not 0, Depth will include BOTH
    }

    private void UpdateLocation(Type type)
    {
        MPoint point = new();
        MPoint offsetPoint = new();
        if (type == typeof(GGA))
        {
            point = new MPoint(GetGGA.Longitude.Degrees, GetGGA.Latitude.Degrees);
            //MPoint offsetPoint = GeoMath.AddOffsetToWgsPoint(point, PositionOffset.X, PositionOffset.Y);
            //Lattitude = offsetPoint.X; //GetGGA.Latitude.Degrees;
            //Longitude = offsetPoint.Y; //GetGGA.Longitude.Degrees;
        }
        if (type == typeof(RMC))
        {
            point = new MPoint(GetRMC.Longitude.Degrees, GetRMC.Latitude.Degrees);
        }
        offsetPoint = GeoMath.AddOffsetToWgsPoint(point, PositionOffset.X, PositionOffset.Y);
        Longitude = offsetPoint.X;
        Latitude = offsetPoint.Y;
    }
}

