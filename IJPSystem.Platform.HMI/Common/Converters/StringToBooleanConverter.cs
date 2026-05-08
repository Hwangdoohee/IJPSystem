using System;
using System.Globalization;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter 구현
    // → 문자열 값을 특정 문자열(parameter)과 비교해서 bool로 변환//RadioButton에서 가장 많이 사용
    public class StringToBooleanConverter : IValueConverter//"문자열이 특정 값과 같은지 비교해서 bool로 바꿔주는 변환기" 
    {
        // ViewModel → UI 변환
        public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // value 또는 parameter가 null이면 false 반환
            if (value == null || parameter == null)
                return false;

            // value와 parameter를 문자열로 변환
            string? valStr = value.ToString();
            string? paramStr = parameter.ToString();

            // null 체크 후 문자열 비교 (대소문자 무시)
            return valStr != null &&
                   valStr.Equals(paramStr, StringComparison.OrdinalIgnoreCase);
        }

        // UI → ViewModel 변환
        public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        {
            // 체크(true) 상태이고 parameter가 있으면 해당 문자열 반환
            if (value is bool b && b && parameter != null)
            {
                return parameter.ToString() ?? Binding.DoNothing;
            }

            // 그 외는 아무 동작 안함
            return Binding.DoNothing;
        }
    }
}