using LineageMTool.Spazzarama.ScreenCapture;
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

using Imgur.API.Authentication.Impl;
using Imgur.API.Models;
using Imgur.API.Endpoints.Impl;
using Imgur.API;

namespace LineageMTool
{
    public enum State
    {
        Run,
        Stop
    }

    public partial class Form1 : Form
    {
        private Task _monitorTask = null;
        private string _configPath = "Config.json";
        private Version _version = new Version(1,1);
        private About frmAbout = null;
        private LineMessage _lineMessage;
        private GameMonitor _gameMonitor;
        private Config _config = new Config();
        public Form1()
        {
            InitializeComponent();
            Initial();
            timer1.Interval = (int)(numericUpDownLineNotifyMinute.Value * 60 * 1000);
            timer1.Start();
        }
       
        private void Initial()
        {
            comboBoxHpToMp.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBoxHealHp.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBoxArrow.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBoxOrange.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBoxBackToHome.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBox1.Items.AddRange(SimulatorInfo.HotKeyList.Split(','));
            comboBoxCaptureSetting.Items.Add("DirectX");
            comboBoxCaptureSetting.Items.Add("GDI");

            if (File.Exists(_configPath))
            {
                string jsonText = File.ReadAllText(_configPath);
                _config = JsonConvert.DeserializeObject<Config>(jsonText);
                if(_config.Version < _version)
                {
                    comboBoxHpToMp.SelectedIndex = 0;
                    comboBoxHealHp.SelectedIndex = 1;
                    comboBoxArrow.SelectedIndex = 2;
                    comboBoxOrange.SelectedIndex = 3;
                    comboBoxBackToHome.SelectedIndex = 5;
                }
                else
                    ConverConfigToUI();
            }
            else
            {
                comboBoxHpToMp.SelectedIndex = 0;
                comboBoxHealHp.SelectedIndex = 1;
                comboBoxArrow.SelectedIndex = 2;
                comboBoxOrange.SelectedIndex = 3;
                comboBoxBackToHome.SelectedIndex = 5;

                ConverUItoConfig();
            }

            labelError.Text = "待機";
            labelError.ForeColor = Color.DarkBlue;

            _gameMonitor = new GameMonitor(_config);
            _gameMonitor.MonitorScreenChagedNotify += MonitorScreenChagedNotify;
            _gameMonitor.MonitorStateNotify += MonitorStateNotify;
            _gameMonitor.PlayerInfoChangedNotify += PlayerInfoChangedNotify;
            _lineMessage = new LineMessage();
            _lineMessage.ErrorCallBack += ErrorNotify;
        }

        private void ConverUItoConfig()
        {
            _config.SimulatorName = textBoxSimulatorName.Text;
            _config.RefreshTime = textBoxRefresh.Text;

            _config.ArrowHotKey = comboBoxArrow.Text;
            _config.BackHomeHotKey = comboBoxBackToHome.Text;
            _config.comboBoxCaptureSettingSelectIndex = comboBoxCaptureSetting.SelectedIndex;
            _config.HealHpHotKey = comboBoxHealHp.Text;
            _config.HpToMpHotKey = comboBoxHpToMp.Text;
            _config.OrangeHotKey = comboBoxOrange.Text;
            _config.DetoxificationHotKey = comboBox1.Text;

            _config.IsArrowHotKeyEnable = checkBox3.Checked;
            _config.IsBackHomeHotKeyEnable = checkBox5.Checked;
            _config.IsDetoxificationHotKeyEnable = checkBox6.Checked;
            _config.IsHealHpHotKeyEnable = checkBox2.Checked;
            _config.IsHpToMpHotKeyEnable = checkBox1.Checked;
            _config.IsOrangeHotKeyEnable = checkBox4.Checked;

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

            _config.LineNotifyInterval = (int)numericUpDownLineNotifyMinute.Value;
            _config.Uid = textBoxUid.Text;
        }

        private void ConverConfigToUI()
        {
            textBoxSimulatorName.Text = _config.SimulatorName;
            textBoxRefresh.Text = _config.RefreshTime;
            comboBoxCaptureSetting.SelectedIndex = _config.comboBoxCaptureSettingSelectIndex;

            comboBoxHpToMp.Text = _config.HpToMpHotKey;
            comboBoxHealHp.Text = _config.HealHpHotKey;
            comboBoxArrow.Text = _config.ArrowHotKey;
            comboBoxOrange.Text = _config.OrangeHotKey;
            comboBoxBackToHome.Text = _config.BackHomeHotKey;
            comboBox1.Text = _config.DetoxificationHotKey;

            checkBox3.Checked = _config.IsArrowHotKeyEnable;
            checkBox5.Checked = _config.IsBackHomeHotKeyEnable;
            checkBox6.Checked = _config.IsDetoxificationHotKeyEnable;
            checkBox2.Checked = _config.IsHealHpHotKeyEnable;
            checkBox1.Checked = _config.IsHpToMpHotKeyEnable;
            checkBox4.Checked = _config.IsOrangeHotKeyEnable;

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

            numericUpDownLineNotifyMinute.Value = _config.LineNotifyInterval;
            textBoxUid.Text = _config.Uid;
        }

        private void button1_Click(object sender, EventArgs e)
        {
            if (_gameMonitor.State == State.Stop)
            {
                ConverUItoConfig();
                if (!_gameMonitor.Simulator.IsSimulatorOpen())
                {
                    MessageBox.Show("找不到雷電模擬器，請確認是否已經開啟，並進入天堂M遊戲");
                    return;
                }
                if (!IsParamterValid())
                {
                    MessageBox.Show("請確認輸入的參數正確後再啟動外掛");
                    return;
                }

                _gameMonitor.State = State.Run;
                button1.Text = "停止";
                _gameMonitor.Player.State = RoleState.Normal;
                _monitorTask = Task.Run(new Action(_gameMonitor.Monitor));
                timer1.Stop();
                timer1.Start();
            }
            else
            {
                _gameMonitor.State = State.Stop;
                button1.Text = "啟動";
                timer1.Stop();
            }
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
                _gameMonitor.State = State.Stop;
                ConverUItoConfig();

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
                if (pictureBox1.Image == null)
                    return;

                Color pixelColor = GetColorAt(e.Location);
                toolStripStatusLabel3.Text = pixelColor.ToString();
                toolStripStatusLabel1.Text = e.Location.X.ToString();
                toolStripStatusLabel2.Text = e.Location.Y.ToString();
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
                ErrorNotify("無法偵測到畫面，請不要把模擬器縮小到工具列");
                return new Color();
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

        private void timer1_Tick(object sender, EventArgs e)
        {
            _lineMessage.SendMessageToLine( textBoxUid.Text, 
                                            _gameMonitor.Player.GetRoleStateMessage(), 
                                            _gameMonitor.Simulator.GetImage((CaptureMode)_config.comboBoxCaptureSettingSelectIndex));
        }

        private void numericUpDownLineNotifyMinute_ValueChanged(object sender, EventArgs e)
        {
            timer1.Stop();
            timer1.Interval = (int)(numericUpDownLineNotifyMinute.Value * 60 * 1000);
            timer1.Start();
        }

        private void ErrorNotify(string lineError)
        {
            listBox1.Invoke(new Action(() =>
            {
                listBox1.Items.Add(lineError);
                listBox1.TopIndex = listBox1.Items.Count - 1;
            }));
        }

        private void button3_Click(object sender, EventArgs e)
        {
            listBox1.Items.Clear();
        }

        public void PlayerInfoChangedNotify(PlayerInfo player)
        {
            this.Invoke(new Action(() =>
            {
                textBoxHp.Text = player.Hp.ToString();
                textBoxMp.Text = player.Mp.ToString();
            }));
        }
        public void MonitorScreenChagedNotify(Image image)
        {
            this.Invoke(new Action(() =>
            {
                pictureBox1.Image = image;
            }));
        }

        public void MonitorStateNotify(string state, Color color, string errorMsg = "")
        {
            this.Invoke(new Action(() =>
            {
                labelError.Text = state;
                labelError.ForeColor = color;
                if (state.Equals("待機"))
                    button1.Text = "啟動";
                
                if (!string.IsNullOrWhiteSpace(errorMsg))
                {
                    listBox1.Items.Add(errorMsg);
                    listBox1.TopIndex = listBox1.Items.Count - 1;
                }
            }));
        }
    }
}
