using IJPSystem.Platform.Domain.Enums;
using System;
using System.Globalization;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>LevelStatus → 탱크 비주얼 액체 높이 (px)</summary>
    public class LevelStatusToHeightConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is LevelStatus s)
            {
                return s switch
                {
                    LevelStatus.HH    => 130.0,
                    LevelStatus.High  => 110.0,
                    LevelStatus.Set   => 80.0,
                    LevelStatus.Low   => 40.0,
                    LevelStatus.Empty => 10.0,
                    _                 => 0.0,
                };
            }
            return 0.0;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}
