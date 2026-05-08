using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>
    /// (IsActive, AlarmMode) → 센서 색상.
    /// MultiBinding 전용 — ConverterParameter에 Binding을 못 쓰는 문제 회피용.
    ///   - IsActive=false        → 비활성 회색
    ///   - IsActive=true, Alarm=false → 녹색 (정상 점등)
    ///   - IsActive=true, Alarm=true  → 빨강 (알람 점등)
    /// </summary>
    public class SensorBrushMultiConverter : IMultiValueConverter
    {
        private static readonly SolidColorBrush ActiveOk    = new(Color.FromRgb(0x4C, 0xAF, 0x50));
        private static readonly SolidColorBrush ActiveAlarm = new(Color.FromRgb(0xF4, 0x43, 0x36));
        private static readonly SolidColorBrush Inactive    = new(Color.FromRgb(0x3F, 0x3F, 0x5E));

        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            bool active    = values.Length > 0 && values[0] is bool a && a;
            bool alarmMode = values.Length > 1 && values[1] is bool m && m;

            if (!active) return Inactive;
            return alarmMode ? ActiveAlarm : ActiveOk;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
