using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>bool → 밸브 상태 색상 (Open=녹색, Closed=어두운 회색)</summary>
    public class ValveStateToBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush OpenBrush   = new(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush ClosedBrush = new(Color.FromRgb(0x4F, 0x4F, 0x6E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? OpenBrush : ClosedBrush;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
