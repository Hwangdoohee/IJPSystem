using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID 슬롯 다이(코팅 헤드). envelope 240×80, body y=6~50.</summary>
    public partial class SlotDie : UserControl
    {
        public SlotDie()
        {
            InitializeComponent();
        }

        public string TagId   { get => (string)GetValue(TagIdProperty);   set => SetValue(TagIdProperty, value); }
        public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }

        public static readonly DependencyProperty TagIdProperty =
            DependencyProperty.Register(nameof(TagId), typeof(string), typeof(SlotDie), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(SlotDie), new PropertyMetadata(string.Empty));
    }
}
