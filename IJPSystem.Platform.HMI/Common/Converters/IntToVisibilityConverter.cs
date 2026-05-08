using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>
    /// 정수 값(보통 리스트의 Count)을 Visibility 상태로 변환합니다.
    /// 0이면 숨기고, 0보다 크면 보여줍니다.
    /// </summary>
    public class IntToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value is int count)
            {
                // 숫자가 0보다 크면 보임, 아니면 숨김
                return count > 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            return Visibility.Collapsed;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}