using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>밸브 보타이 심볼 색상 (열림=시안, 닫힘=어두운 회색)</summary>
    public class BoolToValveBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush OpenBrush   = new(Color.FromRgb(0x4F, 0xC3, 0xF7));
        private static readonly SolidColorBrush ClosedBrush = new(Color.FromRgb(0x3F, 0x3F, 0x5E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
            => (value is bool b && b) ? OpenBrush : ClosedBrush;

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
