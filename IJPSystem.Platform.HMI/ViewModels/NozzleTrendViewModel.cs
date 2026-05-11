using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Infrastructure.Repositories;
using LiveChartsCore;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    /// <summary>
    /// Drop Watcher 검사 이력의 SPC(통계적 공정 관리) 차트.
    /// X축 = 검사 시점, Y축 = Health Rate(%). 평균 + ±3σ 한계선 표시.
    /// </summary>
    public class NozzleTrendViewModel : ViewModelBase
    {
        private const int DefaultLoadCount = 200;

        public ISeries[] Series { get; private set; } = Array.Empty<ISeries>();
        public Axis[] XAxes { get; private set; } = Array.Empty<Axis>();
        public Axis[] YAxes { get; private set; } = Array.Empty<Axis>();
        public RectangularSection[] Sections { get; private set; } = Array.Empty<RectangularSection>();

        private string _statusText = "데이터 로드 중...";
        public string StatusText
        {
            get => _statusText;
            private set => SetProperty(ref _statusText, value);
        }

        private string _spcSummary = "";
        public string SpcSummary
        {
            get => _spcSummary;
            private set => SetProperty(ref _spcSummary, value);
        }

        public ICommand RefreshCommand { get; }

        public NozzleTrendViewModel()
        {
            RefreshCommand = new RelayCommand(_ => Reload());
            Reload();
        }

        private void Reload()
        {
            var snapshots = NozzleHealthRepository.GetRecent(DefaultLoadCount);
            if (snapshots.Count == 0)
            {
                StatusText = "검사 이력이 없습니다. Drop Watcher 에서 INSPECT ALL 을 실행하세요.";
                Series = Array.Empty<ISeries>();
                Sections = Array.Empty<RectangularSection>();
                XAxes = new[] { new Axis { Labels = Array.Empty<string>() } };
                YAxes = new[] { new Axis { MinLimit = 0, MaxLimit = 100, Name = "Health Rate (%)" } };
                NotifyChartChanged();
                return;
            }

            // 시계열 데이터
            double[] healthValues = snapshots.Select(s => s.HealthPct).ToArray();
            string[] timeLabels   = snapshots.Select(s => s.Time.ToString("HH:mm:ss")).ToArray();

            // SPC: 평균(CL) + 표준편차 → UCL/LCL = 평균 ± 3σ (Y축 0~100% 클램프)
            double mean   = healthValues.Average();
            double stdev  = StdDev(healthValues, mean);
            double ucl    = Math.Clamp(mean + 3 * stdev, 0, 100);
            double lcl    = Math.Clamp(mean - 3 * stdev, 0, 100);

            int violations = healthValues.Count(v => v < lcl || v > ucl);

            Series = new ISeries[]
            {
                new LineSeries<double>
                {
                    Name = "Health Rate",
                    Values = healthValues,
                    Stroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    Fill = new SolidColorPaint(SKColors.DodgerBlue.WithAlpha(40)),
                    GeometrySize = 6,
                    GeometryStroke = new SolidColorPaint(SKColors.DodgerBlue, 2),
                    GeometryFill = new SolidColorPaint(SKColors.White),
                },
            };

            // SPC 한계선 (가로 점선)
            Sections = new[]
            {
                HorizontalLine(mean, SKColors.LimeGreen, "CL"),
                HorizontalLine(ucl,  SKColors.OrangeRed, "UCL"),
                HorizontalLine(lcl,  SKColors.OrangeRed, "LCL"),
            };

            XAxes = new[]
            {
                new Axis
                {
                    Labels = timeLabels,
                    LabelsRotation = 45,
                    TextSize = 11,
                    Name = "검사 시점",
                }
            };
            YAxes = new[]
            {
                new Axis
                {
                    Name = "Health Rate (%)",
                    MinLimit = 0,
                    MaxLimit = 100,
                    TextSize = 12,
                }
            };

            StatusText = $"최근 {snapshots.Count}건 로드 — {snapshots[0].Time:yyyy-MM-dd HH:mm:ss} ~ {snapshots[^1].Time:yyyy-MM-dd HH:mm:ss}";
            SpcSummary = $"CL={mean:F2}%   UCL={ucl:F2}%   LCL={lcl:F2}%   σ={stdev:F2}%   이탈={violations}건";

            NotifyChartChanged();
        }

        private static RectangularSection HorizontalLine(double y, SKColor color, string label) => new()
        {
            Yi = y,
            Yj = y,
            Stroke = new SolidColorPaint
            {
                Color = color,
                StrokeThickness = 2,
                PathEffect = new DashEffect(new float[] { 6, 4 }),
            },
            Label = label,
            LabelPaint = new SolidColorPaint(color) { SKTypeface = SKTypeface.FromFamilyName("Segoe UI") },
            LabelSize = 11,
        };

        private static double StdDev(double[] values, double mean)
        {
            if (values.Length < 2) return 0;
            double sumSq = values.Sum(v => (v - mean) * (v - mean));
            return Math.Sqrt(sumSq / (values.Length - 1));
        }

        private void NotifyChartChanged()
        {
            OnPropertyChanged(nameof(Series));
            OnPropertyChanged(nameof(XAxes));
            OnPropertyChanged(nameof(YAxes));
            OnPropertyChanged(nameof(Sections));
        }
    }
}
