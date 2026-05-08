using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    public class BooleanToVisibilityConverter : IValueConverter
    {
        // bool 값을 Visibility로 변환 (ViewModel -> View)
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is bool boolValue)
            {
                // true면 Visible, false면 Collapsed(공간까지 삭제)
                return boolValue ? Visibility.Visible : Visibility.Collapsed;
            }
            return Visibility.Collapsed;
        }

        // Visibility를 bool로 변환 (View -> ViewModel, 거의 사용 안 함)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is Visibility visibility)
            {
                return visibility == Visibility.Visible;
            }
            return false;
        }
    }
}