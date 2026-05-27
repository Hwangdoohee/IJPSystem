using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Domain.Models.Config;

namespace IJPSystem.Platform.Infrastructure.Config
{
    // AppConfig.json 의 런타임 시스템 설정에 전역 접근하기 위한 정적 보유소.
    // Why: 도어 사용 유무 같은 시스템 토글은 여러 ViewModel 에서 동시에 참조/갱신되므로,
    //       각 VM 에 AppSettings 인스턴스를 따로 들고 다니면 동기화 문제가 생긴다.
    public static class AppSettingsService
    {
        private const string ConfigFile = "AppConfig.json";
        private static AppSettings _current = new AppSettings();
        private static readonly ConfigLoader _loader = new ConfigLoader();

        public static AppSettings Current => _current;

        public static void Initialize(AppSettings settings)
        {
            _current = settings ?? new AppSettings();
        }

        public static void Save()
        {
            _loader.SaveAppSettings(PathUtils.GetConfigPath(ConfigFile), _current);
        }
    }
}
