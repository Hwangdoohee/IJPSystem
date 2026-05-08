using System.Windows;
using IJPSystem.Platform.Domain.Enums;
using IJPSystem.Platform.Domain.Models.Config;
using IJPSystem.Platform.Common.Utilities;
using IJPSystem.Platform.Infrastructure.Config;

namespace IJPSystem.Platform.HMI.Views
{
    public partial class LoginWindow : Window
    {
        public UserRole ResultRole { get; private set; } = UserRole.Engineer;

        private readonly AppSettings _settings;

        public LoginWindow()
        {
            InitializeComponent();
            _settings = new ConfigLoader().LoadAppSettings(PathUtils.GetConfigPath("AppConfig.json"));
            PasswordInput.Focus();
        }

        private void Login_Click(object sender, RoutedEventArgs e)
        {
            string pw = PasswordInput.Password;

            if (pw == _settings.AdminPassword)
            {
                ResultRole = UserRole.Admin;
                DialogResult = true;
            }
            else if (pw == _settings.EngineerPassword)
            {
                ResultRole = UserRole.Engineer;
                DialogResult = true;
            }
            else if (pw == _settings.OperatorPassword)
            {
                ResultRole = UserRole.Operator;
                DialogResult = true;
            }
            else
            {
                MessageBox.Show("비밀번호가 일치하지 않습니다.", "인증 실패", MessageBoxButton.OK, MessageBoxImage.Error);
                PasswordInput.Clear();
            }
        }

        private void Cancel_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
            this.Close();
        }
    }
}