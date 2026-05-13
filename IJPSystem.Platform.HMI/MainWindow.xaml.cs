using IJPSystem.Platform.HMI.Common;
using IJPSystem.Platform.HMI.ViewModels;
using IJPSystem.Platform.HMI.Views;
using static IJPSystem.Platform.HMI.Common.Loc;
using System;
using System.ComponentModel;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace IJPSystem.Platform.HMI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            this.Loaded += MainWindow_Loaded;
        }
        private void MainWindow_Loaded(object sender, RoutedEventArgs e)
        {
            // [중요] UI 렌더링이 완전히 끝날 때까지 한 타이밍 쉽니다.
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var viewModel = this.DataContext as MainViewModel;

                // 1. DataContext 체크 (디버깅용)
                if (viewModel == null)
                {
                    MessageBox.Show("MainViewModel을 찾을 수 없습니다. App.xaml.cs를 확인하세요.");
                    return;
                }

                // 2. 로그인 창 띄우기
                //var loginWin = new LoginWindow();
                //loginWin.Owner = this; // 메인 화면 중앙에 띄우기 위해 설정

                //if (loginWin.ShowDialog() == true)
                //{
                //    // 로그인 성공 시 권한 설정 및 로그 기록
                //viewModel.CurrentUserRole = loginWin.ResultRole;
                //viewModel.AddLog(T("Log_LoginSuccess", viewModel.CurrentUserRole), LogLevel.Success);
                ////}
                ////else
                ////{
                ////    // 로그인 취소 시 프로그램 즉시 종료
                ////    Application.Current.Shutdown();
                ////}
            }), DispatcherPriority.ContextIdle); // 가장 낮은 우선순위로 실행 (화면 다 그려진 후)
        }
        // 로그가 추가될 때마다 스크롤을 끝으로 내리는 핸들러
        private void LogScrollViewer_ScrollChanged(object sender, ScrollChangedEventArgs e)
        {
            if (e.ExtentHeightChange > 0)
            {
                //(sender as ScrollViewer)?.ScrollToEnd();
                var scrollViewer = (ScrollViewer)sender;
                scrollViewer.ScrollToEnd();
            }
        }

        // 모든 종료 경로(메뉴 EXIT / X 버튼 / Alt+F4)의 단일 확인 지점
        private void MainWindow_Closing(object? sender, CancelEventArgs e)
        {
            var result = MessageBox.Show(T("Msg_ExitConfirm"), T("Msg_ExitTitle"),
                MessageBoxButton.YesNo, MessageBoxImage.Question);

            if (result != MessageBoxResult.Yes)
            {
                e.Cancel = true;
                return;
            }


            (DataContext as MainViewModel)?.OnApplicationClosing();
            // 드라이버 정리(IO/Motion/Vision)는 App.OnExit가 처리
        }
    }
}