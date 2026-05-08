using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter를 구현한 클래스
    // → 바인딩된 데이터를 UI에서 사용할 값으로 변환할 때 사용
    public class AlarmToColorConverter : IValueConverter
    {
        // ViewModel → UI로 값이 전달될 때 실행되는 메서드
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 bool 타입인지 확인하고 isError 변수에 담음
            if (value is bool isError)
            {
                // 에러(true)면 빨간색, 정상(false)이면 초록색
                string colorCode = isError ? "#EF4444" : "#22C55E";

                // 문자열 색상코드를 Color로 변환 후 Brush로 만들어 반환
                return new SolidColorBrush((Color)ColorConverter.ConvertFromString(colorCode));
            }

            // value가 bool이 아닐 경우 기본값 (회색) 반환
            return Brushes.Gray;
        }

        // UI → ViewModel로 값이 다시 들어갈 때 사용하는 메서드
        // 여기서는 사용하지 않기 때문에 아무것도 하지 않음
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}