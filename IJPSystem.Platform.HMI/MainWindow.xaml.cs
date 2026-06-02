using IJPSystem.Platform.HMI.Common;
using IJPSystem.Platform.HMI.ViewModels;
using static IJPSystem.Platform.HMI.Common.Loc;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }
        // 로그가 추가될 때마다 스크롤을 끝으로 내리는 핸들러
        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
                (sender as ScrollViewer)?.ScrollToEnd();
        }

        // 모든 종료 경로(메뉴 EXIT / X 버튼 / Alt+F4)의 단일 확인 지점
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var vm = DataContext as MainViewModel;

            // 운전 중 종료 차단 — 메인 대시보드 Auto Print / Sequence·Pnid 화면의 Initialize·Purge 등
            // 어느 경로로 실행 중이든 데이터 유실·라인 정지 방지를 위해 종료를 막고 사유 안내
            if (vm?.IsOperationRunning == true)
            {
                Dialogs.Show(T("Msg_ExitBlockedRunning"), T("Msg_ExitBlockedTitle"),
                    MessageBoxButton.OK, MessageBoxImage.Warning);
                e.Cancel = true;
                return;
            }

            var result = Dialogs.Show(T("Msg_ExitConfirm"), T("Msg_ExitTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }


            vm?.OnApplicationClosing();
            // 드라이버 정리(IO/Motion/Vision)는 App.OnExit가 처리
        }
    }
}