using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Serilog;
using System;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace CacheLite
{
    public class Cache
    {
        readonly string _dbName;

        public Cache(string name)
        {
            _dbName = name;
            using (var context = new AppDbContext(_dbName))
            {
                context.Database.EnsureCreated();
                context.CacheItems.RemoveRange(context.CacheItems.Where(x => x.Expiration < DateTime.Now && x.Expiration != null));
                context.SaveChanges();
            }
        }

        /// <summary>
        /// Insert if not exist, update value/expiration if existed
        /// </summary>
        /// <param name="key"></param>
        /// <param name="value"></param>
        /// <param name="timeSpan"></param>
        /// <returns></returns>
        public async Task<bool> UpdateAsync(string key, string value, int? timeSpan)
        {
            bool result = false;
            try
            {
                using (var context = new AppDbContext(_dbName))
                {
                    DateTime? expiration = null;
                    if (timeSpan.HasValue)
                    {
                        expiration = DateTime.Now.Add(TimeSpan.FromMinutes(timeSpan.Value));
                    }

                    CacheItem newItem = null;
                    var existed = await context.CacheItems.FirstOrDefaultAsync(x => x.Key == key);
                    if (existed != null)
                    {
                        existed.Value = value;
                        existed.Expiration = expiration;
                    }
                    else
                    {
                        newItem = new CacheItem
                        {
                            Key = key,
                            Value = value,
                            Expiration = expiration,
                            Type = _dbName
                        };
                        context.CacheItems.Add(newItem);
                    }
                    if (await context.SaveChangesAsync() > 0)
                    {
                        Console.WriteLine($"Added {key} to cache with expiration at {expiration}.");
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public bool GetValue(string key, out string value)
        {
            bool result = false;
            value = string.Empty;
            try
            {
                if (GetAllValuesByKey(key, out string[] values))
                {
                    value = values.FirstOrDefault();
                    result = true;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public bool GetAllValuesByKey(string key, out string[] values)
        {
            bool result = false;
            values = [];
            try
            {
                using (var context = new AppDbContext(_dbName))
                {
                    var cacheItem = context.CacheItems
                        .Where(c => c.Key == key && (c.Expiration > DateTime.Now || c.Expiration == null))
                        .ToList();
                    if (cacheItem.Count > 0)
                    {
                        values = cacheItem.Select(x => x.Value).ToArray();
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public bool GetAllKeys(out string[] key)
        {
            bool result = false;
            key = [];
            try
            {
                using (var context = new AppDbContext(_dbName))
                {
                    var cacheItem = context.CacheItems
                        .Where(c => c.Expiration > DateTime.Now || c.Expiration == null)
                        .ToList();
                    if (cacheItem.Count != 0)
                    {
                        result = true;
                        key = cacheItem.Select(x => x.Key).Distinct().ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public bool GetKey(string value, out string key)
        {
            bool result = false;
            key = string.Empty;
            try
            {
                var allKeys = Array.Empty<string>();
                result = GetKeys(value, out allKeys);
                if (result)
                {
                    key = allKeys.FirstOrDefault();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public bool GetKeys(string value, out string[] key)
        {
            bool result = false;
            key = [];
            try
            {
                using (var context = new AppDbContext(_dbName))
                {
                    var cacheItem = context.CacheItems
                        .Where(c => c.Value == value && (c.Expiration > DateTime.Now || c.Expiration == null))
                        .ToList();
                    if (cacheItem?.Count > 0)
                    {
                        key = cacheItem.Select(x => x.Key).ToArray();
                        result = true;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }

        public async Task<bool> RemoveFromCacheAsync(string key)
        {
            bool result = false;
            try
            {
                using (var context = new AppDbContext(_dbName))
                {
                    var existed = await context.CacheItems.Where(x => x.Key == key).ToListAsync();
                    if (existed.Count > 0)
                    {
                        context.CacheItems.RemoveRange(existed);
                        if (await context.SaveChangesAsync() > 0)
                        {
                            Console.WriteLine("Remove success");
                            result = true;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
            return result;
        }
    }

    class AppDbContext(string type) : DbContext
    {
        readonly string _type = type;

        public DbSet<CacheItem> CacheItems { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder.UseSqlite("Data Source=CacheSqlite.lite");

            #if DEBUG
            optionsBuilder
                          .LogTo(x => Log.Logger.Debug(x),
                                    events:
                                    [
                                        RelationalEventId.CommandExecuted,
                                    ])
                          .EnableDetailedErrors()
                          .EnableSensitiveDataLogging();
            #endif
        }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<CacheItem>().HasQueryFilter(b => b.Type  == _type);

            base.OnModelCreating(modelBuilder);
        }
    }

    public class CacheItem
    {
        [Key]
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Type { get; set; }
        public string Key { get; set; }
        public string Value { get; set; }
        public DateTime? Expiration { get; set; }
    }
}
