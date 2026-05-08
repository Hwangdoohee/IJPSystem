using IJPSystem.Platform.Domain.Enums;
using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID 퍼지 배스. 가로 250×50, anchor=좌상단.</summary>
    public partial class PurgeBath : UserControl
    {
        private const double BodyHeight = 50;

        public PurgeBath()
        {
            InitializeComponent();
            UpdateLiquid();
        }

        public string TagId   { get => (string)GetValue(TagIdProperty);   set => SetValue(TagIdProperty, value); }
        public string Caption { get => (string)GetValue(CaptionProperty); set => SetValue(CaptionProperty, value); }
        public LevelStatus LevelStatus { get => (LevelStatus)GetValue(LevelStatusProperty); set => SetValue(LevelStatusProperty, value); }

        public static readonly DependencyProperty TagIdProperty =
            DependencyProperty.Register(nameof(TagId), typeof(string), typeof(PurgeBath), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty CaptionProperty =
            DependencyProperty.Register(nameof(Caption), typeof(string), typeof(PurgeBath), new PropertyMetadata(string.Empty));
        public static readonly DependencyProperty LevelStatusProperty =
            DependencyProperty.Register(nameof(LevelStatus), typeof(LevelStatus), typeof(PurgeBath),
                new PropertyMetadata(LevelStatus.Unknown, (d, _) => ((PurgeBath)d).UpdateLiquid()));

        private void UpdateLiquid()
        {
            double ratio = LevelStatus switch
            {
                LevelStatus.HH    => 0.85,
                LevelStatus.High  => 0.70,
                LevelStatus.Set   => 0.45,
                LevelStatus.Low   => 0.25,
                LevelStatus.Empty => 0.06,
                _                 => 0.0,
            };
            double h    = BodyHeight * ratio;
            double topY = BodyHeight - h;
            Canvas.SetTop(Liquid, topY);
            Liquid.Height = h;
            LevelLine.Y1 = topY;
            LevelLine.Y2 = topY;
        }
    }
}
