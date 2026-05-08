using System;
using System.Globalization;
using System.Windows.Data;

namespace IJPSystem.Platform.HMI.Common.Converters // 하위 폴더 경로 포함
{
    // IMultiValueConverter 구현
    // → 여러 개의 값을 하나의 값으로 변환할 때 사용
    public class ProgressToWidthConverter : IMultiValueConverter//"진행률(%)을 실제 UI 너비(px)로 바꿔주는 변환기"
    {
        // 여러 값 → 하나의 값으로 변환
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            // values[0]: 현재 진행률 (0 ~ 100)
            // values[1]: 전체 컨트롤 너비 (ActualWidth)

            // 값이 부족하거나 null이면 0 반환
            if (values.Length < 2 || values[0] == null || values[1] == null)
                return 0.0;

            // 문자열 → double 변환 시도
            if (double.TryParse(values[0].ToString(), out double progress) &&
                double.TryParse(values[1].ToString(), out double totalWidth))
            {
                // 진행률 (%)을 실제 픽셀 너비로 변환
                // 예: 50% * 200px = 100px
                return (progress / 100.0) * totalWidth;
            }

            // 변환 실패 시 0 반환
            return 0.0;
        }

        // UI → ViewModel (사용 안함)
        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}