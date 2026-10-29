using System.Windows;
using System.Windows.Controls;

namespace GoBase.Pages.Login
{
    public partial class LoginView : UserControl
    {
        public LoginView()
        {
            InitializeComponent();
            DataContextChanged += (_, e) =>
            {
                if (e.OldValue is LoginViewModel oldViewModel)
                    oldViewModel.PasswordCleared -= ClearPasswordBox;
                if (e.NewValue is LoginViewModel newViewModel)
                    newViewModel.PasswordCleared += ClearPasswordBox;
            };
        }

        // A PasswordBox cannot be data-bound, so its text is passed to the view model here.
        private void PasswordInput_OnPasswordChanged(object sender, RoutedEventArgs e)
        {
            if (DataContext is LoginViewModel viewModel && sender is PasswordBox passwordBox)
                viewModel.Password = passwordBox.Password;
        }

        private void ClearPasswordBox() => PasswordInput.Clear();
    }
}
