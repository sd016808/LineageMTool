using LineageMTool.Spazzarama.ScreenCapture;
using ScreenShotDemo;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LineageMTool
{
    public enum CaptureMode
    {
        DirectX,
        Gdi,
        DAMO
    }
    public class SimulatorInfo
    {
        static public string HotKeyList = "鍵盤7,鍵盤8,鍵盤9,鍵盤0,鍵盤U,鍵盤I,鍵盤O,鍵盤P";
        private uint WM_KEYDOWN = 0x0100;
        private uint WM_KEYUP = 0x0101;
        private uint WM_LBUTTONDOWN = 0x0201;
        private uint WM_LBUTTONUP = 0x0202;
        public string Name { get { return _config.SimulatorName; } }
        private Config _config;
        public bool IsSimulatorOpen()
        {
            IntPtr hwndCalc = WinApi.FindWindow(null, Name);
            return hwndCalc != IntPtr.Zero;
        }
        public SimulatorInfo(Config config)
        {
            _config = config;
        }
        public Image GetImage(CaptureMode captureMode)
        {
            // 抓取雷電模擬器的Handle
            IntPtr handle = WinApi.FindWindow(null, Name);
            handle = WinApi.FindWindowEx(handle, 0, null, null);
            Image image = null;
            switch (captureMode)
            {
                case CaptureMode.DirectX:
                    //dmsoft dm = new dmsoft();
                    //dm.cap
                    image = Direct3DCapture.CaptureWindow(handle);
                    break;
                case CaptureMode.Gdi:
                    ScreenCapture sc = new ScreenCapture();
                    image = sc.CaptureWindow(handle);
                    break;
                default:
                    throw new NotImplementedException("尚未實作的擷取畫面模式");
            }
            return image;
        }
        public void SendMessage(string action)
        {
            // 抓取雷電模擬器的Handle
            IntPtr hwndCalc = WinApi.FindWindow(null, Name);
            string[] hotkeyList = HotKeyList.Split(',');
            IntPtr test = IntPtr.Zero;
            test = WinApi.FindWindowEx(hwndCalc, 0, null, null);

            if (action == hotkeyList[0])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D7), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D7), 0);
            }
            else if (action == hotkeyList[1])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D8), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D8), 0);
            }
            else if (action == hotkeyList[2])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D9), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D9), 0);
            }
            else if (action == hotkeyList[3])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D0), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D0), 0);
            }
            else if (action == hotkeyList[4])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.U), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.U), 0);
            }
            else if (action == hotkeyList[5])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.I), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.I), 0);
            }
            else if (action == hotkeyList[6])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.O), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.O), 0);
            }
            else if (action == hotkeyList[7])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.P), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.P), 0);
            }
        }

        public void SendWindowOnTop()
        {
            // 抓取雷電模擬器的Handle
            IntPtr hwndCalc = WinApi.FindWindow(null, Name);
            string[] hotkeyList = HotKeyList.Split(',');
            IntPtr test = IntPtr.Zero;
            test = WinApi.FindWindowEx(hwndCalc, 0, null, null);


            WinApi.SetForegroundWindow(test);
        }

        public void SendMouseMessage(int x, int y)
        {
            // 抓取雷電模擬器的Handle
            IntPtr hwndCalc = WinApi.FindWindow(null, Name);
            IntPtr test = IntPtr.Zero;
            test = WinApi.FindWindowEx(hwndCalc, 0, null, null);


            WinApi.SendMessage(test, WM_LBUTTONDOWN, 0, x + (y << 16));
            Thread.Sleep(100);
            WinApi.SendMessage(test, WM_LBUTTONUP, 0, x + (y << 16));
            Thread.Sleep(100);
            WinApi.SendMessage(test, WM_LBUTTONDOWN, 0, x + (y << 16));
            Thread.Sleep(100);
            WinApi.SendMessage(test, WM_LBUTTONUP, 0, x + (y << 16));
        }

        public void SendAutoAttackMessage()
        {
            // 抓取雷電模擬器的Handle
            IntPtr hwndCalc = WinApi.FindWindow(null, Name);
            IntPtr test = IntPtr.Zero;
            test = WinApi.FindWindowEx(hwndCalc, 0, null, null);


            WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.K), 0);
            Thread.Sleep(100);
            WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.K), 0);

        }
    }
}
