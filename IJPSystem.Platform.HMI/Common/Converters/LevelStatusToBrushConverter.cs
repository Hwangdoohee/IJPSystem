using IJPSystem.Platform.Domain.Enums;
using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>LevelStatus → 색상 (라벨 배경)</summary>
    public class LevelStatusToBrushConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LevelStatus s)
            {
                return s switch
                {
                    LevelStatus.HH    => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),
                    LevelStatus.High  => new SolidColorBrush(Color.FromRgb(0xFF, 0xC1, 0x07)),
                    LevelStatus.Set   => new SolidColorBrush(Color.FromRgb(0x4C, 0xAF, 0x50)),
                    LevelStatus.Low   => new SolidColorBrush(Color.FromRgb(0xFF, 0x98, 0x00)),
                    LevelStatus.Empty => new SolidColorBrush(Color.FromRgb(0xF4, 0x43, 0x36)),
                    _                 => new SolidColorBrush(Color.FromRgb(0x9E, 0x9E, 0x9E)),
                };
            }
            return new SolidColorBrush(Colors.Gray);
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
