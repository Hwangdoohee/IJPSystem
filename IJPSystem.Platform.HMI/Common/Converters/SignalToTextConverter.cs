using System;
using System.Globalization;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter 구현
    // → bool 값을 "ON / OFF" 텍스트로 변환하는 클래스
    public class SignalToTextConverter : IValueConverter//"신호(bool)를 ON/OFF 문자열로 바꿔주는 변환기"
    {
        // ViewModel → UI 변환
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 bool인지 확인
            if (value is bool isSignal)
            {
                // true → ON, false → OFF
                return isSignal ? "ON" : "OFF";
            }

            // bool이 아닐 경우 기본값 OFF 반환
            return "OFF";
        }

        // UI → ViewModel (사용 안함)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}