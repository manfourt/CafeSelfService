using System;
using System.Windows;
using CafeSelfService.Models;
using CafeSelfService.Util;
using CafeSelfService.ViewModels;

namespace CafeSelfService
{
    public partial class MainWindow : Window
    {
        private readonly DatabaseAccountService accounts = new DatabaseAccountService();
        private readonly AccountViewModel auth;
        private bool registrationMode;

        public MainWindow()
        {
            auth = new AccountViewModel(accounts);
            InitializeComponent();
#if DEBUG
            TestSignInButton.Visibility = Visibility.Visible;
#endif
            LoginInput.Focus();
        }

        private void SwitchAccountMode(object sender, RoutedEventArgs e)
        {
            registrationMode = !registrationMode;
            AuthTitle.Text = registrationMode ? "Регистрация" : "Вход";
            SubmitButton.Content = registrationMode ? "Зарегистрироваться" : "Войти";
            SwitchButton.Content = registrationMode ? "Уже есть аккаунт" : "Создать аккаунт";
            RegistrationFields.Visibility = registrationMode ? Visibility.Visible : Visibility.Collapsed;
            AuthError.Text = "";
            PasswordInput.Clear();
            LoginInput.Focus();
        }

        private void SubmitAccount(object sender, RoutedEventArgs e)
        {
            string login = LoginInput.Text.Trim();
            string password = PasswordInput.Password;
            string name = NameInput.Text.Trim();
            AuthError.Text = "";

            short? role = auth.Submit(login, name, password, registrationMode);
            PasswordInput.Clear();
            AuthError.Text = auth.Error;
            if (!role.HasValue)
            {
                return;
            }

            OpenMenu(login, role == 1);
        }

        private void SignInForTesting(object sender, RoutedEventArgs e)
        {
#if DEBUG
            PasswordInput.Clear();
            short? role;
            try
            {
                role = accounts.GetTestAccountRole();
            }
            catch (Exception)
            {
                AuthError.Text = "Не удалось обратиться к базе данных.";
                return;
            }

            if (!role.HasValue)
            {
                AuthError.Text = "Тестовая учётная запись не найдена.";
                return;
            }

            OpenMenu("test_client", role == 1);
#endif
        }

        private void OpenMenu(string login, bool isAdministrator)
        {
            var viewModel = new ApplicationViewModel(new DatabaseDishService(), new Order(login), isAdministrator);
            DataContext = viewModel;
            SubmitButton.IsDefault = false;
            SignInScreen.Visibility = Visibility.Collapsed;
            TestSignInButton.Visibility = Visibility.Collapsed;
            MenuScreen.Visibility = Visibility.Visible;
            viewModel.LoadDishes();
        }
    }
}
