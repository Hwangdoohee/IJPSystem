using IJPSystem.Platform.Domain.Enums;
using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    public partial class TankVisual : UserControl
    {
        public TankVisual()
        {
            InitializeComponent();
        }

        // ─── LevelStatus ───
        public static readonly DependencyProperty LevelStatusProperty =
            DependencyProperty.Register(nameof(LevelStatus), typeof(LevelStatus), typeof(TankVisual),
                new PropertyMetadata(LevelStatus.Unknown));

        public LevelStatus LevelStatus
        {
            get => (LevelStatus)GetValue(LevelStatusProperty);
            set => SetValue(LevelStatusProperty, value);
        }

        // ─── 4점 센서 ───
        public static readonly DependencyProperty SensorHHProperty =
            DependencyProperty.Register(nameof(SensorHH), typeof(bool), typeof(TankVisual),
                new PropertyMetadata(false));
        public bool SensorHH { get => (bool)GetValue(SensorHHProperty); set => SetValue(SensorHHProperty, value); }

        public static readonly DependencyProperty SensorHighProperty =
            DependencyProperty.Register(nameof(SensorHigh), typeof(bool), typeof(TankVisual),
                new PropertyMetadata(false));
        public bool SensorHigh { get => (bool)GetValue(SensorHighProperty); set => SetValue(SensorHighProperty, value); }

        public static readonly DependencyProperty SensorSetProperty =
            DependencyProperty.Register(nameof(SensorSet), typeof(bool), typeof(TankVisual),
                new PropertyMetadata(false));
        public bool SensorSet { get => (bool)GetValue(SensorSetProperty); set => SetValue(SensorSetProperty, value); }

        public static readonly DependencyProperty SensorEmptyProperty =
            DependencyProperty.Register(nameof(SensorEmpty), typeof(bool), typeof(TankVisual),
                new PropertyMetadata(true));   // Empty 센서: 약액 있을 때 점등 (정상=true)
        public bool SensorEmpty { get => (bool)GetValue(SensorEmptyProperty); set => SetValue(SensorEmptyProperty, value); }
    }
}
