using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    /// <summary>
    /// 정수 값(알람 개수)이 0일 때만 Visible(보임)을 반환하고,
    /// 1 이상이면 Collapsed(공간까지 숨김)를 반환합니다.
    /// </summary>
    public class IntToInverseVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // 입력값이 정수인지 확인
            if (value is int count)
            {
                // ✅ 알람이 0개일 때만 보여주고, 1개라도 있으면 숨깁니다.
                return count == 0 ? Visibility.Visible : Visibility.Collapsed;
            }

            // 기본적으로는 보이게 설정 (에러 방지)
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}