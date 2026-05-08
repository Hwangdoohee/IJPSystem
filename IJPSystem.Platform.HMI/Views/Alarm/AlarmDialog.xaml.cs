using System;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Animation;

namespace IJPSystem.Platform.HMI.Views
{
    /// <summary>
    /// AlarmDialog.xaml에 대한 상호 작용 논리.
    /// Severity (Fatal/Error/Warning/Info)에 따라 테두리/헤더/배지 팔레트 분기.
    /// </summary>
    public partial class AlarmDialog : Window
    {
        // 초기값은 Fatal 팔레트(빨강) — XAML 기본 색상과 일치.
        public string Severity { get; set; } = "Fatal";

        public AlarmDialog()
        {
            InitializeComponent();
            Loaded += (_, __) => ApplyPalette(Severity);
        }

        private record Palette(Color Primary, Color Mid, Color Dark, Color BadgeBg, Color BadgeFg, string HeaderTitle);

        private static readonly Color SlateDim = (Color)ColorConverter.ConvertFromString("#334155");

        private static Palette GetPalette(string? severity) => (severity ?? "").ToUpperInvariant() switch
        {
            "FATAL"   => new(C("#EF4444"), C("#DC2626"), C("#991B1B"), C("#450A0A"), C("#FCA5A5"), "SYSTEM ALARM"),
            "ERROR"   => new(C("#F97316"), C("#EA580C"), C("#9A3412"), C("#431407"), C("#FED7AA"), "ERROR"),
            "WARNING" => new(C("#EAB308"), C("#CA8A04"), C("#854D0E"), C("#422006"), C("#FDE68A"), "WARNING"),
            "INFO"    => new(C("#3B82F6"), C("#2563EB"), C("#1E40AF"), C("#172554"), C("#BFDBFE"), "INFO"),
            _         => new(C("#EF4444"), C("#DC2626"), C("#991B1B"), C("#450A0A"), C("#FCA5A5"), "SYSTEM ALARM"),
        };

        private static Color C(string hex) => (Color)ColorConverter.ConvertFromString(hex);

        private void ApplyPalette(string? severity)
        {
            var p = GetPalette(severity);

            // 정적 요소
            BorderBrushTarget.Color = p.Primary;
            GlowEffect.Color        = p.Primary;
            HeaderStop1.Color       = p.Mid;
            HeaderStop2.Color       = p.Dark;
            AlarmCodeBadge.Background = new SolidColorBrush(p.BadgeBg);
            AlarmCodeText.Foreground  = new SolidColorBrush(p.BadgeFg);
            HeaderTitle.Text          = p.HeaderTitle;

            // 테두리 펄스 애니메이션 — XAML 기본(빨강↔슬레이트)을 Severity 색상으로 덮어쓰기
            var pulse = new ColorAnimation
            {
                From           = p.Primary,
                To             = SlateDim,
                Duration       = TimeSpan.FromSeconds(0.6),
                AutoReverse    = true,
                RepeatBehavior = RepeatBehavior.Forever,
            };
            BorderBrushTarget.BeginAnimation(SolidColorBrush.ColorProperty, pulse);
        }

        private void ACK_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = false; // 확인만, 알람 유지
            Close();
        }

        private void Reset_Click(object sender, RoutedEventArgs e)
        {
            DialogResult = true;  // 알람 해제 요청
            Close();
        }
    }
}
