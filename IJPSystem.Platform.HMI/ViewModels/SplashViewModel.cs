using IJPSystem.Platform.Domain.Common;
using IJPSystem.Platform.HMI.Common.Models;
using System;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows;

namespace IJPSystem.Platform.HMI.ViewModels
{
    // SplashWindow 의 DataContext — 초기화 단계 진행 표시
    public class SplashViewModel : ViewModelBase
    {
        public ObservableCollection<InitStep> InitSteps { get; } = new();

        private string _machineName = "INKJET HMI";
        public string MachineName
        {
            get => _machineName;
            set => SetProperty(ref _machineName, value);
        }

        // 단계 실행: 작업을 수행하면서 step 상태를 갱신. minMs 미만으로 끝나면 잠시 대기 (시각 피드백 보장)
        public async Task<T> RunStepAsync<T>(
            string name,
            string description,
            Func<T> action,
            bool background = true,
            int minMs = 200)
        {
            var step = new InitStep
            {
                Name        = name,
                Description = description,
                Status      = InitStepStatus.Running,
            };
            System.Windows.Application.Current.Dispatcher.Invoke(() => InitSteps.Add(step));

            var sw = Stopwatch.StartNew();
            try
            {
                T result;
                if (background)
                {
                    // 드라이버 등 IO 바운드 작업은 백그라운드 스레드에서 실행
                    result = await Task.Run(action).ConfigureAwait(true);
                }
                else
                {
                    // UI 스레드에 있어야 하는 작업 (DispatcherTimer 등)
                    await Task.Yield();
                    result = action();
                }

                int elapsed = (int)sw.ElapsedMilliseconds;
                if (elapsed < minMs) await Task.Delay(minMs - elapsed);

                step.Status = InitStepStatus.Done;
                return result;
            }
            catch (Exception ex)
            {
                step.Status       = InitStepStatus.Failed;
                step.ErrorMessage = ex.Message;
                throw;
            }
        }

        public Task RunStepAsync(
            string name,
            string description,
            Action action,
            bool background = true,
            int minMs = 200) =>
            RunStepAsync<object?>(name, description, () => { action(); return null; }, background, minMs);
    }
}
