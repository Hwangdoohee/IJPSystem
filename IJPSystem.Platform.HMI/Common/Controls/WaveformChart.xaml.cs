using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using IJPSystem.Platform.HMI.Common;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    public partial class WaveformChart : UserControl
    {
        private const double PadLeft   = 38;
        private const double PadRight  = 10;
        private const double PadTop    = 8;
        private const double PadBottom = 20;

        private static readonly Brush GridBrush  = new SolidColorBrush(Color.FromArgb(45, 148, 163, 184));
        private static readonly Brush LabelBrush = new SolidColorBrush(Color.FromRgb(100, 116, 139));
        private static readonly Brush AxisBrush  = new SolidColorBrush(Color.FromRgb(51, 65, 85));

        public WaveformChart()
        {
            InitializeComponent();
        }

        private void OnChartSizeChanged(object sender, SizeChangedEventArgs e) => Refresh();

        public void Refresh(IReadOnlyList<WaveformSeries>? series)
        {
            _series = series;
            Refresh();
        }

        private IReadOnlyList<WaveformSeries>? _series;

        private void Refresh()
        {
            PART_Chart.Children.Clear();

            double w = PART_Chart.ActualWidth;
            double h = PART_Chart.ActualHeight;
            if (w < 10 || h < 10) return;

            double plotW = w - PadLeft - PadRight;
            double plotH = h - PadTop  - PadBottom;

            // ── 데이터 범위 ───────────────────────────────────────
            double maxT = 0;
            double maxV = 40;

            if (_series != null)
            {
                foreach (var s in _series.Where(s => s.IsVisible && s.Points.Count > 0))
                {
                    maxT = Math.Max(maxT, s.Points.Max(p => p.T));
                    maxV = Math.Max(maxV, s.Points.Max(p => p.V));
                }
            }

            if (maxT <= 0) maxT = 60;
            maxT = Math.Ceiling(maxT / 10.0) * 10 + 2;
            maxV = Math.Ceiling(maxV / 5.0) * 5;

            // ── 그리드 + 레이블 ───────────────────────────────────
            DrawHGrid(plotW, plotH, 0, maxV);
            DrawVGrid(plotW, plotH, maxT);

            // ── 시리즈 ────────────────────────────────────────────
            if (_series != null)
            {
                foreach (var s in _series.Where(s => s.IsVisible && s.Points.Count > 0))
                    DrawSeries(s, plotW, plotH, 0, maxV, maxT);
            }

            // ── 축 선 ─────────────────────────────────────────────
            Add(new Line { X1 = PadLeft, Y1 = PadTop, X2 = PadLeft, Y2 = PadTop + plotH,
                Stroke = AxisBrush, StrokeThickness = 1.5 });
            Add(new Line { X1 = PadLeft, Y1 = PadTop + plotH, X2 = PadLeft + plotW, Y2 = PadTop + plotH,
                Stroke = AxisBrush, StrokeThickness = 1.5 });
        }

        // ─────────────────────────────────────────────────────────────────
        private void DrawHGrid(double plotW, double plotH, double minV, double maxV)
        {
            int step = 5;
            for (double v = minV; v <= maxV + 0.01; v += step)
            {
                double y = PadTop + plotH * (1.0 - (v - minV) / (maxV - minV));

                Add(new Line { X1 = PadLeft, X2 = PadLeft + plotW, Y1 = y, Y2 = y,
                    Stroke = GridBrush, StrokeThickness = 1 });

                var lbl = new TextBlock { Text = v.ToString("F0"), FontSize = 9.5,
                    Foreground = LabelBrush, TextAlignment = TextAlignment.Right, Width = PadLeft - 4 };
                Canvas.SetLeft(lbl, 0);
                Canvas.SetTop(lbl, y - 7);
                PART_Chart.Children.Add(lbl);
            }
        }

        private void DrawVGrid(double plotW, double plotH, double maxT)
        {
            double step = NiceStep(maxT, 9);
            for (double t = 0; t <= maxT + step * 0.01; t += step)
            {
                double x = PadLeft + plotW * (t / maxT);

                Add(new Line { X1 = x, X2 = x, Y1 = PadTop, Y2 = PadTop + plotH,
                    Stroke = GridBrush, StrokeThickness = 1 });

                var lbl = new TextBlock { Text = t.ToString("F0"), FontSize = 9.5, Foreground = LabelBrush };
                Canvas.SetLeft(lbl, x - 8);
                Canvas.SetTop(lbl, PadTop + plotH + 3);
                PART_Chart.Children.Add(lbl);
            }
        }

        private void DrawSeries(WaveformSeries s, double plotW, double plotH,
                                 double minV, double maxV, double maxT)
        {
            var poly = new Polyline
            {
                Stroke          = s.Stroke,
                StrokeThickness = s.StrokeThickness,
                StrokeDashArray = s.DashArray,
                StrokeLineJoin  = PenLineJoin.Round,
            };

            foreach (var (t, v) in s.Points)
            {
                double x = PadLeft + plotW * (t / maxT);
                double y = PadTop  + plotH * (1.0 - (v - minV) / (maxV - minV));
                poly.Points.Add(new Point(x, y));
            }

            PART_Chart.Children.Add(poly);
        }

        private void Add(UIElement el) => PART_Chart.Children.Add(el);

        private static double NiceStep(double range, int targetSteps)
        {
            double rough = range / targetSteps;
            double[] niceVals = { 1, 2, 5, 10, 20, 25, 50, 100 };
            foreach (var n in niceVals)
                if (rough <= n) return n;
            return niceVals[^1];
        }
    }
}
