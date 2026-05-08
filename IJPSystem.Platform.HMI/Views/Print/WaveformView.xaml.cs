using IJPSystem.Platform.HMI.ViewModels;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Views
{
    public partial class WaveformView : UserControl
    {
        public WaveformView()
        {
            InitializeComponent();
            DataContextChanged += OnDataContextChanged;
        }

        private WaveformViewModel? _vm;

        private void OnDataContextChanged(object sender,
            System.Windows.DependencyPropertyChangedEventArgs e)
        {
            if (_vm != null)
                _vm.ChartDataChanged -= OnChartDataChanged;

            _vm = e.NewValue as WaveformViewModel;

            if (_vm != null)
            {
                _vm.ChartDataChanged += OnChartDataChanged;
                // 자동 로드된 데이터가 있으면 즉시 그리기
                WaveChart.Refresh(_vm.AllSeries);
            }
        }

        private void OnChartDataChanged()
        {
            if (_vm != null)
                WaveChart.Refresh(_vm.AllSeries);
        }
    }
}
