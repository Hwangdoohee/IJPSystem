using IJPSystem.Platform.Domain.Enums;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media.Animation;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID 레저버(중간 저장조). 가로 직사각형 520×120, anchor=좌상단.</summary>
    public partial class Reservoir : UserControl
    {
        private const double BodyHeight = 120;
        private const double InnerWidth = 512;

        public Reservoir()
        {
            InitializeComponent();
            UpdateLiquid(animate: false);
        }

        public string TagId   { get => (string)GetValue(TagIdProperty);   set => SetValue(TagIdProperty, value); }
        public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
        public LevelStatus LevelStatus { get => (LevelStatus)GetValue(LevelStatusProperty); set => SetValue(LevelStatusProperty, value); }
        public bool SensorHH    { get => (bool)GetValue(SensorHHProperty);    set => SetValue(SensorHHProperty, value); }
        public bool SensorHigh  { get => (bool)GetValue(SensorHighProperty);  set => SetValue(SensorHighProperty, value); }
        public bool SensorSet   { get => (bool)GetValue(SensorSetProperty);   set => SetValue(SensorSetProperty, value); }
        public bool SensorEmpty { get => (bool)GetValue(SensorEmptyProperty); set => SetValue(SensorEmptyProperty, value); }

        public static readonly DependencyProperty TagIdProperty =
            DependencyProperty.Register(nameof(TagId), typeof(string), typeof(Reservoir), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(Reservoir), new PropertyMetadata(string.Empty));
        // 기본값을 Empty로 설정하여 binding 평가 전에도 게이지가 가시화됨
        public static readonly DependencyProperty LevelStatusProperty =
            DependencyProperty.Register(nameof(LevelStatus), typeof(LevelStatus), typeof(Reservoir),
                new PropertyMetadata(LevelStatus.Empty, (d, _) => ((Reservoir)d).UpdateLiquid(animate: true)));
        public static readonly DependencyProperty SensorHHProperty =
            DependencyProperty.Register(nameof(SensorHH), typeof(bool), typeof(Reservoir), new PropertyMetadata(false));
        public static readonly DependencyProperty SensorHighProperty =
            DependencyProperty.Register(nameof(SensorHigh), typeof(bool), typeof(Reservoir), new PropertyMetadata(false));
        public static readonly DependencyProperty SensorSetProperty =
            DependencyProperty.Register(nameof(SensorSet), typeof(bool), typeof(Reservoir), new PropertyMetadata(false));
        public static readonly DependencyProperty SensorEmptyProperty =
            DependencyProperty.Register(nameof(SensorEmpty), typeof(bool), typeof(Reservoir), new PropertyMetadata(true));

        // 4점 센서 → 액위 비율
        // Unknown은 보수적으로 Empty와 동일 처리
        private static double RatioOf(LevelStatus s) => s switch
        {
            LevelStatus.HH    => 0.92,
            LevelStatus.High  => 0.78,
            LevelStatus.Set   => 0.50,
            LevelStatus.Low   => 0.25,
            LevelStatus.Empty => 0.06,
            _                 => 0.06,
        };

        private void UpdateLiquid(bool animate)
        {
            if (Liquid == null || LevelLine == null) return;

            double ratio = RatioOf(LevelStatus);
            double targetH    = BodyHeight * ratio;
            double targetTopY = BodyHeight - targetH;

            if (!animate)
            {
                Canvas.SetTop(Liquid, targetTopY);
                Liquid.Height = targetH;
                LevelLine.Y1 = targetTopY;
                LevelLine.Y2 = targetTopY;
                return;
            }

            // 부드러운 전환 (300ms ease-out)
            var ease = new QuadraticEase { EasingMode = EasingMode.EaseOut };
            var dur = new System.TimeSpan(0, 0, 0, 0, 300);

            var heightAnim = new DoubleAnimation { To = targetH, Duration = dur, EasingFunction = ease };
            Liquid.BeginAnimation(FrameworkElement.HeightProperty, null);
            Liquid.BeginAnimation(FrameworkElement.HeightProperty, heightAnim);

            // Canvas.Top은 attached property — DoubleAnimation으로 직접 애니메이트 불가하므로 즉시 설정
            // (Liquid 높이가 늘면서 위로 차오르는 시각효과는 Height 애니메이션만으로도 충분)
            Canvas.SetTop(Liquid, targetTopY);
            LevelLine.Y1 = targetTopY;
            LevelLine.Y2 = targetTopY;
        }
    }
}
