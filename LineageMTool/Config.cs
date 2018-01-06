using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LineageMTool
{
    public class Rect
    {
        public string Left;
        public string Right;
        public string Top;
        public string Down;
    }

    public class Config
    {
        public Version Version { get; set; }

        public string SimulatorName { get; set; }
        public string RefreshTime { get; set; }

        public string numericUp1DownText { get; set; }
        public string numericUp2DownText { get; set; }
        public string numericUp4DownText { get; set; }
        public string numericUp3DownText { get; set; }
        public string numericUp6DownText { get; set; }
        public string numericUp8DownText { get; set; }
        public string numericUp7DownText { get; set; }

        public int comboBoxCaptureSettingSelectIndex { get; set; }

        public string HpToMpHotKey { get; set; }
        public string HealHpHotKey { get; set; }
        public string ArrowHotKey { get; set; }
        public string OrangeHotKey { get; set; }
        public string BackHomeHotKey { get; set; }
        public string DetoxificationHotKey { get; set; }

        public bool IsHpToMpHotKeyEnable { get; set; }
        public bool IsHealHpHotKeyEnable { get; set; }
        public bool IsArrowHotKeyEnable { get; set; }
        public bool IsOrangeHotKeyEnable { get; set; }
        public bool IsBackHomeHotKeyEnable { get; set; }
        public bool IsDetoxificationHotKeyEnable { get; set; }

        public int LineNotifyInterval { get; set; }
        public string Uid { get; set; }

        public Rect HpRect { get; set; }
        public Rect MpRect { get; set; }

        public Config()
        {
            HpRect = new Rect();
            MpRect = new Rect();
            Version = new Version(1, 1);
        }
    }
}
