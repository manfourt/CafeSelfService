using System.Windows;
using CafeSelfService.Models;
using CafeSelfService.Util;
using CafeSelfService.ViewModels;

namespace CafeSelfService
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            var viewModel = new ApplicationViewModel(new DatabaseDishService(), new Order("test_client"));
            DataContext = viewModel;
            Loaded += OnLoaded;
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            Loaded -= OnLoaded;
            var viewModel = (ApplicationViewModel)DataContext;
            if (viewModel.LoadCommand.CanExecute(null))
            {
                viewModel.LoadCommand.Execute(null);
            }
        }
    }
}
