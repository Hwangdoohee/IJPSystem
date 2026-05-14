using IJPSystem.Platform.Common.Enums;
using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.HMI.Simulation;
using IJPSystem.Platform.HMI.Simulation.Models;
using System;
using System.Collections.ObjectModel;
using System.IO;
using System.Threading.Tasks;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.ViewModels
{
    public class SimulationViewModel : ViewModelBase
    {
        private readonly MainViewModel _mainVM;
        private readonly ScenarioRunner _runner;

        // 솔루션 루트의 scenarios/ 폴더. Phase 1 한정 절대 경로 — 추후 Content/CopyToOutput 으로 정식화.
        private readonly string _scenarioFolder =
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "..", "scenarios"));

        public ObservableCollection<ScenarioItem> Scenarios { get; } = new();

        private ScenarioItem? _selected;
        public ScenarioItem? Selected
        {
            get => _selected;
            set
            {
                if (SetProperty(ref _selected, value))
                    CommandManager.InvalidateRequerySuggested();
            }
        }

        private string _lastResult = "";
        public string LastResult
        {
            get => _lastResult;
            private set => SetProperty(ref _lastResult, value);
        }

        public string ScenarioFolderPath => _scenarioFolder;

        public ICommand RunCommand    { get; }
        public ICommand RunAllCommand { get; }
        public ICommand ReloadCommand { get; }

        public SimulationViewModel(MainViewModel mainVM)
        {
            _mainVM = mainVM;
            _runner = new ScenarioRunner(mainVM);

            RunCommand    = new RelayCommand(async _ => await RunSelected(),  _ => Selected != null);
            RunAllCommand = new RelayCommand(async _ => await RunAll(),       _ => Scenarios.Count > 0);
            ReloadCommand = new RelayCommand(_ => Reload());

            Reload();
        }

        private void Reload()
        {
            Scenarios.Clear();
            try
            {
                foreach (var (path, def) in ScenarioParser.LoadAll(_scenarioFolder))
                    Scenarios.Add(new ScenarioItem(path, def));
                _mainVM.AddLog($"[SIM] 시나리오 {Scenarios.Count}개 로드: {_scenarioFolder}", LogLevel.Info);
            }
            catch (Exception ex)
            {
                _mainVM.AddLog($"[SIM] 시나리오 로드 실패: {ex.Message}", LogLevel.Error);
            }
            CommandManager.InvalidateRequerySuggested();
        }

        private async Task RunSelected()
        {
            if (Selected == null) return;
            await RunOne(Selected);
            LastResult = $"{Selected.Def.Name} → {Selected.Status}";
        }

        private async Task RunAll()
        {
            int pass = 0, fail = 0;
            foreach (var item in Scenarios)
            {
                Selected = item;
                await RunOne(item);
                if (item.Status.StartsWith("PASS")) pass++; else fail++;
            }
            LastResult = $"전체 {pass + fail}건 — PASS {pass} / FAIL {fail}";
        }

        private async Task RunOne(ScenarioItem item)
        {
            _mainVM.AddLog($"[SIM] {item.Def.Name} — 실행", LogLevel.Info);
            item.Status = "RUNNING...";
            var r = await _runner.RunAsync(item.Def);
            item.Status = r.IsPass ? $"PASS ({r.ElapsedMs}ms)" : $"FAIL — {r.Message}";
            _mainVM.AddLog($"[SIM] {item.Def.Name} — {item.Status}",
                            r.IsPass ? LogLevel.Success : LogLevel.Error);
        }
    }

    public class ScenarioItem : ViewModelBase
    {
        public string      Path { get; }
        public ScenarioDef Def  { get; }

        public ScenarioItem(string path, ScenarioDef def)
        {
            Path   = path;
            Def    = def;
            Status = "";
        }

        private string _status = "";
        public string Status
        {
            get => _status;
            set => SetProperty(ref _status, value);
        }
    }
}
