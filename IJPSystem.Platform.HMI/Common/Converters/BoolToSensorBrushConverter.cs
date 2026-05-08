using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>
    /// 센서 상태(bool) → 색상.
    /// ConverterParameter:
    ///   "Alarm"    → 점등 시 빨강 (알람 의미)
    ///   "Inverted" → 의미 반전 (Empty 센서: false면 알람으로 처리)
    ///   기본값     → 점등 시 녹색 (정상 의미)
    /// </summary>
    public class BoolToSensorBrushConverter : IValueConverter
    {
        private static readonly SolidColorBrush ActiveOk    = new(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush ActiveAlarm = new(Color.FromRgb(0xF4, 0x43, 0x36));
        private static readonly SolidColorBrush Inactive    = new(Color.FromRgb(0x3F, 0x3F, 0x5E));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is not bool active) return Inactive;

            bool alarmMode = false;
            if (parameter is string s)
            {
                if (s == "Alarm")    alarmMode = true;
                if (s == "Inverted") active = !active;
            }
            else if (parameter is bool pb) alarmMode = pb;

            if (!active) return Inactive;
            return alarmMode ? ActiveAlarm : ActiveOk;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
