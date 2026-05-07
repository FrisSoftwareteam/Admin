using FirstReg.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;

namespace FirstReg.Services
{
    public class DataService
    {
        private readonly AppDB _db;

        public DataService(AppDB db) => _db = db;

        #region generic methods

        public T Get<T>(int id) where T : class
        {
            var entity = _db.Set<T>();
            return entity.Find(id);
        }

        public async Task<List<T>> Get<T>() where T : class
        {
            var entity = _db.Set<T>();
            return await entity.ToListAsync();
        }

        public IQueryable<T> GetAsQueryable<T>() where T : class
        {
            var entity = _db.Set<T>();
            return entity.AsQueryable();
        }

        public async Task<List<T>> Find<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entity = _db.Set<T>();
            return await entity.Where(predicate).ToListAsync();
        }

        public async Task<T> Get<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entity = _db.Set<T>();
            return await entity.FirstOrDefaultAsync(predicate);
        }

        public int Count<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entity = _db.Set<T>();
            return entity.Count(predicate);
        }

        public async Task<bool> ExistsAsync<T>(Expression<Func<T, bool>> predicate) where T : class
        {
            var entity = _db.Set<T>();
            return await entity.AnyAsync(predicate);
        }

        public async Task SaveAsync<T>(T entity) where T : class
        {
            _db.Add(entity);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync<T>(T entity) where T : class
        {
            _db.Update(entity);
            await _db.SaveChangesAsync();
        }

        public async Task DeleteAsync<T>(T entity) where T : class
        {
            _db.Remove(entity);
            await _db.SaveChangesAsync();
        }

        public async Task<List<T>> FromSql<T>(string sql) where T : class =>
            await _db.Set<T>().FromSqlRaw(sql).ToListAsync();

        public async Task ExecuteSql(string sql) =>
            await _db.Database.ExecuteSqlRawAsync(sql);

        #endregion
    }
}