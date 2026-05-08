using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    public partial class ValveControl : UserControl
    {
        public ValveControl()
        {
            InitializeComponent();
        }

        public static readonly DependencyProperty IsOpenProperty =
            DependencyProperty.Register(nameof(IsOpen), typeof(bool), typeof(ValveControl),
                new PropertyMetadata(false));
        public bool IsOpen { get => (bool)GetValue(IsOpenProperty); set => SetValue(IsOpenProperty, value); }

        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(ValveControl),
                new PropertyMetadata("Valve"));
        public string Label { get => (string)GetValue(LabelProperty); set => SetValue(LabelProperty, value); }
    }
}
