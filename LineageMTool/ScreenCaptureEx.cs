using SlimDX.Direct2D;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;


namespace LineageMTool
{
    namespace Spazzarama.ScreenCapture
    {
        public static class Direct3DCapture
        {
            private static SlimDX.Direct3D9.Direct3D _direct3D9 = new SlimDX.Direct3D9.Direct3D();
            private static Dictionary<IntPtr, SlimDX.Direct3D9.Device> _direct3DDeviceCache = new Dictionary<IntPtr, SlimDX.Direct3D9.Device>();
 
        /// &lt;summary&gt;
        /// Capture the entire client area of a window
        /// &lt;/summary&gt;
        /// &lt;param name=&quot;hWnd&quot;&gt;&lt;/param&gt;
        /// &lt;returns&gt;&lt;/returns&gt;
        public static System.Drawing.Bitmap CaptureWindow(IntPtr hWnd)
            {
                return CaptureRegionDirect3D(hWnd, NativeMethods.GetAbsoluteClientRect(hWnd));
            }

            /// &lt;summary&gt;
            /// Capture a region of the screen using Direct3D
            /// &lt;/summary&gt;
            /// &lt;param name=&quot;handle&quot;&gt;The handle of a window&lt;/param&gt;
            /// &lt;param name=&quot;region&quot;&gt;The region to capture (in screen coordinates)&lt;/param&gt;
            /// &lt;returns&gt;A bitmap containing the captured region, this should be disposed of appropriately when finished with it&lt;/returns&gt;
        public static System.Drawing.Bitmap CaptureRegionDirect3D(IntPtr handle, Rectangle region)
            {
                IntPtr hWnd = handle;
                System.Drawing.Bitmap bitmap = null;

                // We are only supporting the primary display adapter for Direct3D mode
                SlimDX.Direct3D9.AdapterInformation adapterInfo = _direct3D9.Adapters.DefaultAdapter;
                SlimDX.Direct3D9.Device device;

                #region Get Direct3D Device
                // Retrieve the existing Direct3D device if we already created one for the given handle
                if (_direct3DDeviceCache.ContainsKey(hWnd))
                {
                    device = _direct3DDeviceCache[hWnd];
                }
                // We need to create a new device
                else
                {
                    // Setup the device creation parameters
                    SlimDX.Direct3D9.PresentParameters parameters = new SlimDX.Direct3D9.PresentParameters();
                    parameters.BackBufferFormat = adapterInfo.CurrentDisplayMode.Format;
                    Rectangle clientRect = NativeMethods.GetAbsoluteClientRect(hWnd);
                    parameters.BackBufferHeight = clientRect.Height;
                    parameters.BackBufferWidth = clientRect.Width;
                    parameters.Multisample = SlimDX.Direct3D9.MultisampleType.None;
                    parameters.SwapEffect = SlimDX.Direct3D9.SwapEffect.Discard;
                    parameters.DeviceWindowHandle = hWnd;
                    parameters.PresentationInterval = SlimDX.Direct3D9.PresentInterval.Default;
                    parameters.FullScreenRefreshRateInHertz = 0;

                    // Create the Direct3D device
                    device = new SlimDX.Direct3D9.Device(_direct3D9, adapterInfo.Adapter, SlimDX.Direct3D9.DeviceType.Hardware, hWnd, SlimDX.Direct3D9.CreateFlags.SoftwareVertexProcessing, parameters);
                    _direct3DDeviceCache.Add(hWnd, device);
                }
                #endregion

                // Capture the screen and copy the region into a Bitmap
                using (SlimDX.Direct3D9.Surface surface = SlimDX.Direct3D9.Surface.CreateOffscreenPlain(device, adapterInfo.CurrentDisplayMode.Width, adapterInfo.CurrentDisplayMode.Height, SlimDX.Direct3D9.Format.A8R8G8B8, SlimDX.Direct3D9.Pool.SystemMemory))
                {
                    device.GetFrontBufferData(0, surface);

                    // Update: thanks digitalutopia1 for pointing out that SlimDX have fixed a bug
                    // where they previously expected a RECT type structure for their Rectangle
                    bitmap = new System.Drawing.Bitmap(SlimDX.Direct3D9.Surface.ToStream(surface, SlimDX.Direct3D9.ImageFileFormat.Bmp, new Rectangle(region.Left, region.Top, region.Width, region.Height)));
                    // Previous SlimDX bug workaround: new Rectangle(region.Left, region.Top, region.Right, region.Bottom)));

                }

                return bitmap;
            }
        }

        #region Native Win32 Interop
        /// &lt;summary&gt;
        /// The RECT structure defines the coordinates of the upper-left and lower-right corners of a rectangle.
        /// &lt;/summary&gt;
        [Serializable, StructLayout(LayoutKind.Sequential)]
        internal struct RECT
        {
            public int Left;
            public int Top;
            public int Right;
            public int Bottom;

            public RECT(int left, int top, int right, int bottom)
            {
                this.Left = left;
                this.Top = top;
                this.Right = right;
                this.Bottom = bottom;
            }

            public Rectangle AsRectangle
            {
                get
                {
                    return new Rectangle(this.Left, this.Top, this.Right - this.Left, this.Bottom - this.Top);
                }
            }

            public static RECT FromXYWH(int x, int y, int width, int height)
            {
                return new RECT(x, y, x + width, y + height);
            }

            public static RECT FromRectangle(Rectangle rect)
            {
                return new RECT(rect.Left, rect.Top, rect.Right, rect.Bottom);
            }
        }

        [System.Security.SuppressUnmanagedCodeSecurity()]
        internal sealed class NativeMethods
        {
            [DllImport("user32.dll")]
            internal static extern bool GetClientRect(IntPtr hWnd, out RECT lpRect);

            [DllImport("user32.dll")]
            [return: MarshalAs(UnmanagedType.Bool)]
            internal static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

            /// &lt;summary&gt;
            /// Get a windows client rectangle in a .NET structure
            /// &lt;/summary&gt;
            /// &lt;param name=&quot;hwnd&quot;&gt;The window handle to look up&lt;/param&gt;
            /// &lt;returns&gt;The rectangle&lt;/returns&gt;
            internal static Rectangle GetClientRect(IntPtr hwnd)
            {
                RECT rect = new RECT();
                GetClientRect(hwnd, out rect);
                return rect.AsRectangle;
            }

            /// &lt;summary&gt;
            /// Get a windows rectangle in a .NET structure
            /// &lt;/summary&gt;
            /// &lt;param name=&quot;hwnd&quot;&gt;The window handle to look up&lt;/param&gt;
            /// &lt;returns&gt;The rectangle&lt;/returns&gt;
            internal static Rectangle GetWindowRect(IntPtr hwnd)
            {
                RECT rect = new RECT();
                GetWindowRect(hwnd, out rect);
                return rect.AsRectangle;
            }

            internal static Rectangle GetAbsoluteClientRect(IntPtr hWnd)
            {
                Rectangle windowRect = NativeMethods.GetWindowRect(hWnd);
                Rectangle clientRect = NativeMethods.GetClientRect(hWnd);

                // This gives us the width of the left, right and bottom chrome - we can then determine the top height
                int chromeWidth = (int)((windowRect.Width - clientRect.Width) / 2);

                return new Rectangle(new Point(windowRect.X + chromeWidth, windowRect.Y + (windowRect.Height - clientRect.Height - chromeWidth)), clientRect.Size);
            }
        }
        #endregion
    }
}
