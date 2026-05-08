using IJPSystem.Platform.Domain.Models.Alarm;
using IJPSystem.Platform.HMI.Common;
using IJPSystem.Platform.HMI.ViewModels;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using static IJPSystem.Platform.HMI.Common.Loc;

namespace IJPSystem.Platform.HMI.Views
{
    /// <summary>
    /// AlarmHistoryView.xaml에 대한 상호 작용 논리
    /// </summary>
    public partial class AlarmHistoryView : UserControl
    {
        public AlarmHistoryView()
        {
            InitializeComponent();

            var vm = this.DataContext as IJPSystem.Platform.HMI.ViewModels.AlarmViewModel;
            if (vm == null)
            {
                System.Diagnostics.Debug.WriteLine("CRITICAL: DataContext가 AlarmViewModel이 아닙니다!");
            }
            else
            {
                System.Diagnostics.Debug.WriteLine("SUCCESS: DataContext 연결 성공!");
            }
        }
        private void ExportButton_Click(object sender, RoutedEventArgs e)
        {
            if (DataContext is MainViewModel mainVm)
                mainVm.AlarmVM.ExecuteExportCsv();
        }

        private void AlarmRow_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            if (DataContext is not MainViewModel mainVm) return;
            if (AlarmDataGrid.SelectedItem is not AlarmModel selected) return;

            if (selected.IsCleared) return;

            var result = MessageBox.Show(
                T("Msg_AlarmResetRowConfirm", selected.AlarmCode, selected.AlarmName),
                T("Msg_AlarmResetRowTitle"),
                MessageBoxButton.YesNo,
                MessageBoxImage.Question);

            if (result == MessageBoxResult.Yes)
                mainVm.AlarmVM.ClearSingleAlarm(selected);

            // 행 선택 해제
            AlarmDataGrid.SelectedItem = null;
        }
    }

}
