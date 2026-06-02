using System.Windows;

namespace IJPSystem.Platform.HMI.Common
{
    // Maximized 메인 창 뒤로 다이얼로그가 가려지는 문제를 막기 위해 owner 를 자동 지정한다.
    // App 시작/예외 처리 시점처럼 MainWindow 가 아직 없을 때는 owner 없이 표시한다.
    public static class Dialogs
    {
        private static Window? Owner => System.Windows.Application.Current?.MainWindow;

        public static MessageBoxResult Show(string text)
            => Owner is { } o ? MessageBox.Show(o, text) : MessageBox.Show(text);

        public static MessageBoxResult Show(string text, string caption)
            => Owner is { } o ? MessageBox.Show(o, text, caption) : MessageBox.Show(text, caption);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton button)
            => Owner is { } o ? MessageBox.Show(o, text, caption, button) : MessageBox.Show(text, caption, button);

        public static MessageBoxResult Show(string text, string caption, MessageBoxButton button, MessageBoxImage icon)
            => Owner is { } o ? MessageBox.Show(o, text, caption, button, icon) : MessageBox.Show(text, caption, button, icon);
    }
}
