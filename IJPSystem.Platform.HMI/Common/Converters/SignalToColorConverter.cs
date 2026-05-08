using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter 구현
    // → bool 값을 UI 색상(Brush)으로 변환하는 클래스
    // 클래스 이름은 XAML에서 사용되므로 반드시 동일해야 함
    public class SignalToColorConverter : IValueConverter//"신호(bool)를 색상으로 바꿔주는 변환기"
    {
        // ViewModel → UI 변환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 bool인지 확인
            if (value is bool isOn)
            {
                // ON(true) → 초록색
                // OFF(false) → 회색
                return isOn
                    ? new SolidColorBrush((Color)ColorConverter.ConvertFromString("#22C55E"))
                    : new SolidColorBrush((Color)ColorConverter.ConvertFromString("#475569"));
            }

            // bool이 아닐 경우 투명 처리
            return Brushes.Transparent;
        }

        // UI → ViewModel (사용 안함)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            return Binding.DoNothing;
        }
    }
}