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
            Simulator = new SimulatorInfo(config);
            Player = new PlayerInfo(config, Simulator);
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
                                MonitorStateNotify?.Invoke("正常", Color.Green, string.Empty);
                                //if(_config.PlayerNo == PlayerNo.P1)
                                //    Simulator.SendAutoAttackMessage();

                                //判斷動作
                                if (_config.IsFollow1P)
                                {
                                    Player.Get1PLocation(image);
                                    if ((Player.X != 0 && Player.Y != 0) && ((Math.Abs(Player.X - 1273) > 50) || (Math.Abs(Player.Y - 225) > 50)))
                                    {
                                        MoveTo1P();
                                    }
                                    else
                                    {
                                        //if (Player.CheckIfGotAttackMessage(image))
                                        //{
                                            Simulator.SendAutoAttackMessage();
                                        //}

                                        CheckAction(Player.Hp, Player.Mp);
                                    }
                                }
                                else
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

        private void MoveTo1P()
        {
            if ( Player.X == 0 && Player.Y == 0 )
                    return;

            double angle = CalAngle(new Point(1273,225), new Point(Player.X, Player.Y));
            Simulator.SendMouseMessage((int)(Math.Cos(angle)*165+ 697), 370 - (int)(Math.Sin(angle) * 165));


            //if (Player.X >= 1019 && Player.Y <= 213)
            //{
            //    Simulator.SendMouseMessage(740, 208);
            //}
            //else if (Player.X >= 1019 && Player.Y >= 213)
            //{
            //    Simulator.SendMouseMessage(740, 527);
            //}
            //else if(Player.X <= 1019 && Player.Y <= 213)
            //{
            //    Simulator.SendMouseMessage(304, 208);
            //}
            //else if(Player.X <= 1019 && Player.Y >= 213)
            //{
            //    Simulator.SendMouseMessage(304, 527);
            //}

        }

        private double CalAngle(Point pa, Point pb)
        {
            // pa為圓心  
            /// Y alias is reverse from Cartesian plane
            return Math.Atan2(pa.Y - pb.Y, pb.X - pa.X);
        }

        private void CheckAction(int hp, int mp)
        {
            if (hp < 10)
            {
                //中毒或石化暫時不處理
                return;
            }
            if (_config.IsDetoxificationHotKeyEnable && Player.State == RoleState.Detoxification)
            {
                Simulator.SendMessage(_config.DetoxificationHotKey);
                return;
            }

            if (_config.IsBackHomeHotKeyEnable && hp < int.Parse(_config.numericUp7DownText))
            {
                Simulator.SendMessage(_config.BackHomeHotKey);
                Thread.Sleep(500);
                Simulator.SendMessage(_config.BackHomeHotKey);
                Thread.Sleep(500);
                Simulator.SendMessage(_config.BackHomeHotKey);
                Player.State = RoleState.BackHome;
                State = State.Stop;
                Simulator.SendWindowOnTop();
                return;
            }

            if (_config.IsOrangeHotKeyEnable && hp < int.Parse(_config.numericUp8DownText))
                Simulator.SendMessage(_config.OrangeHotKey);

            if (_config.IsHealHpHotKeyEnable && hp < int.Parse(_config.numericUp3DownText) && mp > int.Parse(_config.numericUp4DownText))
                Simulator.SendMessage(_config.HealHpHotKey);

            else if (_config.IsHpToMpHotKeyEnable && (mp < int.Parse(_config.numericUp1DownText) && hp > int.Parse(_config.numericUp2DownText)))
            {
                Simulator.SendMessage(_config.HpToMpHotKey);
            }
            else if (_config.IsArrowHotKeyEnable && mp > int.Parse(_config.numericUp6DownText))
            {
                for (int i = 0; i < 3; i++)
                {
                    Simulator.SendMessage(_config.ArrowHotKey);
                }
            }
        }
    }
}
