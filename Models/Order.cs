using System;
using System.Linq;
using System.Collections.ObjectModel;
using System.ComponentModel;

namespace CafeSelfService.Models
{
    public class Order : INotifyPropertyChanged
    {
        private readonly ObservableCollection<OrderItem> items = new ObservableCollection<OrderItem>();
        public ReadOnlyObservableCollection<OrderItem> Items { get; }
        public int Id { get; internal set; }
        public string CustomerLogin { get; }
        public DateTime DateTime { get; }
        public OrderStatus Status { get; private set; }
        = OrderStatus.Created;
        public decimal DiscountAmount { get; private set; }
        public decimal TotalAmount
        {
            get
            {
                return items.Sum(item => item.Total) - DiscountAmount;
            }
        }

        public int ItemCount
        {
            get
            {
                return items.Sum(item => item.Quantity);
            }
        }

        public Order(string customerLogin)
        {
            if (string.IsNullOrWhiteSpace(customerLogin))
            {
                throw new ArgumentException("Укажите клиента заказа.", nameof(customerLogin));
            }
            CustomerLogin = customerLogin;
            DateTime = System.DateTime.Now;
            Items = new ReadOnlyObservableCollection<OrderItem>(items);
        }

        public bool CanAddDish(Dish dish)
        {
            if (dish == null || Status != OrderStatus.Created)
            {
                return false;
            }
            var existing = items.FirstOrDefault(item => item.DishId == dish.Id);
            return existing == null || existing.Quantity < OrderItem.MaximumQuantity;
        }

        public void AddDish(Dish dish)
        {
            if (dish == null)
            {
                throw new ArgumentNullException(nameof(dish));
            }
            EnsureEditable();
            var existing = items.FirstOrDefault(item => item.DishId == dish.Id);
            if (existing == null)
            {
                items.Add(new OrderItem(dish));
            }
            else existing.ChangeQuantity(existing.Quantity + 1);
            NotifyChanged();
        }

        public bool CanChangeQuantity(OrderItem item, int quantity)
        {
            return Status == OrderStatus.Created && item != null && items.Contains(item)
            && quantity >= OrderItem.MinimumQuantity && quantity <= OrderItem.MaximumQuantity;
        }

        public void ChangeQuantity(OrderItem item, int quantity)
        {
            EnsureEditable();
            if (!items.Contains(item))
            {
                throw new ArgumentException("Позиция не принадлежит заказу.", nameof(item));
            }
            item.ChangeQuantity(quantity);
            NotifyChanged();
        }

        public void RemoveItem(OrderItem item)
        {
            EnsureEditable();
            if (items.Remove(item))
            {
                NotifyChanged();
            }
        }

        private void EnsureEditable()
        {
            if (Status != OrderStatus.Created)
            {
                throw new InvalidOperationException("Изменять можно только формируемый заказ.");
            }
        }

        private void NotifyChanged()
        {
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(TotalAmount)));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(ItemCount)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
