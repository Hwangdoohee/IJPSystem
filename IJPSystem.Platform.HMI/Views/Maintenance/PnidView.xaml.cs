using System;
using System.Windows.Controls;

namespace IJPSystem.Platform.HMI.Views
{
    public partial class PnidView : UserControl
    {
        public PnidView()
        {
            InitializeComponent();
            // 화면 전환 시 ViewModel 타이머 정리
            Unloaded += (_, __) => (DataContext as IDisposable)?.Dispose();
        }
    }
}
