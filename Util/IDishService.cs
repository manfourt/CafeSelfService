using System.Collections.Generic;
using CafeSelfService.Models;

namespace CafeSelfService.Util
{
    public interface IDishService
    {
        List<Dish> GetDishes();
    }
}
