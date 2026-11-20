using System;
using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
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
                OnPropertyChanged(nameof(HasNoResults));
            }
        }

        public bool HasNoResults
        {
            get
            {
                return MenuItems.IsEmpty && !IsBusy && string.IsNullOrEmpty(StatusMessage);
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

        private RelayCommand navigateCommand;
        public RelayCommand NavigateCommand
        {
            get
            {
                if (navigateCommand == null)
                {
                    navigateCommand = new RelayCommand(section =>
                    {
                        string name = section as string;
                        if (name == "Меню" || name == "Корзина" || name == "Статистика")
                        {
                            CurrentSection = name;
                        }
                    });
                }
                return navigateCommand;
            }
        }

        private RelayCommand closeDishCommand;
        public RelayCommand CloseDishCommand
        {
            get
            {
                if (closeDishCommand == null)
                {
                    closeDishCommand = new RelayCommand(obj => SelectedDish = null);
                }
                return closeDishCommand;
            }
        }

        private RelayCommand addToCartCommand;
        public RelayCommand AddToCartCommand
        {
            get
            {
                if (addToCartCommand == null)
                {
                    addToCartCommand = new RelayCommand(obj =>
                    {
                        var dish = SelectedDish;
                        if (dish == null)
                        {
                            return;
                        }
                        if (!CurrentOrder.CanAddDish(dish))
                        {
                            return;
                        }
                        CurrentOrder.AddDish(dish);
                        SelectedDish = null;
                    });
                }
                return addToCartCommand;
            }
        }

        private RelayCommand removeFromCartCommand;
        public RelayCommand RemoveFromCartCommand
        {
            get
            {
                if (removeFromCartCommand == null)
                {
                    removeFromCartCommand = new RelayCommand(obj =>
                    {
                        var item = obj as OrderItem;
                        if (item != null)
                        {
                            CurrentOrder.RemoveItem(item);
                        }
                    });
                }
                return removeFromCartCommand;
            }
        }

        public Order CurrentOrder { get; }
        public ReadOnlyObservableCollection<OrderItem> CartItems
        {
            get
            {
                return CurrentOrder.Items;
            }
        }

        private RelayCommand increaseQuantityCommand;
        public RelayCommand IncreaseQuantityCommand
        {
            get
            {
                if (increaseQuantityCommand == null)
                {
                    increaseQuantityCommand = new RelayCommand(obj =>
                    {
                        var item = obj as OrderItem;
                        if (item != null && CurrentOrder.CanChangeQuantity(item, item.Quantity + 1))
                        {
                            CurrentOrder.ChangeQuantity(item, item.Quantity + 1);
                        }
                    }, obj => obj is OrderItem && CurrentOrder.CanChangeQuantity((OrderItem)obj, ((OrderItem)obj).Quantity + 1));
                }
                return increaseQuantityCommand;
            }
        }

        private RelayCommand decreaseQuantityCommand;
        public RelayCommand DecreaseQuantityCommand
        {
            get
            {
                if (decreaseQuantityCommand == null)
                {
                    decreaseQuantityCommand = new RelayCommand(obj =>
                    {
                        var item = obj as OrderItem;
                        if (item != null && CurrentOrder.CanChangeQuantity(item, item.Quantity - 1))
                        {
                            CurrentOrder.ChangeQuantity(item, item.Quantity - 1);
                        }
                    }, obj => obj is OrderItem && CurrentOrder.CanChangeQuantity((OrderItem)obj, ((OrderItem)obj).Quantity - 1));
                }
                return decreaseQuantityCommand;
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

        private bool isBusy;
        private string statusMessage = "";
        public ObservableCollection<Dish> Dishes { get; }
        = new ObservableCollection<Dish>();
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

        public bool IsBusy
        {
            get
            {
                return isBusy;
            }
            private set
            {
                isBusy = value;
                OnPropertyChanged();
                OnPropertyChanged(nameof(HasNoResults));
                CommandManager.InvalidateRequerySuggested();
            }
        }

        private RelayCommand loadCommand;
        public RelayCommand LoadCommand
        {
            get
            {
                if (loadCommand == null)
                {
                    loadCommand = new RelayCommand(async obj => await LoadAsync(), obj => !IsBusy);
                }
                return loadCommand;
            }
        }

        public ApplicationViewModel(IDishService dishService, Order currentOrder)
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
            CurrentOrder.PropertyChanged += (sender, args) =>
            {
                NotifyCartChanged();
                CommandManager.InvalidateRequerySuggested();
            }
            ;
            MenuItems = CollectionViewSource.GetDefaultView(Dishes);
            MenuItems.Filter = item =>
            {
                var dish = (Dish)item;
                string query = SearchText.Trim();
                return Matches(dish.Name, query) || Matches(dish.Category, query) || Matches(dish.Description, query);
            }
            ;
        }

        public async Task LoadAsync()
        {
            if (IsBusy)
            {
                return;
            }
            IsBusy = true;
            StatusMessage = "";
            try
            {
                var dishes = await dishService.GetDishesAsync();
                SelectedDish = null;
                Dishes.Clear();
                foreach (var dish in dishes) Dishes.Add(dish);
            }
            catch (Exception)
            {
                StatusMessage = "Не удалось подключиться к базе данных. Попробуйте открыть приложение позже.";
            }
            finally
            {
                IsBusy = false;
            }
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
}
