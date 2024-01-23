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
    public class EcwProvider : IProvider, IDisposable
    {
        private readonly double MAXRESULOTION = 0.2;
        private struct WorldProperties
        {
            public double PixelSizeX;

            public double RotationAroundYAxis;

            public double RotationAroundXAxis;

            public double PixelSizeY;

            public double XCenterOfUpperLeftPixel;

            public double YCenterOfUpperLeftPixel;
        }
        private struct EcwProperties
        {
            public double Width;

            public double Height;

            public double HResolution;

            public double VResolution;
        }

        private const string WorldExtension = "eww";

        private readonly IFeature _feature;

        private readonly MRect _extent;

        private MRaster _mRaster;
        public string CRS { get; set; } = "";
        
        //private EcwInfo _Info;
        public EcwProvider(string ecwpath, List<Mapsui.Styles.Color> colorTrans)
        {
            if (!File.Exists(ecwpath))
            {
                throw new ArgumentException($"ECW file expected at {ecwpath}");
            }
            string ewwpath = $"{GetPathWithoutExtension(ecwpath)}.{WorldExtension}";
            if (!File.Exists(ewwpath))
            {
                throw new ArgumentException($"World file expected at {ewwpath}");
            }
            EcwInfo _Info = new(ecwpath);
            WorldProperties worldProperties = LoadWorld(ewwpath);
            int scale = (int)(MAXRESULOTION / _Info.fCellIncrementX);
            using MemoryStream memoryStream = ReadFile(ecwpath,_Info,colorTrans,scale);
            _extent = CalculateExtent(_Info, worldProperties);
            _mRaster = new MRaster(memoryStream.ToArray(), _extent);
            _feature = new RasterFeature(_mRaster);
            _feature.Styles.Add(new RasterStyle());
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
        
        //public enum NCSError
        //{

        //}
        //internal struct NCSFileInfo { }
        //struct NCSFileInfo
        //{ }
        //public struct NCSFileInfo
        //{
        //    UInt32 nSizeX;
        //    UInt32 nSizeY;
        //    UInt16 nBands;
        //    UInt16 nCompressionRate;
        //    NCSCellSizeUnitType eCellSizeUnits;
        //    Double fCellIncrementX;
        //    Double fCellIncrementY;
        //    Double fOriginX;
        //    Double fOriginY;
        //    unsafe char* szDatum;
        //    unsafe char* szProjection;
        //    Double fCWRotationDegrees;
        //    NCSFileColorSpace eColorSpace;
        //    NCSCellType eCellType;
        //    NCSFileBandInfo pBands;
        //    Byte nFormatVersion;
        //    Byte nCellBitDepth;
        //    unsafe char* sCompressionDate;
        //    float fActualCompressionRate;
        //    unsafe char* pFileMetaData;
        //};
        enum NCSCellSizeUnitType
        {
            ECW_CELL_UNITS_INVALID = 0,
            ECW_CELL_UNITS_METERS = 1,
            ECW_CELL_UNITS_DEGREES = 2,
            ECW_CELL_UNITS_FEET = 3,
            ECW_CELL_UNITS_UNKNOWN = 4
        };
        //enum NCSFileColorSpace
        //{
        //    NCSCS_NONE = 0,
        //    NCSCS_GREYSCALE = 1,    // Greyscale
        //    NCSCS_YUV = 2,    // YUV - JPEG Digital, JP2 ICT
        //    NCSCS_MULTIBAND = 3,    // Multi-band imagery
        //    NCSCS_sRGB = 4,    // sRGB
        //    NCSCS_YCbCr = 5             // YCbCr - JP2 ONLY, Auto-converted to sRGB
        //};
        //enum NCSCellType
        //{
        //    NCSCT_UINT8 = 0,
        //    NCSCT_UINT16 = 1,
        //    NCSCT_UINT32 = 2,
        //    NCSCT_UINT64 = 3,
        //    NCSCT_INT8 = 4,
        //    NCSCT_INT16 = 5,
        //    NCSCT_INT32 = 6,
        //    NCSCT_INT64 = 7,
        //    NCSCT_IEEE4 = 8,
        //    NCSCT_IEEE8 = 9,
        //    NCSCT_NUMVALUES = 10
        //};
        //struct NCSFileBandInfo
        //{
        //    byte nBits;
        //    bool bSigned;
        //    unsafe char* szDesc;
        //};

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern void NCSGetLibVersion(out int major, out int minor);

        //[DllImport(@"./NCSEcw.dll", CharSet = CharSet.Auto)]
        //public static extern NCSError NCSOpenFileView(string fileName, out IntPtr pNCSFileView, IntPtr callBack);

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern NCSError NCSCloseFileViewEx(IntPtr pNCSFileView, bool clearCache);

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern NCSError NCSGetViewFileInfo(IntPtr pNCSFileView, out IntPtr ppNCSFileViewFileInfoPointer);

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern NCSError NCSSetFileView(IntPtr pNCSFileView, uint nBands, [In, Out] uint[] pBandList, uint nTLX, uint nTLY, uint nBRX, uint nBRY, uint nSizeX, uint nSizeY);

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern NCSError NCSReadViewLineBGR(IntPtr pNCSFileView, IntPtr pBGRTriplets);

        //[DllImport(@"./NCSEcw.dll")]
        //public static extern NCSError NCSReadViewLineBGRA(IntPtr pNCSFileView, IntPtr pBGRTriplets);

        //[DllImport(@"./NCSEcw.dll", CharSet = CharSet.Auto)]
        //public static extern NCSError NCSGetEPSGCode(string szDatum, string szProjection, out int epsg); 
        #endregion
        public class EcwInfo
        {
            public int nSizeX { get; private set; }
            public int nSizeY { get; private set; }
            public double fCellIncrementX { get; private set; }
            public double fCellIncrementY { get; private set; }
            public double fOriginX { get; private set; }
            public double fOriginY { get; private set; }
            public double fCWRotationDegrees { get; private set; }

            public EcwInfo(string path)
            {
                IntPtr pNCSFileView = IntPtr.Zero;
                NCSError error = NCSOpenFileView(path, out pNCSFileView, IntPtr.Zero);

                IntPtr pFileInfo = IntPtr.Zero;
                NCSError infoerror = NCSGetViewFileInfo(pNCSFileView, out pFileInfo);

                using (FileStream stream = File.OpenRead(path))
                {
                    IntPtr offset = pFileInfo;
                    nSizeX = Marshal.ReadInt32(offset);
                    offset += sizeof(int);
                    nSizeY = Marshal.ReadInt32(offset);
                    offset += sizeof(int) + sizeof(UInt16) + sizeof(UInt16) + sizeof(NCSCellSizeUnitType);
                    double[] buffer = [0.0];
                    Marshal.Copy(offset, buffer, 0, 1);
                    fCellIncrementX = buffer[0];
                    offset += sizeof(double);
                    Marshal.Copy(offset, buffer, 0, 1);
                    fCellIncrementY = buffer[0];
                    offset += sizeof(double);
                    Marshal.Copy(offset, buffer, 0, 1);
                    fOriginX = buffer[0];
                    offset += sizeof(double);
                    Marshal.Copy(offset, buffer, 0, 1);
                    fOriginY = buffer[0];
                    offset += sizeof(double) + sizeof(char) + sizeof(char);
                    Marshal.Copy(offset, buffer, 0, 1);
                    fCWRotationDegrees = buffer[0];
                }
            }
        }
        private static MemoryStream ReadFile(string path,EcwInfo info,List<Color> noDataColors,int scale)
        {
            SKBitmap sKBitmap = new();
            try
            {
                NCSInit();

                int major, minor = 0;
                NCSGetLibVersion(out major, out minor);
                Trace.WriteLine($"ECWJP2 SDK v{major}.{minor}");
                IntPtr pNCSFileView = IntPtr.Zero;
                NCSError error = NCSOpenFileView(path, out pNCSFileView, IntPtr.Zero);

                //IntPtr pFileInfo = IntPtr.Zero;
                //NCSError infoerror = NCSGetViewFileInfo(pNCSFileView, out pFileInfo);
                //info = new(path, pFileInfo);

                uint[] bandList = [0, 1, 2];

                // nTLX=2160, nTLY=0, nBRX=4319, nBRY=1079, nSizeX=512, nSizeY=256

                int tileWidth = info.nSizeX/scale;
                int tileHeight = info.nSizeY/scale;

                NCSError result = NCSSetFileView(pNCSFileView,
                    3,
                    bandList,
                    (uint)0,
                    (uint)0,
                    (uint)info.nSizeX - 1,
                    (uint)info.nSizeY - 1,
                    (uint)(tileWidth),
                    (uint)(tileHeight));

                var bitmap = new Bitmap(tileWidth, tileHeight);

                var bmdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
                try
                {
                    Trace.WriteLine($"Stride: {bmdata.Stride}");
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
                //bitmap.Save("op.jpg", ImageFormat.Jpeg);
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
        private static MRect CalculateExtent(EcwInfo info, WorldProperties worldProperties)
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
}

//{
//        //internal const string DllPath = "NCSEcw.dll";
//        internal const string DllPath = "libNCSEcw.dylib";

//[DllImport(DllPath)]
//public static extern void NCSInit();

//[DllImport(DllPath)]
//public static extern void NCSShutdown();

//[DllImport(DllPath)]
//public static extern void NCSGetLibVersion(out int major, out int minor);
//[DllImport(DllPath, CharSet = CharSet.Auto)]
//public static extern NCSError NCSOpenFileView(string fileName, out IntPtr pNCSFileView, IntPtr callBack);
//[DllImport(DllPath)]
//public static extern NCSError NCSCloseFileViewEx(IntPtr pNCSFileView, bool clearCache);
//[DllImport(DllPath)]
//public static extern NCSError NCSGetViewFileInfo(IntPtr pNCSFileView, out IntPtr ppNCSFileViewFileInfoPointer);
////// https://docs.microsoft.com/de-de/dotnet/framework/interop/marshaling-different-types-of-arrays
//[DllImport(DllPath)]
//public static extern NCSError NCSSetFileView(IntPtr pNCSFileView, uint nBands, [In, Out] uint[] pBandList, uint nTLX, uint nTLY, uint nBRX, uint nBRY, uint nSizeX, uint nSizeY);
//[DllImport(DllPath)]
//public static extern NCSError NCSReadViewLineBGR(IntPtr pNCSFileView, IntPtr pBGRTriplets);
//// extern NCSReadStatus NCS_CALL NCSReadViewLineBGRA( NCSFileView *pNCSFileView, UINT32 *pBGRA);
//[DllImport(DllPath)]
//public static extern NCSError NCSReadViewLineBGRA(IntPtr pNCSFileView, IntPtr pBGRTriplets);
//[DllImport(DllPath, CharSet = CharSet.Auto)]
//public static extern NCSError NCSGetEPSGCode(string szDatum, string szProjection, out int epsg);

//static void Main(string[] args)
//{
//    try
//    {
//        NCSInit();

//        int major, minor = 0;
//        NCSGetLibVersion(out major, out minor);
//        Console.WriteLine("ECWJP2 SDK v{0}.{1}", major, minor);
//        IntPtr pNCSFileView = IntPtr.Zero;

//        NCSError error = NCSOpenFileView("<path_to_ssk>/testdata/RGB_8bit.ecw", out pNCSFileView, IntPtr.Zero);

//        uint[] bandList = new uint[3] { 0, 1, 2 };

//        // nTLX=2160, nTLY=0, nBRX=4319, nBRY=1079, nSizeX=512, nSizeY=256

//        int tileWidth = 512;
//        int tileHeight = 256;

//        NCSError result = NCSSetFileView(pNCSFileView,
//            3,
//            bandList,
//            (uint)2160,
//            (uint)0,
//            (uint)4319,
//            (uint)1079,
//            (uint)tileWidth,
//            (uint)tileHeight);

//        var bitmap = new Bitmap(tileWidth, tileHeight);

//        var bmdata = bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height), System.Drawing.Imaging.ImageLockMode.ReadWrite, bitmap.PixelFormat);
//        try
//        {
//            Console.WriteLine("Stride: {0}", bmdata.Stride);
//            var ptrdest = (IntPtr)bmdata.Scan0; // bmdata.ImageData;
//            for (int i = 0; i < tileHeight; i++)
//            {
//                result = NCSReadViewLineBGRA(pNCSFileView, ptrdest);
//                ptrdest += bmdata.Stride;
//            }
//        }
//        finally
//        {
//            bitmap.UnlockBits(bmdata);
//        }
//        bitmap.Save("op.jpg", System.Drawing.Imaging.ImageFormat.Jpeg);

//    }
//    finally
//    {
//        NCSShutdown();
//    }
//}
//    }
 
//    internal enum NCSError
//{
//}
//}
