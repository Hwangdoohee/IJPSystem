using System.Windows;

namespace IJPSystem.Platform.HMI.Views
{
    public partial class RecipeDiffWindow : Window
    {
        public RecipeDiffWindow()
        {
            InitializeComponent();
        }

        private void Close_Click(object sender, RoutedEventArgs e) => Close();
    }
}
