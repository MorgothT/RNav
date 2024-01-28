using Mapsui;
using Mapsui.Layers;
using Mapsui.Providers;
using Mapsui.Styles;
using SkiaSharp;
using SkiaSharp.Views.Desktop;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Runtime.InteropServices;
using Color = Mapsui.Styles.Color;

namespace Mapper_v1.Providers
{
#pragma warning disable CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
    public class EcwProvider : IProvider, IDisposable
    {
        private double MAXRESULOTION = 0.2;
        private struct WorldProperties
        {
            public double PixelSizeX;

            public double RotationAroundYAxis;

            public double RotationAroundXAxis;

            public double PixelSizeY;

            public double XCenterOfUpperLeftPixel;

            public double YCenterOfUpperLeftPixel;
        }
        //private struct EcwProperties
        //{
        //    public double Width;

        //    public double Height;

        //    public double HResolution;

        //    public double VResolution;
        //}
        private NCSFileInfo FileInfo;

        private const string WorldExtension = "eww";

        private readonly IFeature _feature;

        private readonly MRect _extent;

        private MRaster _mRaster;
        public string CRS { get; set; } = "";

        public EcwProvider(string ecwpath, List<Color>? colorTrans) : this(ecwpath, colorTrans, null) { }
        public EcwProvider(string ecwpath, List<Color>? colorTrans,double? resulotion)
        {
            if (resulotion is not null) MAXRESULOTION = (double)resulotion;
            if (!File.Exists(ecwpath))
            {
                throw new ArgumentException($"ECW file expected at {ecwpath}");
            }
            string ewwpath = $"{GetPathWithoutExtension(ecwpath)}.{WorldExtension}";
            if (!File.Exists(ewwpath))
            {
                throw new ArgumentException($"World file expected at {ewwpath}");
            }
            FileInfo = ReadFileInfo(ecwpath);
            WorldProperties worldProperties = LoadWorld(ewwpath);
            int scale = FileInfo.fCellIncrementX < MAXRESULOTION ? (int)(MAXRESULOTION / FileInfo.fCellIncrementX) : 1;
            using (MemoryStream memoryStream = ReadFile(ecwpath, FileInfo, colorTrans, scale))
            {
                _extent = CalculateExtent(FileInfo, worldProperties);
                _mRaster = new MRaster(memoryStream.ToArray(), _extent);
                _feature = new RasterFeature(_mRaster);
                _feature.Styles.Add(new RasterStyle());
            }
        }

        #region DllLib
        internal const string DllPath = "NCSEcw.dll";
        [DllImport(DllPath)]
        public static extern void NCSInit();

        [DllImport(DllPath)]
        public static extern void NCSShutdown();

        [DllImport(DllPath)]
        public static extern void NCSGetLibVersion(out int major, out int minor);
        [DllImport(DllPath, CharSet = CharSet.Auto)]
        public static extern NCSError NCSOpenFileView(string fileName, out IntPtr pNCSFileView, IntPtr callBack);
        [DllImport(DllPath)]
        public static extern NCSError NCSCloseFileViewEx(IntPtr pNCSFileView, bool clearCache);
        [DllImport(DllPath)]
        public static extern NCSError NCSGetViewFileInfo(IntPtr pNCSFileView, out IntPtr ppNCSFileViewFileInfoPointer);
        //// https://docs.microsoft.com/de-de/dotnet/framework/interop/marshaling-different-types-of-arrays
        [DllImport(DllPath)]
        public static extern NCSError NCSSetFileView(IntPtr pNCSFileView, uint nBands, [In, Out] uint[] pBandList, uint nTLX, uint nTLY, uint nBRX, uint nBRY, uint nSizeX, uint nSizeY);
        [DllImport(DllPath)]
        public static extern NCSError NCSReadViewLineBGR(IntPtr pNCSFileView, IntPtr pBGRTriplets);
        // extern NCSReadStatus NCS_CALL NCSReadViewLineBGRA( NCSFileView *pNCSFileView, UINT32 *pBGRA);
        [DllImport(DllPath)]
        public static extern NCSError NCSReadViewLineBGRA(IntPtr pNCSFileView, IntPtr pBGRTriplets);
        [DllImport(DllPath, CharSet = CharSet.Auto)]
        public static extern NCSError NCSGetEPSGCode(string szDatum, string szProjection, out int epsg);
        #endregion
        #region NCSStructs
        public struct NCSFileInfo
        {
            public UInt32 nSizeX;
            public UInt32 nSizeY;
            public UInt16 nBands;
            public UInt16 nCompressionRate;
            public NCSCellSizeUnitType eCellSizeUnits;
            public Double fCellIncrementX;
            public Double fCellIncrementY;
            public Double fOriginX;
            public Double fOriginY;
            public string szDatum;
            public string szProjection;
            public Double fCWRotationDegrees;
            public NCSFileColorSpace eColorSpace;
            public NCSCellType eCellType;
            public IntPtr pBands;
            public Byte nFormatVersion;
            public Byte nCellBitDepth;
            public string sCompressionDate;
            public float fActualCompressionRate;
            public IntPtr pFileMetaData;
        };
        public struct NCSFileBandInfo
        {
            public byte nBits;
            public bool bSigned;
            public string szDesc;
        };
        public struct NCSFileMetaData
        {
            public unsafe char* sClassification;
            public unsafe char* sAcquisitionDate;
            public unsafe char* sAcquisitionSensorName;
            public unsafe char* sCompressionSoftware;
            public unsafe char* sAuthor; // updatable
            public unsafe char* sCopyright;
            public unsafe char* sCompany; // updatable
            public unsafe char* sEmail; // updatable
            public unsafe char* sAddress;// updatable
            public unsafe char* sTelephone; // updatable
        }
    #endregion
        #region Enums
    public enum NCSCellSizeUnitType
        {
            ECW_CELL_UNITS_INVALID = 0,
            ECW_CELL_UNITS_METERS = 1,
            ECW_CELL_UNITS_DEGREES = 2,
            ECW_CELL_UNITS_FEET = 3,
            ECW_CELL_UNITS_UNKNOWN = 4
        };
        public enum NCSFileColorSpace
        {
            NCSCS_NONE = 0,
            NCSCS_GREYSCALE = 1,    // Greyscale
            NCSCS_YUV = 2,    // YUV - JPEG Digital, JP2 ICT
            NCSCS_MULTIBAND = 3,    // Multi-band imagery
            NCSCS_sRGB = 4,    // sRGB
            NCSCS_YCbCr = 5             // YCbCr - JP2 ONLY, Auto-converted to sRGB
        };
        public enum NCSCellType
        {
            NCSCT_UINT8 = 0,
            NCSCT_UINT16 = 1,
            NCSCT_UINT32 = 2,
            NCSCT_UINT64 = 3,
            NCSCT_INT8 = 4,
            NCSCT_INT16 = 5,
            NCSCT_INT32 = 6,
            NCSCT_INT64 = 7,
            NCSCT_IEEE4 = 8,
            NCSCT_IEEE8 = 9,
            NCSCT_NUMVALUES = 10
        };
        public enum NCSError
        {
            NCS_SUCCESS = 0, NCS_QUEUE_NODE_CREATE_FAILED, NCS_FILE_OPEN_FAILED, NCS_FILE_LIMIT_REACHED,
            NCS_FILE_SIZE_LIMIT_REACHED, NCS_FILE_NO_MEMORY, NCS_CLIENT_LIMIT_REACHED, NCS_DUPLICATE_OPEN,
            NCS_PACKET_REQUEST_NYI, NCS_PACKET_TYPE_ILLEGAL, NCS_DESTROY_CLIENT_DANGLING_REQUESTS, NCS_UNKNOWN_CLIENT_UID,
            NCS_COULDNT_CREATE_CLIENT, NCS_NET_COULDNT_RESOLVE_HOST, NCS_NET_COULDNT_CONNECT, NCS_NET_RECV_TIMEOUT,
            NCS_NET_HEADER_SEND_FAILURE, NCS_NET_HEADER_RECV_FAILURE, NCS_NET_PACKET_SEND_FAILURE, NCS_NET_PACKET_RECV_FAILURE,
            NCS_NET_401_UNAUTHORISED, NCS_NET_403_FORBIDDEN, NCS_NET_404_NOT_FOUND, NCS_NET_407_PROXYAUTH,
            NCS_NET_UNEXPECTED_RESPONSE, NCS_NET_BAD_RESPONSE, NCS_NET_ALREADY_CONNECTED, NCS_INVALID_CONNECTION,
            NCS_WINSOCK_FAILURE, NCS_SYMBOL_ERROR, NCS_OPEN_DB_ERROR, NCS_DB_QUERY_FAILED,
            NCS_DB_SQL_ERROR, NCS_GET_LAYER_FAILED, NCS_DB_NOT_OPEN, NCS_QT_TYPE_UNSUPPORTED,
            NCS_PREF_INVALID_USER_KEY, NCS_PREF_INVALID_MACHINE_KEY, NCS_REGKEY_OPENEX_FAILED, NCS_REGQUERY_VALUE_FAILED,
            NCS_INVALID_REG_TYPE, NCS_INVALID_ARGUMENTS, NCS_ECW_ERROR, NCS_SERVER_ERROR,
            NCS_UNKNOWN_ERROR, NCS_EXTENT_ERROR, NCS_COULDNT_ALLOC_MEMORY, NCS_INVALID_PARAMETER,
            NCS_FILEIO_ERROR, NCS_COULDNT_OPEN_COMPRESSION, NCS_COULDNT_PERFORM_COMPRESSION, NCS_GENERATED_TOO_MANY_OUTPUT_LINES,
            NCS_USER_CANCELLED_COMPRESSION, NCS_COULDNT_READ_INPUT_LINE, NCS_INPUT_SIZE_EXCEEDED, NCS_MISMATCH_PARAMETER_COMPRESSION,
            NCS_REGION_OUTSIDE_FILE, NCS_NO_SUPERSAMPLE, NCS_ZERO_SIZE, NCS_TOO_MANY_BANDS,
            NCS_INVALID_BAND_NR, NCS_INPUT_SIZE_TOO_SMALL, NCS_INCOMPATIBLE_PROTOCOL_VERSION, NCS_WININET_FAILURE,
            NCS_COULDNT_LOAD_WININET, NCS_FILE_INVALID_SETVIEW, NCS_FILE_NOT_OPEN, NCS_JNI_REFRESH_NOT_IMPLEMENTED,
            NCS_INCOMPATIBLE_COORDINATE_SYSTEMS, NCS_INCOMPATIBLE_COORDINATE_DATUM, NCS_INCOMPATIBLE_COORDINATE_PROJECTION, NCS_INCOMPATIBLE_COORDINATE_UNITS,
            NCS_COORDINATE_CANNOT_BE_TRANSFORMED, NCS_GDT_ERROR, NCS_NET_PACKET_RECV_ZERO_LENGTH, NCS_UNSUPPORTEDLANGUAGE,
            NCS_CONNECTION_LOST, NCS_COORD_CONVERT_ERROR, NCS_METABASE_OPEN_FAILED, NCS_METABASE_GET_FAILED,
            NCS_NET_HEADER_SEND_TIMEOUT, NCS_JNI_ERROR, NCS_DB_INVALID_NAME, NCS_SYMBOL_COULDNT_RESOLVE_HOST,
            NCS_INVALID_ERROR_ENUM, NCS_FILE_EOF, NCS_FILE_NOT_FOUND, NCS_FILE_INVALID,
            NCS_FILE_SEEK_ERROR, NCS_FILE_NO_PERMISSIONS, NCS_FILE_OPEN_ERROR, NCS_FILE_CLOSE_ERROR,
            NCS_FILE_IO_ERROR, NCS_SET_EXTENTS_ERROR, NCS_FILE_PROJECTION_MISMATCH, NCS_GDT_UNKNOWN_PROJECTION,
            NCS_GDT_UNKNOWN_DATUM, NCS_GDT_USER_SERVER_FAILED, NCS_GDT_REMOTE_PATH_DISABLED, NCS_GDT_BAD_TRANSFORM_MODE,
            NCS_GDT_TRANSFORM_OUT_OF_BOUNDS, NCS_LAYER_DUPLICATE_LAYER_NAME, NCS_LAYER_INVALID_PARAMETER, NCS_PIPE_CREATE_FAILED,
            NCS_FILE_MKDIR_EXISTS, NCS_FILE_MKDIR_PATH_NOT_FOUND, NCS_ECW_READ_CANCELLED, NCS_JP2_GEODATA_READ_ERROR,
            NCS_JP2_GEODATA_WRITE_ERROR, NCS_JP2_GEODATA_NOT_GEOREFERENCED, NCS_PROGRESSIVE_VIEW_TOO_LARGE, NCS_GDT_SET_DATUM_SHIFTS_ERROR,
            NCS_GDT_GET_DATUM_SHIFTS_ERROR, NCS_PROJECTION_STRING_TOO_LONG, NCS_OTDF_FILE_VERSION_NOT_SUPPORTED, NCS_ECWP_POLLING,
            NCS_INSUFFICIENT_PRIVILEGE, NCS_INSUFFICENT_FILESYSTEM_SPACE, NCS_INVALID_WMS_SERVICE, NCS_NON_MATCHED_TEMPORAL_EXTENT,
            NCS_UNSUPPORTED_FILE_TYPE_OR_VERSION, NCS_NO_VALUE, NCS_MAX_ERROR_NUMBER
        } 
        #endregion
        private static NCSFileInfo ReadFileInfo(string path)
        {
            IntPtr pNCSFileView = IntPtr.Zero;
            NCSError error = NCSOpenFileView(path, out pNCSFileView, IntPtr.Zero);

            IntPtr pFileInfo = IntPtr.Zero;
            NCSError infoerror = NCSGetViewFileInfo(pNCSFileView, out pFileInfo);
            return Marshal.PtrToStructure<NCSFileInfo>(pFileInfo);
        }
        private static MemoryStream ReadFile(string path,NCSFileInfo fileInfo, List<Color> noDataColors, int scale)
        {
            SKBitmap sKBitmap = new();
            try
            {
                NCSInit();

                int major, minor = 0;
                NCSGetLibVersion(out major, out minor);
                //Trace.WriteLine($"ECWJP2 SDK v{major}.{minor}");
                IntPtr pNCSFileView = IntPtr.Zero;
                NCSError error = NCSOpenFileView(path, out pNCSFileView, IntPtr.Zero);

                uint[] bandList;
                if (fileInfo.nBands == 3) bandList = [0, 1, 2];
                else if (fileInfo.nBands == 4) bandList = [0, 1, 2, 3];
                else throw new DataMisalignedException();

                int tileWidth = (int)(fileInfo.nSizeX / scale);
                int tileHeight = (int)(fileInfo.nSizeY / scale);

                NCSError result = NCSSetFileView(pNCSFileView,
                    fileInfo.nBands,
                    bandList,
                    (uint)0,
                    (uint)0,
                    (uint)fileInfo.nSizeX - 1,
                    (uint)fileInfo.nSizeY - 1,
                    (uint)tileWidth,
                    (uint)tileHeight);

                var bitmap = new Bitmap(tileWidth, tileHeight);

                var bmdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                try
                {
                    //Trace.WriteLine($"Stride: {bmdata.Stride}");
                    var ptrdest = (IntPtr)bmdata.Scan0; // bmdata.ImageData;
                    for (int i = 0; i < tileHeight; i++)
                    {
                        result = NCSReadViewLineBGRA(pNCSFileView, ptrdest);
                        ptrdest += bmdata.Stride;
                    }
                }
                finally
                {
                    bitmap.UnlockBits(bmdata);
                }
                sKBitmap = bitmap.ToSKBitmap();
                if (noDataColors != null)
                {
                    SKBitmap sKBitmap2 = ApplyColorFilter(sKBitmap, noDataColors);
                    sKBitmap.Dispose();
                    sKBitmap = sKBitmap2;
                }
                MemoryStream memoryStream = new();
                sKBitmap.Encode(memoryStream, SKEncodedImageFormat.Png, 100);
                return memoryStream;
            }
            finally
            {
                sKBitmap.Dispose();
                NCSShutdown();
            }
        }
        private static MRect CalculateExtent(NCSFileInfo info, WorldProperties worldProperties)
        {
            double num = worldProperties.XCenterOfUpperLeftPixel - worldProperties.PixelSizeX * 0.5;
            double maxX = num + worldProperties.PixelSizeX * info.nSizeX + worldProperties.PixelSizeX * 0.5;
            double num2 = worldProperties.YCenterOfUpperLeftPixel + worldProperties.PixelSizeY * 0.5;
            double minY = num2 + worldProperties.PixelSizeY * info.nSizeY - worldProperties.PixelSizeY * 0.5;
            return new MRect(num, minY, maxX, num2);
        }
        private static WorldProperties LoadWorld(string location)
        {
            using TextReader textReader = File.OpenText(location);
            WorldProperties result = default(WorldProperties);
            result.PixelSizeX = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            result.RotationAroundYAxis = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            result.RotationAroundXAxis = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            result.PixelSizeY = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            result.XCenterOfUpperLeftPixel = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            result.YCenterOfUpperLeftPixel = Convert.ToDouble(textReader.ReadLine()?.Replace(',', '.'), CultureInfo.InvariantCulture);
            return result;
        }
        public MRect? GetExtent()
        {
            return _extent;
        }
        public Task<IEnumerable<IFeature>> GetFeaturesAsync(FetchInfo fetchInfo)
        {
            if (_extent.Intersects(fetchInfo.Extent))
            {
                return Task.FromResult((IEnumerable<IFeature>)new IFeature[1] { _feature });
            }

            return Task.FromResult(Enumerable.Empty<IFeature>());
        }
        private static SKBitmap ApplyColorFilter(SKBitmap bitmapImage, ICollection<Color> colors)
        {
            return ApplyAlphaOnNonIndexedBitmap(bitmapImage, colors);
        }
        private static SKBitmap ApplyAlphaOnNonIndexedBitmap(SKBitmap bitmapImage, IEnumerable<Color> colors)
        {
            SKColor[] pixels = bitmapImage.Pixels;
            List<SKColor> list = new List<SKColor>();
            foreach (Color color in colors)
            {
                list.Add(new SKColor((byte)color.R, (byte)color.B, (byte)color.B, (byte)color.A));
            }

            int counter;
            for (counter = 0; counter < pixels.Length; counter++)
            {
                if (pixels[counter].Alpha != 0 && list.Any((SKColor f) => f == pixels[counter]))
                {
                    SKColor sKColor = pixels[counter];
                    pixels[counter] = new SKColor(sKColor.Red, sKColor.Green, sKColor.Blue, 0);
                }
            }

            return new SKBitmap(bitmapImage.Info)
            {
                Pixels = pixels
            };
        }
        public bool? IsCrsSupported(string? crs)
        {
            return string.Equals(crs?.Trim(), CRS?.Trim(), StringComparison.CurrentCultureIgnoreCase);
        }
        public void Dispose()
        {
            _feature?.Dispose();
        }
        private static string GetPathWithoutExtension(string path)
        {
            return Path.GetDirectoryName(path) + Path.DirectorySeparatorChar + Path.GetFileNameWithoutExtension(path);
        }
    }
#pragma warning restore CS8632 // The annotation for nullable reference types should only be used in code within a '#nullable' annotations context.
}