using System;
using System.Globalization;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter를 구현한 클래스
    // → bool 값을 UI에서 표시할 문자열로 변환하는 역할
    public class ConnectionToTextConverter : IValueConverter //"bool 값을 상태 텍스트로 바꿔주는 변환기"
    {
        // ViewModel → UI로 값이 전달될 때 실행
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 bool인지 확인 후 isConnected에 담음
            if (value is bool isConnected)
            {
                // true면 ONLINE, false면 OFFLINE 문자열 반환
                return isConnected ? "PLC: ONLINE" : "PLC: OFFLINE";
            }

            // bool이 아닌 경우 기본 상태 표시
            return "PLC: UNKNOWN";
        }

        // UI → ViewModel로 값이 다시 들어갈 때 사용
        // 여기서는 구현하지 않았기 때문에 예외 발생
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}