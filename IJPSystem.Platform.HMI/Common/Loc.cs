using System.Windows;

namespace IJPSystem.Platform.HMI.Common
{
    /// <summary>
    /// ViewModel/Code-behind 에서 리소스 딕셔너리 문자열을 가져오는 헬퍼.
    /// System.Windows.Application.Current.Resources 에 로드된 언어 파일(ko-KR / en-US)을 그대로 사용합니다.
    /// </summary>
    public static class Loc
    {
        /// <summary>키에 해당하는 현재 언어 문자열을 반환합니다.</summary>
        public static string T(string key)
            => System.Windows.Application.Current?.TryFindResource(key) as string ?? key;

        /// <summary>키에 해당하는 문자열을 string.Format으로 포매팅합니다.</summary>
        public static string T(string key, params object[] args)
        {
            string template = T(key);
            try   { return string.Format(template, args); }
            catch { return template; }
        }
    }
}
