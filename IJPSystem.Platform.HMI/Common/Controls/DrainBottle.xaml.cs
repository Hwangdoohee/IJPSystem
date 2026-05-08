using IJPSystem.Platform.Domain.Enums;
using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID 드레인 보틀 — 납작한 병 모양 + 받침대.
    /// envelope 110×80, 받침대 y=80~92.
    /// 3 sensors: SensorDetect (보틀 감지), SensorLeak (누액), SensorHigh (액위 상한).</summary>
    public partial class DrainBottle : UserControl
    {
        // 병 body 내부 영역
        private const double BodyTop = 13;
        private const double BodyBottom = 80;
        private const double BodyHeight = BodyBottom - BodyTop; // 67

        public DrainBottle()
        {
            InitializeComponent();
            UpdateLiquid();
        }

        public string TagId   { get => (string)GetValue(TagIdProperty);   set => SetValue(TagIdProperty, value); }
        public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
        public LevelStatus LevelStatus { get => (LevelStatus)GetValue(LevelStatusProperty); set => SetValue(LevelStatusProperty, value); }
        public bool SensorDetect { get => (bool)GetValue(SensorDetectProperty); set => SetValue(SensorDetectProperty, value); }
        public bool SensorLeak   { get => (bool)GetValue(SensorLeakProperty);   set => SetValue(SensorLeakProperty,   value); }
        public bool SensorHigh   { get => (bool)GetValue(SensorHighProperty);   set => SetValue(SensorHighProperty,   value); }

        public static readonly DependencyProperty TagIdProperty =
            DependencyProperty.Register(nameof(TagId), typeof(string), typeof(DrainBottle), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(DrainBottle), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty LevelStatusProperty =
            DependencyProperty.Register(nameof(LevelStatus), typeof(LevelStatus), typeof(DrainBottle),
                new PropertyMetadata(LevelStatus.Empty, (d, _) => ((DrainBottle)d).UpdateLiquid()));
        public static readonly DependencyProperty SensorDetectProperty =
            DependencyProperty.Register(nameof(SensorDetect), typeof(bool), typeof(DrainBottle), new PropertyMetadata(false));
        public static readonly DependencyProperty SensorLeakProperty =
            DependencyProperty.Register(nameof(SensorLeak), typeof(bool), typeof(DrainBottle), new PropertyMetadata(false));
        public static readonly DependencyProperty SensorHighProperty =
            DependencyProperty.Register(nameof(SensorHigh), typeof(bool), typeof(DrainBottle), new PropertyMetadata(false));

        private void UpdateLiquid()
        {
            if (Liquid == null || LevelLine == null) return;

            double ratio = LevelStatus switch
            {
                LevelStatus.HH    => 0.92,
                LevelStatus.High  => 0.78,
                LevelStatus.Set   => 0.50,
                LevelStatus.Low   => 0.25,
                LevelStatus.Empty => 0.06,
                _                 => 0.06,
            };
            double h    = BodyHeight * ratio;
            double topY = BodyBottom - h;
            Canvas.SetTop(Liquid, topY);
            Liquid.Height = h;
            LevelLine.Y1 = topY;
            LevelLine.Y2 = topY;
        }
    }
}
