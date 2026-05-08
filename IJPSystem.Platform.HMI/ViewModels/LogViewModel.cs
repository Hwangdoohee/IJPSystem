using IJPSystem.Platform.Common.Constants;
using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.Infrastructure.Repositories;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    /// <summary>
    /// SystemLog DB(C:\Logs\SystemLog.db) 기반 로그 뷰어 VM.
    /// AlarmViewModel 과 동일한 필터/검색/Export 패턴을 따른다.
    /// </summary>
    public class LogViewModel : ViewModelBase
    {
        public ObservableCollection<SystemLogRepository.SystemLogEntry> Logs { get; } = new();

        public string[] LevelOptions { get; } =
            { "ALL", "Info", "Success", "Warning", "Error", "Fatal" };

        // ── 카테고리(Quick-Filter) 패턴 ─────────────────────────────────────
        // 모든 AddLog 메시지는 "[CAT] ..." 언어-중립 prefix 로 시작 (taxonomy:
        //   [NAV] [AUTH] [SEQ] [RECIPE] [ALARM] [MOTION] [IO] [VISION]
        //   [WAVEFORM] [PNID] [SYS]).
        // 카테고리 버튼은 그 prefix 들을 그대로 매칭하므로 다국어 영향 없음.
        private static readonly Dictionary<string, string[]> CategoryPatterns =
            new(StringComparer.OrdinalIgnoreCase)
        {
            // 로그인/권한 + 화면 전환
            ["LOGIN_NAV"] = new[] { "[AUTH]", "[NAV]" },

            // 시퀀스 (AutoPrint / Initialize / SequenceVM / PnidVM 자동 시퀀스)
            ["SEQ"]       = new[] { "[SEQ]" },

            // 수동 조작 (모터/IO/비전/웨이브폼/P&ID 수동 제어 모두)
            ["MANUAL"]    = new[] { "[MOTION]", "[IO]", "[VISION]", "[WAVEFORM]", "[PNID]" },

            // 알람 발생/해제
            ["ALARM"]     = new[] { "[ALARM]" },

            // ERROR 카테고리는 Level 기반(Error + Fatal) — patterns 사용 안 함
            ["ERROR"]     = Array.Empty<string>(),
        };

        private string _activeCategory = "ALL";
        public string ActiveCategory
        {
            get => _activeCategory;
            private set
            {
                if (SetProperty(ref _activeCategory, value))
                {
                    // 활성 카테고리 토글 표시 — 모든 IsCat* binding 을 갱신해야
                    // 비활성으로 전환되는 버튼의 트리거도 풀린다
                    OnPropertyChanged(nameof(IsCatAll));
                    OnPropertyChanged(nameof(IsCatLoginNav));
                    OnPropertyChanged(nameof(IsCatSeq));
                    OnPropertyChanged(nameof(IsCatManual));
                    OnPropertyChanged(nameof(IsCatAlarm));
                    OnPropertyChanged(nameof(IsCatError));
                }
            }
        }

        // 활성 카테고리 표시용 (버튼 하이라이트)
        public bool IsCatAll       => ActiveCategory == "ALL";
        public bool IsCatLoginNav  => ActiveCategory == "LOGIN_NAV";
        public bool IsCatSeq       => ActiveCategory == "SEQ";
        public bool IsCatManual    => ActiveCategory == "MANUAL";
        public bool IsCatAlarm     => ActiveCategory == "ALARM";
        public bool IsCatError     => ActiveCategory == "ERROR";

        private DateTime? _filterStartDate;
        public DateTime? FilterStartDate
        {
            get => _filterStartDate;
            set => SetProperty(ref _filterStartDate, value);
        }

        private DateTime? _filterEndDate;
        public DateTime? FilterEndDate
        {
            get => _filterEndDate;
            set => SetProperty(ref _filterEndDate, value);
        }

        private string _filterLevel = "ALL";
        public string FilterLevel
        {
            get => _filterLevel;
            set => SetProperty(ref _filterLevel, value);
        }

        private string _filterKeyword = string.Empty;
        public string FilterKeyword
        {
            get => _filterKeyword;
            set => SetProperty(ref _filterKeyword, value);
        }

        public int RecordCount => Logs.Count;

        public ICommand RefreshCommand     { get; }
        public ICommand SearchCommand      { get; }
        public ICommand ResetFilterCommand { get; }
        public ICommand ExportCsvCommand   { get; }
        public ICommand ClearAllCommand    { get; }
        public ICommand SetCategoryCommand { get; }

        public LogViewModel()
        {
            RefreshCommand     = new RelayCommand(_ => Refresh());
            SearchCommand      = new RelayCommand(_ => Refresh());
            ResetFilterCommand = new RelayCommand(_ => ResetFilters());
            ExportCsvCommand   = new RelayCommand(_ => ExportCsv());
            ClearAllCommand    = new RelayCommand(_ => ClearAll());
            SetCategoryCommand = new RelayCommand(p =>
            {
                ActiveCategory = (p as string) ?? "ALL";
                Refresh();
            });
        }

        public void Refresh()
        {
            string?   lvl      = string.Equals(FilterLevel, "ALL", StringComparison.OrdinalIgnoreCase)
                                 ? null : FilterLevel;
            string[]? levels   = null;
            string[]? patterns = null;

            // FilterEndDate 는 날짜만 선택되므로 23:59:59 까지 포함시킴
            DateTime? toInclusive = FilterEndDate?.Date.AddDays(1).AddTicks(-1);

            // ERROR 카테고리는 Level 기반(Error + Fatal). 이 경우 LEVEL 콤보 필터는 무시.
            if (string.Equals(ActiveCategory, "ERROR", StringComparison.OrdinalIgnoreCase))
            {
                levels = new[] { "Error", "Fatal" };
                lvl = null;
            }
            else if (!string.Equals(ActiveCategory, "ALL", StringComparison.OrdinalIgnoreCase) &&
                     CategoryPatterns.TryGetValue(ActiveCategory, out var arr) &&
                     arr.Length > 0)
            {
                patterns = arr;
            }

            var rows = SystemLogRepository.Query(
                from:     FilterStartDate?.Date,
                to:       toInclusive,
                level:    lvl,
                levels:   levels,
                keyword:  FilterKeyword,
                patterns: patterns);

            Logs.Clear();
            foreach (var r in rows) Logs.Add(r);
            OnPropertyChanged(nameof(RecordCount));
        }

        private void ResetFilters()
        {
            FilterStartDate = null;
            FilterEndDate   = null;
            FilterLevel     = "ALL";
            FilterKeyword   = string.Empty;
            ActiveCategory  = "ALL";
            Refresh();
        }

        private void ExportCsv()
        {
            if (Logs.Count == 0)
            {
                MessageBox.Show("내보낼 로그가 없습니다.", "Export",
                                MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            try
            {
                if (!Directory.Exists(AppConstants.LogFolder))
                    Directory.CreateDirectory(AppConstants.LogFolder);

                string fileName = $"SystemLog_{DateTime.Now.ToFileStamp()}.csv";
                string path     = Path.Combine(AppConstants.LogFolder, fileName);

                var sb = new StringBuilder();
                sb.AppendLine("Time,Level,Message");
                foreach (var l in Logs)
                {
                    string msg = (l.Message ?? "").Replace("\"", "\"\"");
                    sb.AppendLine($"{l.Time:yyyy-MM-dd HH:mm:ss.fff},{l.Level},\"{msg}\"");
                }
                File.WriteAllText(path, sb.ToString(), Encoding.UTF8);

                MessageBox.Show($"내보냈습니다:\n{path}", "Export",
                                MessageBoxButton.OK, MessageBoxImage.Information);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"내보내기 실패:\n{ex.Message}", "Export",
                                MessageBoxButton.OK, MessageBoxImage.Error);
            }
        }

        private void ClearAll()
        {
            var result = MessageBox.Show(
                "DB 의 모든 시스템 로그를 삭제합니다.\n계속하시겠습니까?\n\n(.txt 일별 파일은 삭제되지 않습니다)",
                "Clear System Log",
                MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;

            SystemLogRepository.DeleteAll();
            Refresh();
        }
    }
}
