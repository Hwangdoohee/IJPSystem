using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>
    /// P&amp;ID용 단일 인쇄 헤드 컨트롤.
    /// 매니폴드 분기선 + 헤드 박스 + 노즐 슬릿/점 + 라벨.
    /// PnidView에서 Canvas.Left/Top으로 위치만 지정하면 됨.
    /// </summary>
    public partial class PrintHead : UserControl
    {
        public PrintHead()
        {
            InitializeComponent();
        }

        // 헤드 라벨 (H1, H2, ...)
        public static readonly DependencyProperty LabelProperty =
            DependencyProperty.Register(nameof(Label), typeof(string), typeof(PrintHead),
                new PropertyMetadata(string.Empty));

        public string Label
        {
            get => (string)GetValue(LabelProperty);
            set => SetValue(LabelProperty, value);
        }
    }
}
