using System;
using System.Security.Cryptography;
using System.Text;
using Npgsql;
using System.Collections.Generic;
using System.Linq;
using CafeSelfService.Data;
using CafeSelfService.Models;

namespace CafeSelfService.Util
{
    public class DatabaseDishService : IDishService
    {
        public List<Dish> GetDishes()
        {
            using (var db = new CafeDbContext())
            {
                return db.Dishes.AsNoTracking().OrderBy(d => d.Name).ToList();
            }
        }
    }

    public class DatabaseAccountService
    {
        public bool Register(string login, string name, string password)
        {
            byte[] hash = HashPassword(login, password);
            using (var db = new CafeDbContext())
            {
                return db.Database.ExecuteSqlCommand(
                    @"INSERT INTO public.tableuser
                      (login, name, passwordhash1, passwordhash2, type, bonusbalance)
                      VALUES (@login, @name, @first, @second, 0, 0)
                      ON CONFLICT (login) DO NOTHING",
                    new NpgsqlParameter("login", login),
                    new NpgsqlParameter("name", name),
                    new NpgsqlParameter("first", BitConverter.ToInt64(hash, 0)),
                    new NpgsqlParameter("second", BitConverter.ToInt64(hash, 8))) == 1;
            }
        }

#if DEBUG
        public short? GetTestAccountRole()
        {
            using (var db = new CafeDbContext())
            {
                return db.Database.SqlQuery<short?>(
                    "SELECT type FROM public.tableuser WHERE login = @login",
                    new NpgsqlParameter("login", "test_client")).SingleOrDefault();
            }
        }
#endif

        public short? SignIn(string login, string password)
        {
            StoredAccount account;
            using (var db = new CafeDbContext())
            {
                account = db.Database.SqlQuery<StoredAccount>(
                    @"SELECT passwordhash1 AS ""Hash1"", passwordhash2 AS ""Hash2"", type AS ""Role""
                      FROM public.tableuser WHERE login = @login",
                    new NpgsqlParameter("login", login)).SingleOrDefault();
            }

            byte[] hash = HashPassword(login, password);
            if (account == null || (account.Hash1 == 0 && account.Hash2 == 0))
            {
                return null;
            }
            long difference = (account.Hash1 ^ BitConverter.ToInt64(hash, 0))
                            | (account.Hash2 ^ BitConverter.ToInt64(hash, 8));
            return difference == 0 ? (short?)account.Role : null;
        }

        private static byte[] HashPassword(string login, string password)
        {
            // В учебной схеме соль привязана к логину, а хеш хранится в двух BIGINT.
            byte[] salt = Encoding.UTF8.GetBytes("MaKey:password:v1:" + login);
            using (var hash = new Rfc2898DeriveBytes(password, salt, 600000, HashAlgorithmName.SHA256))
            {
                return hash.GetBytes(16);
            }
        }

        private class StoredAccount
        {
            public long Hash1 { get; set; }
            public long Hash2 { get; set; }
            public short Role { get; set; }
        }
    }
}
