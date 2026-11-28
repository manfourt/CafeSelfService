using System.Text.RegularExpressions;
using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Windows.Input;
using System.Windows.Data;
using CafeSelfService.Models;
using CafeSelfService.Util;

namespace CafeSelfService.ViewModels
{
    public class ApplicationViewModel : INotifyPropertyChanged
    {
        private readonly IDishService dishService;
        private Dish selectedDish;
        private string searchText = "";
        private string currentSection = "Меню";
        public ICollectionView MenuItems { get; }
        public RelayCommand NavigateCommand { get; }
        public RelayCommand CloseDishCommand { get; }
        public RelayCommand AddToCartCommand { get; }
        public RelayCommand RemoveFromCartCommand { get; }
        public RelayCommand IncreaseQuantityCommand { get; }
        public RelayCommand DecreaseQuantityCommand { get; }

        public string SearchText
        {
            get
            {
                return searchText;
            }
            set
            {
                searchText = value ?? "";
                OnPropertyChanged();
                MenuItems.Refresh();
                OnPropertyChanged(nameof(EmptyMenuMessage));
                OnPropertyChanged(nameof(HasNoResults));
            }
        }

        public string EmptyMenuMessage
        {
            get
            {
                return string.IsNullOrWhiteSpace(SearchText) ? "Меню пока пусто" : "По вашему запросу ничего не найдено";
            }
        }

        public bool HasNoResults
        {
            get
            {
                return MenuItems.IsEmpty && string.IsNullOrEmpty(StatusMessage);
            }
        }

        public string CurrentSection
        {
            get
            {
                return currentSection;
            }
            private set
            {
                currentSection = value;
                OnPropertyChanged();
            }
        }

        public Order CurrentOrder { get; }
        public bool IsAdministrator { get; }
        public ReadOnlyObservableCollection<OrderItem> CartItems
        {
            get
            {
                return CurrentOrder.Items;
            }
        }

        public bool HasDishDetails
        {
            get
            {
                return SelectedDish != null;
            }
        }

        public bool IsCartEmpty
        {
            get
            {
                return CartItems.Count == 0;
            }
        }

        public int CartCount
        {
            get
            {
                return CurrentOrder.ItemCount;
            }
        }

        public decimal CartTotal
        {
            get
            {
                return CurrentOrder.TotalAmount;
            }
        }

        private void NotifyCartChanged()
        {
            OnPropertyChanged(nameof(IsCartEmpty));
            OnPropertyChanged(nameof(CartCount));
            OnPropertyChanged(nameof(CartTotal));
        }

        private string statusMessage = "";
        public ObservableCollection<Dish> Dishes { get; } = new ObservableCollection<Dish>();
        public Dish SelectedDish
        {
            get
            {
                return selectedDish;
            }
            set
            {
                selectedDish = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasDishDetails));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        public string StatusMessage
        {
            get
            {
                return statusMessage;
            }
            private set
            {
                statusMessage = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoResults));
            }
        }

        public ApplicationViewModel(IDishService dishService, Order currentOrder, bool isAdministrator = false)
        {
            if (dishService == null)
            {
                throw new ArgumentNullException(nameof(dishService));
            }
            this.dishService = dishService;
            if (currentOrder == null)
            {
                throw new ArgumentNullException(nameof(currentOrder));
            }
            CurrentOrder = currentOrder;
            IsAdministrator = isAdministrator;
            CurrentOrder.PropertyChanged += OnOrderChanged;
            MenuItems = CollectionViewSource.GetDefaultView(Dishes);
            MenuItems.Filter = MatchesSearch;

            NavigateCommand = new RelayCommand(Navigate);
            CloseDishCommand = new RelayCommand(parameter => SelectedDish = null);
            AddToCartCommand = new RelayCommand(AddSelectedDish);
            RemoveFromCartCommand = new RelayCommand(RemoveCartItem);
            IncreaseQuantityCommand = new RelayCommand(
                parameter => ChangeQuantity(parameter, 1),
                parameter => CanChangeQuantity(parameter, 1));
            DecreaseQuantityCommand = new RelayCommand(
                parameter => ChangeQuantity(parameter, -1),
                parameter => CanChangeQuantity(parameter, -1));
        }

        private void Navigate(object parameter)
        {
            string section = parameter as string;
            if (section == "Меню" || section == "Корзина" || (section == "Статистика" && IsAdministrator))
            {
                CurrentSection = section;
            }
        }

        private void AddSelectedDish(object parameter)
        {
            Dish dish = SelectedDish;
            if (CurrentOrder.CanAddDish(dish))
            {
                CurrentOrder.AddDish(dish);
                SelectedDish = null;
            }
        }

        private void RemoveCartItem(object parameter)
        {
            OrderItem item = parameter as OrderItem;
            if (item != null)
            {
                CurrentOrder.RemoveItem(item);
            }
        }

        private bool CanChangeQuantity(object parameter, int change)
        {
            OrderItem item = parameter as OrderItem;
            return item != null && CurrentOrder.CanChangeQuantity(item, item.Quantity + change);
        }

        private void ChangeQuantity(object parameter, int change)
        {
            if (CanChangeQuantity(parameter, change))
            {
                OrderItem item = (OrderItem)parameter;
                CurrentOrder.ChangeQuantity(item, item.Quantity + change);
            }
        }

        private void OnOrderChanged(object sender, PropertyChangedEventArgs e)
        {
            NotifyCartChanged();
            CommandManager.InvalidateRequerySuggested();
        }

        public void LoadDishes()
        {
            StatusMessage = "";
            try
            {
                var dishes = dishService.GetDishes();
                SelectedDish = null;
                Dishes.Clear();
                foreach (Dish dish in dishes)
                {
                    Dishes.Add(dish);
                }
                OnPropertyChanged(nameof(HasNoResults));
            }
            catch (Exception)
            {
                StatusMessage = "Не удалось подключиться к базе данных. Попробуйте открыть приложение позже.";
            }
        }

        private bool MatchesSearch(object item)
        {
            Dish dish = (Dish)item;
            string query = SearchText.Trim();
            return Matches(dish.Name, query) || Matches(dish.Category, query) || Matches(dish.Description, query);
        }

        private static bool Matches(string text, string query)
        {
            return (text ?? "").IndexOf(query, StringComparison.CurrentCultureIgnoreCase) >= 0;
        }

        public event PropertyChangedEventHandler PropertyChanged;
        private void OnPropertyChanged([CallerMemberName] string property = "")
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(property));
            }
        }
    }

    public class AccountViewModel
    {
        private readonly DatabaseAccountService accounts;
        public string Error { get; private set; }

        public AccountViewModel(DatabaseAccountService accounts)
        {
            this.accounts = accounts;
        }

        public short? Submit(string login, string name, string password, bool registrationMode)
        {
            Error = "";
            if (!Regex.IsMatch(login, @"\A[A-Za-z0-9_]{3,50}\z") || password.Length == 0)
            {
                Error = "Введите логин (3–50 латинских букв, цифр или подчёркиваний) и пароль.";
                return null;
            }
            if (registrationMode && (!Regex.IsMatch(name, @"\A[А-ЯЁA-Z]") || password.Length < 15))
            {
                Error = "Имя начните с заглавной буквы. Пароль должен содержать не менее 15 символов.";
                return null;
            }

            short? role;
            try
            {
                if (registrationMode)
                {
                    if (!accounts.Register(login, name, password))
                    {
                        Error = "Этот логин уже занят.";
                        return null;
                    }
                    role = 0;
                }
                else
                {
                    role = accounts.SignIn(login, password);
                }
            }
            catch (Exception)
            {
                Error = "Не удалось обратиться к базе данных. Попробуйте позже.";
                return null;
            }

            if (role != 0 && role != 1)
            {
                Error = "Неверный логин или пароль.";
                return null;
            }

            return role;
        }
    }
}
