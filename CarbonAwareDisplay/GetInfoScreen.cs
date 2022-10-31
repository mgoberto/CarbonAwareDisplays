using System;
using System.Collections.Generic;
using System.Drawing.Imaging;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;

namespace LightMeasure
{
    internal class GetInfoScreen
    {
        //[DllImport("gdi32.dll", CharSet = CharSet.Auto, SetLastError = true, ExactSpelling = true)]
        //public static extern int BitBlt(IntPtr hDC, int x, int y, int nWidth, int nHeight, IntPtr hSrcDC, int xSrc, int ySrc, int dwRop);
        const int ENUM_CURRENT_SETTINGS = -1;

        public static Rectangle GetRealRectangle(Screen screen)
        {
            DEVMODE dm = new DEVMODE();
            dm.dmSize = (short)Marshal.SizeOf(typeof(DEVMODE));
            EnumDisplaySettings(screen.DeviceName, ENUM_CURRENT_SETTINGS, ref dm);
            return new Rectangle(dm.dmPositionX, dm.dmPositionY, dm.dmPelsWidth, dm.dmPelsHeight);
        }

        [DllImport("user32.dll")]
        public static extern bool EnumDisplaySettings(string lpszDeviceName, int iModeNum, ref DEVMODE lpDevMode);


        [StructLayout(LayoutKind.Sequential)]
        public struct DEVMODE
        {
            private const int CCHDEVICENAME = 0x20;
            private const int CCHFORMNAME = 0x20;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmDeviceName;
            public short dmSpecVersion;
            public short dmDriverVersion;
            public short dmSize;
            public short dmDriverExtra;
            public int dmFields;
            public int dmPositionX;
            public int dmPositionY;
            public ScreenOrientation dmDisplayOrientation;
            public int dmDisplayFixedOutput;
            public short dmColor;
            public short dmDuplex;
            public short dmYResolution;
            public short dmTTOption;
            public short dmCollate;
            [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x20)]
            public string dmFormName;
            public short dmLogPixels;
            public int dmBitsPerPel;
            public int dmPelsWidth;
            public int dmPelsHeight;
            public int dmDisplayFlags;
            public int dmDisplayFrequency;
            public int dmICMMethod;
            public int dmICMIntent;
            public int dmMediaType;
            public int dmDitherType;
            public int dmReserved1;
            public int dmReserved2;
            public int dmPanningWidth;
            public int dmPanningHeight;
        }

        //public static Color GetColorAt(Point location)
        //{
        //    Bitmap screenPixel = new Bitmap(1, 1, PixelFormat.Format32bppArgb);
        //    using (Graphics gdest = Graphics.FromImage(screenPixel))
        //    {
        //        using (Graphics gsrc = Graphics.FromHwnd(IntPtr.Zero))
        //        {
        //            IntPtr hSrcDC = gsrc.GetHdc();
        //            IntPtr hDC = gdest.GetHdc();
        //            int retval = BitBlt(hDC, 0, 0, 1, 1, hSrcDC, location.X, location.Y, (int)CopyPixelOperation.SourceCopy);
        //            gdest.ReleaseHdc();
        //            gsrc.ReleaseHdc();
        //        }
        //    }
        //    return screenPixel.GetPixel(0, 0);
        //}

        public static Color GetDominantColor(Bitmap bmp)
        {

            //Used for tally
            int r = 0;
            int g = 0;
            int b = 0;

            int total = 0;

            for (int x = 0; x < bmp.Width; x++)
            {
                for (int y = 0; y < bmp.Height; y++)
                {
                    Color clr = bmp.GetPixel(x, y);

                    r += clr.R;
                    g += clr.G;
                    b += clr.B;

                    total++;
                }
            }

            //Calculate average
            r /= total;
            g /= total;
            b /= total;

            return Color.FromArgb(r, g, b);
        }
        public static void GetColorScreen(Screen sc, out Rectangle rect, out uint dpix, out uint dpiy, out Color csc)
        {
            rect = GetInfoScreen.GetRealRectangle(sc);
            int x = rect.X;
            int y = rect.Y;
            int w = rect.Width;
            int h = rect.Height;
            dpix = 0;
            dpiy = 0;

            // Get Screen Shot
            csc = new Color();
            using (Bitmap bmpScreenshot = new Bitmap(w, h))
            {
                using (var gfxScreenshot = Graphics.FromImage(bmpScreenshot))
                {
                    gfxScreenshot.CopyFromScreen(x, y, 0, 0, new Size(w, h));
                    csc = GetInfoScreen.GetDominantColor(bmpScreenshot);
                }
            }
            sc.GetDpi(DpiType.Effective, out dpix, out dpiy);
        }

    }

    public static class ScreenExtensions
    {
        public static void GetDpi(this System.Windows.Forms.Screen screen, DpiType dpiType, out uint dpiX, out uint dpiY)
        {
            var pnt = new System.Drawing.Point(screen.Bounds.Left + 1, screen.Bounds.Top + 1);
            var mon = MonitorFromPoint(pnt, 2/*MONITOR_DEFAULTTONEAREST*/);
            GetDpiForMonitor(mon, dpiType, out dpiX, out dpiY);
        }

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dd145062(v=vs.85).aspx
        [DllImport("User32.dll")]
        private static extern IntPtr MonitorFromPoint([In] System.Drawing.Point pt, [In] uint dwFlags);

        //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280510(v=vs.85).aspx
        [DllImport("Shcore.dll")]
        private static extern IntPtr GetDpiForMonitor([In] IntPtr hmonitor, [In] DpiType dpiType, [Out] out uint dpiX, [Out] out uint dpiY);
    }

    //https://msdn.microsoft.com/en-us/library/windows/desktop/dn280511(v=vs.85).aspx
    public enum DpiType
    {
        Effective = 0,
        Angular = 1,
        Raw = 2,
    }
}
