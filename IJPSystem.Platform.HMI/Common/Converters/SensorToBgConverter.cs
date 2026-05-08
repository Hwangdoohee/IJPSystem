using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters // 프로젝트 네임스페이스에 맞게 수정하세요
{
    public class SensorToBgConverter : IValueConverter
    {
        // 센서 감지 시 배경색 (주황색/Amber)
        private static readonly SolidColorBrush DetectedBrush = new SolidColorBrush(Color.FromRgb(245, 158, 11));

        // 센서 미감지 시 배경색 (어두운 회색)
        private static readonly SolidColorBrush NormalBrush = new SolidColorBrush(Color.FromRgb(30, 41, 59));

        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool isDetected && isDetected)
            {
                return DetectedBrush;
            }
            return NormalBrush;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}