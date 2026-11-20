using System.Collections.Generic;
using System.Threading.Tasks;
using CafeSelfService.Models;

namespace CafeSelfService.Util
{
    public interface IDishService
    {
        Task<List<Dish>> GetDishesAsync();
    }
}
