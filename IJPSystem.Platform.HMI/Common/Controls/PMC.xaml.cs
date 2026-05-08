using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace IJPSystem.Platform.HMI.Common.Controls
{
    /// <summary>P&amp;ID Meniscus Pressure Controller. 200×148.
    /// 양압(P+, Purge)/음압(P-, Meniscus) 각각 PV(읽기)/SV(쓰기) + SET 버튼 노출.</summary>
    public partial class PMC : UserControl
    {
        public PMC() => InitializeComponent();

        // ── 양압 (P+, Purge) ──
        public double PressurePV
        {
            get => (double)GetValue(PressurePVProperty);
            set => SetValue(PressurePVProperty, value);
        }
        public double PressureSV
        {
            get => (double)GetValue(PressureSVProperty);
            set => SetValue(PressureSVProperty, value);
        }
        public ICommand SetPressureCommand
        {
            get => (ICommand)GetValue(SetPressureCommandProperty);
            set => SetValue(SetPressureCommandProperty, value);
        }

        // ── 음압 (P-, Meniscus) ──
        public double VacuumPV
        {
            get => (double)GetValue(VacuumPVProperty);
            set => SetValue(VacuumPVProperty, value);
        }
        public double VacuumSV
        {
            get => (double)GetValue(VacuumSVProperty);
            set => SetValue(VacuumSVProperty, value);
        }
        public ICommand SetVacuumCommand
        {
            get => (ICommand)GetValue(SetVacuumCommandProperty);
            set => SetValue(SetVacuumCommandProperty, value);
        }

        public static readonly DependencyProperty PressurePVProperty =
            DependencyProperty.Register(nameof(PressurePV), typeof(double), typeof(PMC), new PropertyMetadata(0.0));
        public static readonly DependencyProperty PressureSVProperty =
            DependencyProperty.Register(nameof(PressureSV), typeof(double), typeof(PMC),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty SetPressureCommandProperty =
            DependencyProperty.Register(nameof(SetPressureCommand), typeof(ICommand), typeof(PMC), new PropertyMetadata(null));

        public static readonly DependencyProperty VacuumPVProperty =
            DependencyProperty.Register(nameof(VacuumPV), typeof(double), typeof(PMC), new PropertyMetadata(0.0));
        public static readonly DependencyProperty VacuumSVProperty =
            DependencyProperty.Register(nameof(VacuumSV), typeof(double), typeof(PMC),
                new FrameworkPropertyMetadata(0.0, FrameworkPropertyMetadataOptions.BindsTwoWayByDefault));
        public static readonly DependencyProperty SetVacuumCommandProperty =
            DependencyProperty.Register(nameof(SetVacuumCommand), typeof(ICommand), typeof(PMC), new PropertyMetadata(null));
    }
}
