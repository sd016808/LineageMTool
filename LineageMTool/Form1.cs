using Newtonsoft.Json;
using ScreenShotDemo;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace LineageMTool
{
    enum State
    {
        Run,
        Stop
    }

    enum RoleState
    {
        Normal,
        Error,
        Die,
        OutOfArrow,
        BackHome
    }

    public partial class Form1 : Form
    {
        private State _state = State.Stop;
        private RoleState _roleState = RoleState.Normal;
        private string _hotKeyList = "鍵盤7,鍵盤8,鍵盤9,鍵盤0,鍵盤U,鍵盤I,鍵盤O,鍵盤P";
        private string _uid = string.Empty;
        private Task _monitorTask = null;
        private Config _config = null;
        private string _configPath = "Config.json";
        private int _version = 1;
        private About frmAbout = null;
        private uint WM_KEYDOWN = 0x0100;
        private uint WM_KEYUP = 0x0101;
        public const int WM_LBUTTONDOWN = 0x0201;
        public const int WM_LBUTTONUP = 0x0202;
        private string _oldmessage = "";
        public Form1()
        {
            InitializeComponent();
            Initial();
        }
       
        private void Initial()
        {
            ManageAero(false);
            comboBoxHpToMp.Items.AddRange(_hotKeyList.Split(','));
            comboBoxHealHp.Items.AddRange(_hotKeyList.Split(','));
            comboBoxArrow.Items.AddRange(_hotKeyList.Split(','));
            comboBoxOrange.Items.AddRange(_hotKeyList.Split(','));
            comboBoxBackToHome.Items.AddRange(_hotKeyList.Split(','));

            if(File.Exists(_configPath))
            {
                string jsonText = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<Config>(jsonText);

                if (_config.Version < _version)
                {
                    _config = new Config();
                    comboBoxHpToMp.SelectedIndex = 0;
                    comboBoxHealHp.SelectedIndex = 1;
                    comboBoxArrow.SelectedIndex = 2;
                    comboBoxOrange.SelectedIndex = 3;
                    comboBoxBackToHome.SelectedIndex = 5;
                }
                else
                {
                    textBoxSimulatorName.Text = _config.SimulatorName;
                    comboBoxHpToMp.SelectedIndex = _config.HpToMpSelectIndex;
                    comboBoxHealHp.SelectedIndex = _config.HealSelectIndex;
                    comboBoxArrow.SelectedIndex = _config.ArrowSelectIndex;
                    comboBoxOrange.SelectedIndex = _config.OrangeSelectIndex;
                    comboBoxBackToHome.SelectedIndex = _config.BackHomeSelectIndex;
                    textBoxRefresh.Text = _config.RefreshTime;

                    numericUpDown1.Text = _config.numericUp1DownText;
                    numericUpDown2.Text = _config.numericUp2DownText;
                    numericUpDown3.Text = _config.numericUp3DownText;
                    numericUpDown4.Text = _config.numericUp4DownText;
                    numericUpDown6.Text = _config.numericUp6DownText;
                    numericUpDown7.Text = _config.numericUp7DownText;
                    numericUpDown8.Text = _config.numericUp8DownText;

                    textBox4.Text = _config.HpRect.Top;
                    textBox3.Text = _config.HpRect.Down;
                    textBox1.Text = _config.HpRect.Left;
                    textBox2.Text = _config.HpRect.Right;

                    textBox6.Text = _config.MpRect.Top;
                    textBox5.Text = _config.MpRect.Down;
                    textBox8.Text = _config.MpRect.Left;
                    textBox7.Text = _config.MpRect.Right;
                }
            }
            else
            {
                _config = new Config();
                comboBoxHpToMp.SelectedIndex = 0;
                comboBoxHealHp.SelectedIndex = 1;
                comboBoxArrow.SelectedIndex = 2;
                comboBoxOrange.SelectedIndex = 3;
                comboBoxBackToHome.SelectedIndex = 5;
            }

            labelError.Text = "待機";
            labelError.ForeColor = Color.DarkBlue;
        }

        public readonly uint DWM_EC_DISABLECOMPOSITION = 0;
        public readonly uint DWM_EC_ENABLECOMPOSITION = 1;
        [DllImport("dwmapi.dll", EntryPoint = "DwmEnableComposition")]
        protected static extern uint Win32DwmEnableComposition(uint uCompositionAction);

        public void ManageAero(bool a)
        {
            if (a)
                Win32DwmEnableComposition(DWM_EC_ENABLECOMPOSITION);
            if (!a)
                Win32DwmEnableComposition(DWM_EC_DISABLECOMPOSITION);
        }

        private void textBox1_KeyPress(object sender, KeyPressEventArgs e)
        {
            //e.Handled = !char.IsDigit(e.KeyChar) && !char.IsControl(e.KeyChar);
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_state == State.Stop)
            {
                if(!IsSimulatorOpen())
                {
                    MessageBox.Show("找不到雷電模擬器，請確認是否已經開啟，並進入天堂M遊戲");
                    return;
                }
                if (!IsParamterValid())
                {
                    MessageBox.Show("請確認輸入的參數正確後再啟動外掛");
                    return;
                }

                _state = State.Run;
                button1.Text = "停止";
                _uid = textBox9.Text;
                _roleState = RoleState.Normal;
                _monitorTask = Task.Run(new Action(Monitor));
            }
            else
            {
                _state = State.Stop;
                button1.Text = "啟動";
            }
        }

        private string GetMessage()
        {
            switch(_roleState)
            {
                case RoleState.Error:
                    return "外掛發生錯誤";
                case RoleState.Die:
                    return "角色死亡";
                case RoleState.OutOfArrow:
                    return "箭矢用完，使用回捲回村";
                case RoleState.BackHome:
                    return "使用回捲回村";
                default:
                    return "外掛正常運行中";
            }
        }

        private void SendMessageToLine(string uid, string message)
        {
            if (_oldmessage == message)
                return;

            try
            {
                _oldmessage = message;
                //isRock.LineBot.Utility.PushMessage(
                // "Uf65bb0bcdc6e631cafc09c79cb2183df", "Hello Harris", "XfzPgOG9PcPqQj38QNOWkAtpSC8M7K2TJGPe0erfeogRIOr/6Xh5Hdl+CDwt0KUgkd0PvTLQ5ebqCyzYNT9kbJshTDy54NgKZG/9tFaRTQPmWH4x/l7xpGXTWTdLSdLVx9aKtSYvLVFJoUd0vPbfvAdB04t89/1O/w1cDnyilFU=");
                isRock.LineBot.Utility.PushMessage(
                uid, message, "XfzPgOG9PcPqQj38QNOWkAtpSC8M7K2TJGPe0erfeogRIOr/6Xh5Hdl+CDwt0KUgkd0PvTLQ5ebqCyzYNT9kbJshTDy54NgKZG/9tFaRTQPmWH4x/l7xpGXTWTdLSdLVx9aKtSYvLVFJoUd0vPbfvAdB04t89/1O/w1cDnyilFU=");
            }
            catch
            {
                listBox1.Invoke(new Action(() =>
                { 
                    listBox1.Items.Add("請確認Line uid設定正確");
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                }));
            }
        }

        private void Monitor()
        {
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                while (_state == State.Run)
                {
                    // 抓取雷電模擬器的Handle
                    IntPtr hwndCalc = WinApi.FindWindow(null, textBoxSimulatorName.Text);

                    // 抓取雷電的畫面
                    ScreenCapture sc = new ScreenCapture();
                    try
                    {
                        //IntPtr test = WinApi.FindWindowEx(hwndCalc, 0, null, null);
                        //while (true)
                        //{
                        //    WinApi.SendMessage(test, WM_LBUTTONDOWN, 800, 400);
                        //    Thread.Sleep(1000);
                        //    WinApi.SendMessage(test, WM_LBUTTONUP, 800, 400);
                        //    Thread.Sleep(1000);
                        //}
                        Image image = sc.CaptureWindow(hwndCalc);
                        // 計算HP和MP
                        int hp = CalculateHpPercent(image);
                        textBoxHp.Invoke(new Action(() => { textBoxHp.Text = hp.ToString(); }));
                        int mp = CalculateMpPercent(image);
                        textBoxMp.Invoke(new Action(() => { textBoxMp.Text = mp.ToString(); }));
                        bool isArrowRunOut = false;// IsArrowRunOut(image);
                        pictureBox1.Image = image;
                        if (hp == 0 && mp == 0)
                        {
                            listBox1.Invoke(new Action(()=> 
                            {
                                _roleState = RoleState.Error;
                                labelError.Text = "異常";
                                labelError.ForeColor = Color.Red;
                                listBox1.Items.Add("請確認組隊視窗是否開啟，以及XY範圍設定正確");
                                listBox1.TopIndex = listBox1.Items.Count - 1;
                            }));
                            _roleState = RoleState.Error;
                            string msg = GetMessage();
                            SendMessageToLine(_uid, msg);
                        }
                        else
                        {
                            if (hp == 0 && mp > 0)
                                _roleState = RoleState.Die;

                            labelError.Invoke(new Action(() =>
                            {
                                labelError.Text = "正常";
                                labelError.ForeColor = Color.Green;
                            }));
                            // 判斷動作
                            if (isArrowRunOut)
                            {
                                _roleState = RoleState.OutOfArrow;
                                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxBackToHome), hwndCalc); }));
                                string msg = GetMessage();
                                SendMessageToLine(_uid, msg);
                                _state = State.Stop;
                            }
                            else
                            {
                                CheckAction(hp, mp, hwndCalc);
                            }
                        }
                    }
                    catch(Exception ex)
                    {

                    }

                    double refreshTime = (double)textBoxRefresh.Invoke(new Func<double>(()=> { return double.Parse(textBoxRefresh.Text); }));
                    if (refreshTime > 0)
                        Thread.Sleep((int)(refreshTime*1000));


                    timer.Restart();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show("異常發生，停止外掛，錯誤原因:" + ex.Message);
            }
            finally
            {
                _state = State.Stop;
                button1.Invoke(new Action(() => 
                {
                    button1.Text = "啟動";
                    listBox1.Items.Clear();
                    labelError.Text = "待機";
                    labelError.ForeColor = Color.DarkBlue;
                }));
            }
        }

        private void BackToHome()
        {
            //throw new NotImplementedException();
        }

        private bool IsArrowRunOut(Image image)
        {
            Bitmap bmp = new Bitmap(image);
            Rect2 rect = new Rect2();
            rect.Left = 1045;
            rect.Right = 1076;
            rect.Top = 635;
            rect.Down = 654;

            for (int x = rect.Left; x <= rect.Right; x++)
            {
                for (int y = rect.Top; y <= rect.Down; y++)
                {
                    Color pxl = bmp.GetPixel(x, y);
                    if (pxl.R > 100 && pxl.G > 100)
                        return false;
                }
            }
            return true;
        }

        private Bitmap GetMoneyArea(Image image)
        {
            Bitmap bmp = new Bitmap(699-613 + 1,80-60 + 1, PixelFormat.Format24bppRgb);
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
                            bitmap.SetPixel(i - startX, j, image.GetPixel(i,j));
                        }
                    }
                    bitmapList.Add(bitmap);
                }
            }

            return bitmapList;
        }

        private int GetMoney(List<Bitmap> splitImageList)
        {
            //foreach(var image in splitImageList)
            //{

            //}
            return 0;
        }

        private int GetGrayNumColor(System.Drawing.Color posClr)
        {
            return (posClr.R * 19595 + posClr.G * 38469 + posClr.B * 7472) >> 16;
        }

        private int CalculateHpPercent(Image image)
        {
            List<Color> redPointList = new List<Color>();
            int hpleftX = int.Parse(textBox1.Text);
            int hprightX = int.Parse(textBox2.Text);
            int hptopY = int.Parse(textBox4.Text);
            int hpdownY = int.Parse(textBox3.Text);
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
                    if ((RedAverage > 100 && GreenAverage < 50 && BlueAverage < 50) || (RedAverage < 20 && GreenAverage > 50 && BlueAverage < 20))
                        redPointList.Add(Color.FromArgb(RedAverage, GreenAverage, BlueAverage));
                }
            }
            int hpPercent = (int)(redPointList.Count*100 / (hprightX - hpleftX));
            return hpPercent;
        }

        private int CalculateMpPercent(Image image)
        {
            List<Color> bluePointList = new List<Color>();

            int hpleftX = int.Parse(textBox8.Text);
            int hprightX = int.Parse(textBox7.Text);
            int hptopY = int.Parse(textBox6.Text);
            int hpdownY = int.Parse(textBox5.Text);
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
                        RedAverage += pxl.R;
                        GreenAverage += pxl.G;
                        BlueAverage += pxl.B;
                    }
                    RedAverage /= (hpdownY - hptopY);
                    GreenAverage /= (hpdownY - hptopY);
                    BlueAverage /= (hpdownY - hptopY);
                    if (BlueAverage > 100 && RedAverage < 20)
                        bluePointList.Add(Color.FromArgb(RedAverage, GreenAverage, BlueAverage));
                }
            }
            int hpPercent = (int)(bluePointList.Count * 100 / (hprightX - hpleftX));
            return hpPercent;
        }

        private bool IsSimulatorOpen()
        {
            IntPtr hwndCalc = WinApi.FindWindow(null, textBoxSimulatorName.Text);
            return hwndCalc != IntPtr.Zero;
        }

        private bool IsParamterValid()
        {
            try
            {
                //檢查刷新頻率設定
                double.Parse(textBoxRefresh.Text);

                //檢查熱鍵是否重複
                List<string> hotKeyCheckList = new List<string>();
                if (IsHotKeyUsed(hotKeyCheckList, comboBoxHpToMp))
                    return false;
                if (IsHotKeyUsed(hotKeyCheckList, comboBoxHealHp))
                    return false;
                if (IsHotKeyUsed(hotKeyCheckList, comboBoxArrow))
                    return false;
                if (IsHotKeyUsed(hotKeyCheckList, comboBoxOrange))
                    return false;
                if (IsHotKeyUsed(hotKeyCheckList, comboBoxBackToHome))
                    return false;

                return true;
            }
            catch
            {
                return false;
            }
        }

        private bool IsHotKeyUsed(List<string> hotKeyCheckList, ComboBox comboBox)
        {
            if (hotKeyCheckList.Any(x => x.Equals(comboBox.Text)))
                return true;

            hotKeyCheckList.Add(comboBox.Text);
            return false;
        }

        private void Form1_FormClosing(object sender, FormClosingEventArgs e)
        {
            try
            {
                _state = State.Stop;
                _config.SimulatorName = textBoxSimulatorName.Text;
                _config.HpToMpSelectIndex = comboBoxHpToMp.SelectedIndex;
                _config.HealSelectIndex = comboBoxHealHp.SelectedIndex;
                _config.ArrowSelectIndex = comboBoxArrow.SelectedIndex;
                _config.OrangeSelectIndex = comboBoxOrange.SelectedIndex;
                _config.BackHomeSelectIndex = comboBoxBackToHome.SelectedIndex;
                _config.RefreshTime = textBoxRefresh.Text;

                _config.numericUp1DownText = numericUpDown1.Text;
                _config.numericUp2DownText = numericUpDown2.Text;
                _config.numericUp3DownText = numericUpDown3.Text;
                _config.numericUp4DownText = numericUpDown4.Text;
                _config.numericUp6DownText = numericUpDown6.Text;
                _config.numericUp7DownText = numericUpDown7.Text;
                _config.numericUp8DownText = numericUpDown8.Text;

                _config.HpRect.Top = textBox4.Text;
                _config.HpRect.Down = textBox3.Text;
                _config.HpRect.Left = textBox1.Text;
                _config.HpRect.Right = textBox2.Text;

                _config.MpRect.Top = textBox6.Text;
                _config.MpRect.Down = textBox5.Text;
                _config.MpRect.Left = textBox8.Text;
                _config.MpRect.Right = textBox7.Text;

                _config.Version = _version;

                ManageAero(true);

                string jsonText = JsonConvert.SerializeObject(_config);
                File.WriteAllText(_configPath, jsonText);
            }
            catch
            {

            }
        }

        private void pictureBox1_MouseMove(object sender, MouseEventArgs e)
        {
            try
            {
                //lock(lockObject)
                {
                    if (pictureBox1.Image == null)
                        return;

                    Color pixelColor = GetColorAt(e.Location);
                    toolStripStatusLabel3.Text = pixelColor.ToString();
                    toolStripStatusLabel1.Text = e.Location.X.ToString();
                    toolStripStatusLabel2.Text = e.Location.Y.ToString();
                }
            }
            catch
            {

            }
        }

        private Color GetColorAt(Point point)
        {
            try
            {
                return ((Bitmap)pictureBox1.Image).GetPixel(point.X, point.Y);
            }
            catch
            {
                _state = State.Stop;
                //MessageBox.Show("請不要把模擬器縮小到工具列");
                return new Color();
            }
        }
        
        private void CheckAction(int hp, int mp, IntPtr hwndCalc)
        {
            if (hp < numericUpDown7.Value)
            {
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxBackToHome), hwndCalc); }));
                Thread.Sleep(500);
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxBackToHome), hwndCalc); }));
                Thread.Sleep(500);
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxBackToHome), hwndCalc); }));
                _state = State.Stop;
                _roleState = RoleState.BackHome;
                string msg = GetMessage();
                SendMessageToLine(_uid, msg);
            }

            // 高治檢查
            if (hp < numericUpDown8.Value)
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxOrange), hwndCalc); }));

            if (hp < numericUpDown3.Value && mp > numericUpDown4.Value)
            {
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxHealHp), hwndCalc); }));
            }
            else if (mp < numericUpDown1.Value && hp > numericUpDown2.Value)
            {
                this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxHpToMp), hwndCalc); }));
            }
            else if (mp > numericUpDown6.Value)
            {
                for (int i = 0; i < 3; i++)
                {
                    this.Invoke(new Action(() => { SendMessageToSimulator(GetComboboxText(comboBoxArrow), hwndCalc); }));
                }
            }
        }

        private void SendMessageToSimulator(string action, IntPtr hwndCalc)
        {
            string[] hotkeyList= _hotKeyList.Split(',');
            IntPtr test = IntPtr.Zero;
            if(textBoxSimulatorName.Text.Contains("雷電"))
                test = WinApi.FindWindowEx(hwndCalc, 0, null, null);
            else
                test = hwndCalc;

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
            else if(action == hotkeyList[2])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D9), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D9), 0);
            }
            else if(action == hotkeyList[3])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.D0), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.D0), 0);
            }
            else if(action == hotkeyList[4])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.U), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.U), 0);
            }
            else if(action == hotkeyList[5])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.I), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.I), 0);
            }
            else if(action == hotkeyList[6])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.O), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.O), 0);
            }
            else if(action == hotkeyList[7])
            {
                WinApi.SendMessage(test, WM_KEYDOWN, Convert.ToInt32(Keys.P), 0);
                Thread.Sleep(100);
                WinApi.SendMessage(test, WM_KEYUP, Convert.ToInt32(Keys.P), 0);
            }
        }

        private string GetComboboxText(ComboBox combobox)
        {
            string text = string.Empty;
            combobox.Invoke(new Action(()=>
            {
                text = combobox.Text;
            }));
            return text;
        }

        private void button2_Click(object sender, EventArgs e)
        {
            if (frmAbout != null && frmAbout.IsDisposed == false && frmAbout.Visible == true)
            {
                return;
            }
            else
            {
                frmAbout = new About();
                frmAbout.TopMost = true;
                frmAbout.Show();
            }
        }
    }

    public class Rect2
    {
        public int Left;
        public int Right;
        public int Top;
        public int Down;
    }

    public class Rect
    {
        public string Left;
        public string Right;
        public string Top;
        public string Down;
    }

    public class Config
    {
        public int Version { get; set; }
        public int HpToMpSelectIndex { get; set; }
        public int HealSelectIndex { get; set; }
        public int ArrowSelectIndex { get; set; }
        public int OrangeSelectIndex { get; set; }
        public int BackHomeSelectIndex { get; set; }
        public string SimulatorName { get; set; }
        public string RefreshTime { get; set; }

        public string numericUp1DownText { get; set; }
        public string numericUp2DownText { get; set; }
        public string numericUp4DownText { get; set; }
        public string numericUp3DownText { get; set; }
        public string numericUp6DownText { get; set; }
        public string numericUp8DownText { get; set; }
        public string numericUp7DownText { get; set; }

        public Rect HpRect { get; set; }
        public Rect MpRect { get; set; }

        public Config()
        {
            HpRect = new Rect();
            MpRect = new Rect();
            Version = 0;
        }
    }
}
