using Emgu.CV;
using Emgu.CV.CvEnum;
using Emgu.CV.Structure;
using LineageMTool.Properties;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineageMTool
{
    public enum RoleState
    {
        Normal,
        Error,
        Die,
        OutOfArrow,
        BackHome,
        Detoxification
    }

    public enum PlayerNo
    {
        P1,
        P2,
        P3,
        P4
    }

    public class PlayerInfo
    { 
        // 固定解析度下的座標 1280*720
        private Rect GetHpLocation(PlayerNo playerNo)
        {
            switch(playerNo)
            {
                case PlayerNo.P1:
                    return new Rect(72, 177, 254, 255);
                case PlayerNo.P2:
                    return new Rect(80, 192, 349, 350);
                default:
                    return new Rect(80, 192, 280, 281);
            }
        }
        // 固定解析度下的座標 1280*720
        private Rect GetMpLocation(PlayerNo playerNo)
        {
            switch (playerNo)
            {
                case PlayerNo.P1:
                    return new Rect(72, 177, 263, 264);
                case PlayerNo.P2:
                    return new Rect(80, 192, 361, 362);
                default:
                    return new Rect(80, 192, 290, 291);
            }
        }
        private RoleState _state;
        public RoleState State
        {
            get
            {
                return _state;
            }
            set
            {
                _state = value;
                switch(value)
                {
                    case RoleState.Normal:
                    case RoleState.Detoxification:
                        _stopwatchSpacialError.Stop();
                        break;
                    case RoleState.BackHome:
                    case RoleState.Die:
                    case RoleState.Error:
                    case RoleState.OutOfArrow:
                        if (!_stopwatchSpacialError.IsRunning)
                            _stopwatchSpacialError.Start();

                        if (_stopwatchSpacialError.Elapsed.TotalMinutes > 5)
                        {
                            _stopwatchSpacialError.Restart();
                            LineMessage message = new LineMessage();
                            message.SendMessageToLine(_config.Uid, GetRoleStateMessage(), _simulatorInfo.GetImage((CaptureMode)_config.comboBoxCaptureSettingSelectIndex));
                        }
                        break;
                }

            }
        }
        private SimulatorInfo _simulatorInfo;
        private Stopwatch _stopwatchSpacialError = new Stopwatch();
        private Config _config;
        public int Hp { get; set; }
        public int Mp { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public PlayerInfo(Config config, SimulatorInfo simulatorInfo)
        {
            _config = config;
            _simulatorInfo = simulatorInfo;
        }
        public void CalculateHpPercent(Image image)
        {
            List<Color> redPointList = new List<Color>();
            Rect rect = GetHpLocation(_config.PlayerNo);
            int hpleftX = int.Parse(rect.Left);
            int hprightX = int.Parse(rect.Right);
            int hptopY = int.Parse(rect.Top);
            int hpdownY = int.Parse(rect.Down);
            int weight = 0;
            using (Bitmap bmp = new Bitmap(image))
            {

                for (int x = hpleftX; x < hprightX; x++)
                {
                    int RedAverage = 0;
                    int GreenAverage = 0;
                    int BlueAverage = 0;
                    for (int y = hptopY; y < hpdownY; y++)
                    {
                        Color pxl = bmp.GetPixel(x, y);
                        GreenAverage += pxl.G;
                        BlueAverage += pxl.B;
                        RedAverage += pxl.R;
                    }

                    RedAverage /= (hpdownY - hptopY);
                    GreenAverage /= (hpdownY - hptopY);
                    BlueAverage /= (hpdownY - hptopY);
                    if ((RedAverage > 100 && GreenAverage < 50 && BlueAverage < 50))
                    {
                        State = RoleState.Detoxification;
                        redPointList.Add(Color.FromArgb(RedAverage, GreenAverage, BlueAverage));
                        weight--;
                    }

                    else if ((RedAverage < 20 && GreenAverage > 50 && BlueAverage < 20))
                    {
                        
                        redPointList.Add(Color.FromArgb(RedAverage, GreenAverage, BlueAverage));
                        weight++;
                    }
                }
            }
            if (weight < 0)
                State = RoleState.Normal;
            else
                State = RoleState.Detoxification;

            int hpPercent = (int)(redPointList.Count * 100 / (hprightX - hpleftX));
            Hp =  hpPercent;
        }

        internal bool CheckIfGotAttackMessage(Image image)
        {
            Bitmap target = new Bitmap(Resources.attack);
            return TemplateMatching(new Bitmap(image), target, new Rectangle(1000, 420, 1177-1000, 490-420));
        }

        public void Get1PLocation(Image image)
        {
            X = 0;
            Y = 0;
            Bitmap target1P = new Bitmap(Resources._1P);
            TemplateMatching(new Bitmap(image), target1P, new Rectangle(1183, 141, 1369 - 1183, 306 - 141));
        }
        public void CalculateMpPercent(Image image)
        {
            List<Color> bluePointList = new List<Color>();

            Rect rect = GetMpLocation(_config.PlayerNo);
            int mpleftX = int.Parse(rect.Left);
            int mprightX = int.Parse(rect.Right);
            int mptopY = int.Parse(rect.Top);
            int mpdownY = int.Parse(rect.Down);
            using (Bitmap bmp = new Bitmap(image))
            {
                for (int x = mpleftX; x < mprightX; x++)
                {
                    int RedAverage = 0;
                    int GreenAverage = 0;
                    int BlueAverage = 0;
                    for (int y = mptopY; y < mpdownY; y++)
                    {
                        Color pxl = bmp.GetPixel(x, y);
                        RedAverage += pxl.R;
                        GreenAverage += pxl.G;
                        BlueAverage += pxl.B;
                    }
                    RedAverage /= (mpdownY - mptopY);
                    GreenAverage /= (mpdownY - mptopY);
                    BlueAverage /= (mpdownY - mptopY);
                    if (BlueAverage > 100 && RedAverage < 20)
                        bluePointList.Add(Color.FromArgb(RedAverage, GreenAverage, BlueAverage));
                }
            }
            int mpPercent = (int)(bluePointList.Count * 100 / (mprightX - mpleftX));
            Mp =  mpPercent;
        }
        public string GetRoleStateMessage()
        {
            switch (State)
            {
                case RoleState.Error:
                    return "外掛發生錯誤";
                case RoleState.Die:
                    return "角色死亡";
                case RoleState.OutOfArrow:
                    return "箭矢用完，使用回捲回村";
                case RoleState.BackHome:
                    return "使用回捲回村";
                case RoleState.Detoxification:
                    return "角色中毒，嘗試使用解毒藥水";
                default:
                    return "外掛正常運行中";
            }
        }

        private bool TemplateMatching(Bitmap soruce, Bitmap template, Rectangle rect)
        {
            Image<Bgr, byte> Image1 = new Image<Bgr, byte>(soruce); //Your first image
            Image<Bgr, byte> Image2 = new Image<Bgr, byte>(template); //Your second image
            Image1.ROI = rect;
            double Threshold = 0.7; //set it to a decimal value between 0 and 1.00, 1.00 meaning that the images must be identical

            Image<Gray, float> Matches = Image1.MatchTemplate(Image2, TemplateMatchingType.CcoeffNormed);
            double max = 0;
            for (int y = 0; y < Matches.Data.GetLength(0); y++)
            {
                for (int x = 0; x < Matches.Data.GetLength(1); x++)
                {
                    if (Matches.Data[y, x, 0] > max) //Check if its a valid match
                    {
                        max = Matches.Data[y, x, 0];
                        //Image2 found within Image1
                        X = x+ rect.X;
                        Y = y+ rect.Y;
                    }
                }
            }
            if (max < Threshold)
            {
                X = 0;
                Y = 0;
                return false;
            }
            else
                return true;
        }

        #region 暫時沒用到
        private bool IsArrowRunOut(Image image)
        {
            //Bitmap bmp = new Bitmap(image);
            //Rect rect = new Rect();
            //rect.Left = 1045;
            //rect.Right = 1076;
            //rect.Top = 635;
            //rect.Down = 654;

            //for (int x = rect.Left; x <= rect.Right; x++)
            //{
            //    for (int y = rect.Top; y <= rect.Down; y++)
            //    {
            //        Color pxl = bmp.GetPixel(x, y);
            //        if (pxl.R > 100 && pxl.G > 100)
            //            return false;
            //    }
            //}
            return false;
        }
        private Bitmap GetMoneyArea(Image image)
        {
            Bitmap bmp = new Bitmap(699 - 613 + 1, 80 - 60 + 1, PixelFormat.Format24bppRgb);
            Bitmap old = new Bitmap(image);
            for (int x = 613; x <= 699; x++)
            {
                for (int y = 60; y <= 80; y++)
                {
                    Color pxl = old.GetPixel(x, y);
                    bmp.SetPixel(x - 613, y - 60, pxl);
                }
            }
            return bmp;
        }
        private Bitmap TranslateToGrayImage(Bitmap image)
        {
            Bitmap bmp = new Bitmap(image);
            for (int x = 0; x < image.Width; x++)
            {
                for (int y = 0; y < image.Height; y++)
                {
                    Color pxl = bmp.GetPixel(x, y);
                    int gray = GetGrayNumColor(pxl);
                    bmp.SetPixel(x, y, Color.FromArgb(gray, gray, gray));
                }
            }
            return bmp;
        }
        private List<Bitmap> SplitImg(Bitmap image)
        {
            List<Bitmap> bitmapList = new List<Bitmap>();
            List<int> splitXPoint = new List<int>();
            for (int x = 0; x < image.Width; x++)
            {
                int count = 0;
                for (int y = 0; y < image.Height; y++)
                {
                    Color pxl = image.GetPixel(x, y);
                    if (pxl.R > 180)
                        count++;
                }
                splitXPoint.Add(count);
            }

            bool upperFlag = false;
            int startX = 0;
            int endX = 0;
            for (int x = 0; x < image.Width; x++)
            {
                if (upperFlag == false && splitXPoint[x] > 0)
                {
                    upperFlag = true;
                    startX = x;
                }
                if (upperFlag == true && splitXPoint[x] == 0)
                {
                    upperFlag = false;
                    endX = x;
                    Bitmap bitmap = new Bitmap(endX - startX + 1, image.Height, PixelFormat.Format24bppRgb);
                    for (int i = startX; x <= endX; x++)
                    {
                        for (int j = 0; j < image.Height; j++)
                        {
                            bitmap.SetPixel(i - startX, j, image.GetPixel(i, j));
                        }
                    }
                    bitmapList.Add(bitmap);
                }
            }

            return bitmapList;
        }
        private int GetMoney(List<Bitmap> splitImageList)
        {
            return 0;
        }
        private int GetGrayNumColor(System.Drawing.Color posClr)
        {
            return (posClr.R * 19595 + posClr.G * 38469 + posClr.B * 7472) >> 16;
        }
        #endregion
    }
}
