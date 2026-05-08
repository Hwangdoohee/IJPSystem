using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID 계장 버블 (PT/FI/PI/LT 등). 26×42 (태그 포함). anchor=좌상단.</summary>
    public partial class InstrumentBubble : UserControl
    {
        public InstrumentBubble() => InitializeComponent();

        public string Type  { get => (string)GetValue(TypeProperty);  set => SetValue(TypeProperty, value); }
        public string TagId { get => (string)GetValue(TagIdProperty); set => SetValue(TagIdProperty, value); }

        public static readonly DependencyProperty TypeProperty =
            DependencyProperty.Register(nameof(Type),  typeof(string), typeof(InstrumentBubble), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty TagIdProperty =
            DependencyProperty.Register(nameof(TagId), typeof(string), typeof(InstrumentBubble), new PropertyMetadata(string.Empty));
    }
}
