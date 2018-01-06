using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace LineageMTool
{
    class GameMonitor
    {
        public State State { get; set; }
        public PlayerInfo Player;
        public SimulatorInfo Simulator;
        public Config _config;
        public GameMonitor(Config config)
        {
            State = State.Stop;
            _config = config;
            Player = new PlayerInfo(config);
            Simulator = new SimulatorInfo(config);
        }
        public Action<PlayerInfo> PlayerInfoChangedNotify;
        public Action<Image> MonitorScreenChagedNotify;
        public Action<string, Color, string> MonitorStateNotify;
        public void Monitor()
        {
            try
            {
                Stopwatch timer = Stopwatch.StartNew();
                while (State == State.Run)
                {
                    try
                    {
                        //IntPtr test = WinApi.FindWindowEx(hwndCalc, 0, null, null);
                        Image image = Simulator.GetImage((CaptureMode)_config.comboBoxCaptureSettingSelectIndex);
                        // 計算HP和MP                
                        Player.CalculateHpPercent(image);
                        Player.CalculateMpPercent(image);
                        MonitorScreenChagedNotify?.Invoke(image);
                        PlayerInfoChangedNotify?.Invoke(Player);
                        if (Player.Hp == 0 && Player.Mp == 0)
                        {
                            Player.State = RoleState.Error;
                            MonitorStateNotify?.Invoke("異常", Color.Red, "請確認組隊視窗是否開啟，以及XY範圍設定正確");
                        }
                        else
                        {
                            if (Player.Hp == 0 && Player.Mp > 0)
                            {
                                Player.State = RoleState.Die;
                            }
                            else
                            {
                                Player.State = RoleState.Normal;
                                MonitorStateNotify?.Invoke("正常", Color.Green, string.Empty);
                                // 判斷動作
                                CheckAction(Player.Hp, Player.Mp);
                            }
                        }
                    }
                    catch (Exception ex)
                    {

                    }

                    double refreshTime = double.Parse(_config.RefreshTime);
                    if (refreshTime > 0)
                        Thread.Sleep((int)(refreshTime * 1000));


                    timer.Restart();
                }
            }
            catch (Exception ex)
            {
                MonitorStateNotify?.Invoke("異常", Color.Red, "異常發生，停止外掛，錯誤原因:" + ex.Message);
            }
            finally
            {
                State = State.Stop;
                MonitorStateNotify?.Invoke("待機", Color.DarkBlue, string.Empty);
            }
        }

        private void CheckAction(int hp, int mp)
        {
            if (hp < int.Parse(_config.numericUp7DownText))
            {
                Simulator.SendMessage(_config.BackHomeHotKey);
                Thread.Sleep(500);
                Simulator.SendMessage(_config.BackHomeHotKey);
                Thread.Sleep(500);
                Simulator.SendMessage(_config.BackHomeHotKey);
                Player.State = RoleState.BackHome;
                State = State.Stop;
            }

            // 高治檢查
            if (hp < int.Parse(_config.numericUp8DownText))
                Simulator.SendMessage(_config.OrangeHotKey);

            if (hp < int.Parse(_config.numericUp3DownText) && mp > int.Parse(_config.numericUp4DownText))
                Simulator.SendMessage(_config.HealHpHotKey);

            else if (mp < int.Parse(_config.numericUp1DownText) && hp > int.Parse(_config.numericUp2DownText))
            {
                Simulator.SendMessage(_config.HpToMpHotKey);
            }
            else if (mp > int.Parse(_config.numericUp6DownText))
            {
                for (int i = 0; i < 3; i++)
                {
                    Simulator.SendMessage(_config.ArrowHotKey);
                }
            }
        }
    }
}
