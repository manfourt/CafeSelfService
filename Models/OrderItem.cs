using System;
using System.ComponentModel;

namespace CafeSelfService.Models
{
    public class OrderItem : INotifyPropertyChanged
    {
        public const int MinimumQuantity = 1;
        public const int MaximumQuantity = 100;
        public Dish Dish { get; }
        public int DishId
        {
            get
            {
                return Dish.Id;
            }
        }

        public string Name
        {
            get
            {
                return Dish.Name;
            }
        }

        public decimal Price { get; }
        public int Quantity { get; private set; }
        public decimal Total
        {
            get
            {
                return Price * Quantity;
            }
        }

        internal OrderItem(Dish dish)
        {
            if (dish == null)
            {
                throw new ArgumentNullException(nameof(dish));
            }
            Dish = dish;
            if (dish.Id <= 0 || dish.Price <= 0)
            {
                throw new ArgumentException("Блюдо должно иметь идентификатор и положительную цену.", nameof(dish));
            }
            // Цена позиции фиксируется отдельно от меняющейся цены меню.
            Price = dish.Price;
            Quantity = 1;
        }

        internal void ChangeQuantity(int quantity)
        {
            if (quantity < MinimumQuantity || quantity > MaximumQuantity)
            {
                throw new ArgumentOutOfRangeException(nameof(quantity), "Допустимо от 1 до 100 порций.");
            }
            Quantity = quantity;
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Quantity)));
            }
            if (PropertyChanged != null)
            {
                PropertyChanged(this, new PropertyChangedEventArgs(nameof(Total)));
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;
    }
}
