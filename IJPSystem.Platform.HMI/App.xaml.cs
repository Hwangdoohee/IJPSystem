using IJPSystem.Drivers.IO;
using IJPSystem.Drivers.Motion;
using IJPSystem.Drivers.Vision;
using IJPSystem.Machines.Inkjet5G;
using IJPSystem.Platform.Common.Constants;
using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Domain.Interfaces;
using IJPSystem.Platform.Domain.Models.Motion;
using IJPSystem.Platform.HMI.ViewModels;
using IJPSystem.Platform.HMI.Views;
using IJPSystem.Platform.Infrastructure.Config;
using System;
using System.Threading.Tasks;
using System.Windows;

namespace IJPSystem.Platform.HMI
{
    public partial class App : System.Windows.Application
    {
        private IMachine? _machine;

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);

            // SplashWindow 즉시 표시 (이후 머신/드라이버 초기화 진행 상황 단계별 표시)
            var splashVM = new SplashViewModel();
            var splash   = new SplashWindow { DataContext = splashVM };
            splash.Show();

            try
            {
                var loader = new ConfigLoader();

                var appSettings = await splashVM.RunStepAsync(
                    "System Configuration", "AppConfig.json 로드",
                    () => loader.LoadAppSettings(GetConfigPath("AppConfig.json")));

                var ioDriver = await splashVM.RunStepAsync(
                    "I/O Driver", "Virtual I/O 드라이버 연결",
                    InitializeIODriver);

                var motionDriver = await splashVM.RunStepAsync(
                    "Motion Driver", "Virtual Motion 드라이버 연결",
                    InitializeMotionDriver);

                var visionDriver = await splashVM.RunStepAsync(
                    "Vision Driver", "Virtual Vision 드라이버 연결",
                    InitializeVisionDriver);

                _machine = await splashVM.RunStepAsync(
                    "Machine Setup", "InkjetMachine 초기화 + Motor Config 로드",
                    () => appSettings.MachineType.ToUpper() switch
                    {
                        "INKJET5G" => CreateInkjet5G(loader, ioDriver, motionDriver, visionDriver),
                        _ => throw new NotSupportedException($"Unsupported: {appSettings.MachineType}"),
                    });

                splashVM.MachineName = _machine.MachineName.ToUpper();

                // MainViewModel 은 DispatcherTimer 등을 만들기 때문에 UI 스레드에서 생성
                var mainVM = await splashVM.RunStepAsync(
                    "HMI 준비", "메인 ViewModel 구성 + 화면 진입",
                    () =>
                    {
                        var controller = new InkjetController(_machine);
                        return new MainViewModel(controller);
                    },
                    background: false);

                // 마지막 ✓ 잠깐 보여주기
                await Task.Delay(350);

                new MainWindow { DataContext = mainVM }.Show();
                splash.Close();
            }
            catch (Exception ex)
            {
                splash.Close();
                MessageBox.Show($"Startup failed: {ex.Message}");
                Shutdown();
            }
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // 모든 종료 경로(메뉴 EXIT / X 버튼 / Alt+F4 / Shutdown)의 단일 정리 지점
            try { _machine?.Terminate(); }
            catch { /* 종료 중 예외는 삼킴 */ }

            base.OnExit(e);
        }

        private IMachine CreateMachine()
        {
            var loader = new ConfigLoader();
            var appSettings = loader.LoadAppSettings(GetConfigPath("AppConfig.json"));
            var ioDriver     = InitializeIODriver();
            var motionDriver = InitializeMotionDriver();
            var visionDriver = InitializeVisionDriver();

            return appSettings.MachineType.ToUpper() switch
            {
                "INKJET5G" => CreateInkjet5G(loader, ioDriver, motionDriver, visionDriver),
                _ => throw new NotSupportedException($"Unsupported: {appSettings.MachineType}")
            };
        }

        private IMachine CreateInkjet5G(ConfigLoader loader, IIODriver io, IMotionDriver motion, IVisionDriver vision)
        {
            var machine = new InkjetMachine(io, motion, vision);
            machine.Config = loader.LoadMotionConfig(GetConfigPath(AppConstants.MotorConfigFile))
                             ?? new MotionAxisRoot();
            machine.Initialize();
            return machine;
        }

        
        
        private IIODriver InitializeIODriver()
        { 
            string path = GetConfigPath(AppConstants.IoConfigFile);
            var loader = new ConfigLoader();
            var ioConfig = loader.LoadIOConfig(path);

            var ioDriver = new VirtualIODriver();
            ioDriver.Initialize(ioConfig.GetAllDevices());

            return ioDriver;
        }

        private IMotionDriver InitializeMotionDriver()
        {
            string path = GetConfigPath(AppConstants.MotorConfigFile);
            var loader = new ConfigLoader();
            var motionConfig = loader.LoadMotionConfig(path);

            var motionDriver = new VirtualMotionDriver(); 
            if (motionConfig?.MotionAxisList != null)
            {
                motionDriver.Initialize(motionConfig.MotionAxisList);
            }

            return motionDriver;
        }
        private IVisionDriver InitializeVisionDriver()
        {
            string path = GetConfigPath(AppConstants.VisionConfigFile);
            var loader = new ConfigLoader();
            var root = loader.LoadVisionConfig(path);

            var visionDriver = new VirtualVisionDriver();
            visionDriver.Initialize(root.VisionCameraList);

            return visionDriver;
        }
        private static string GetConfigPath(string fileName) => PathUtils.GetConfigPath(fileName);
    }
}