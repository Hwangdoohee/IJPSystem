using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    public partial class SensorIndicator : UserControl
    {
        public SensorIndicator()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsActiveProperty =
            DependencyProperty.Register(nameof(IsActive), typeof(bool), typeof(SensorIndicator),
                new PropertyMetadata(false));
        public bool IsActive { get => (bool)GetValue(IsActiveProperty); set => SetValue(IsActiveProperty, value); }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(SensorIndicator),
                new PropertyMetadata("Sensor"));
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }

        /// <summary>true이면 점등=알람(빨강), false이면 점등=정상(녹색)</summary>
        public static readonly DependencyProperty AlarmModeProperty =
            DependencyProperty.Register(nameof(AlarmMode), typeof(bool), typeof(SensorIndicator),
                new PropertyMetadata(false));
        public bool AlarmMode { get => (bool)GetValue(AlarmModeProperty); set => SetValue(AlarmModeProperty, value); }
    }
}
