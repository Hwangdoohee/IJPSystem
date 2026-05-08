using System;
using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;

namespace IJPSystem.Platform.HMI.Common.Converters
{
    // IValueConverter 구현
    // → LogLevel(enum)을 UI 색상(Brush)으로 변환하는 클래스
    public class LogLevelToColorConverter : IValueConverter//"LogLevel(enum)을 색상으로 바꿔주는 변환기"
    {
        // ViewModel → UI로 값이 전달될 때 실행
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            // value가 LogLevel 타입인지 확인
            if (value is LogLevel level)
            {
                // level 값에 따라 색상 반환 (switch 표현식 사용)
                return level switch
                {
                    LogLevel.Info => new SolidColorBrush(Color.FromRgb(96, 165, 250)),    // 정보: 연파랑
                    LogLevel.Success => new SolidColorBrush(Color.FromRgb(74, 222, 128)), // 성공: 연초록
                    LogLevel.Warning => new SolidColorBrush(Color.FromRgb(251, 191, 36)), // 경고: 노랑
                    LogLevel.Error => new SolidColorBrush(Color.FromRgb(248, 113, 113)),  // 에러: 연빨강
                    LogLevel.Fatal => new SolidColorBrush(Color.FromRgb(225, 29, 72)),    // 치명적: 진빨강

                    // 정의되지 않은 값은 기본 회색
                    _ => new SolidColorBrush(Color.FromRgb(148, 163, 184))
                };
            }

            // LogLevel이 아닐 경우 기본 회색
            return new SolidColorBrush(Colors.Gray);
        }

        // UI → ViewModel 변환 (사용 안함)
        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
            => throw new NotImplementedException();
    }
}