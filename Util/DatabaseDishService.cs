using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using CafeSelfService.Data;
using CafeSelfService.Models;

namespace CafeSelfService.Util
{
    public class DatabaseDishService : IDishService
    {
        public async Task<List<Dish>> GetDishesAsync()
        {
            using (var db = new CafeDbContext())
            return await db.Dishes.AsNoTracking().OrderBy(d => d.Name).ToListAsync();
        }
    }
}
