using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq.Expressions;
using ZeroLevel.Services.FileSystem;

namespace ZeroLevel.SqLite
{
    public abstract class BaseSqLiteDB<T>
        : IDisposable
        where T : class, new()
    {
        protected SQLiteConnection _db;
        public BaseSqLiteDB(string name)
        {
            _db = new SQLiteConnection(PrepareDb(name));
        }

        public int Append(T record)
        {
            return _db.Insert(record);
        }

        public CreateTableResult CreateTable()
        {
            return _db.CreateTable<T>();
        }

        public int DropTable()
        {
            return _db.DropTable<T>();
        }

        public IEnumerable<T> SelectAll()
        {
            return _db.Table<T>();
        }

        public IEnumerable<T> SelectBy(Expression<Func<T, bool>> predicate)
        {
            return _db.Table<T>().Where(predicate);
        }

        public T Single(Expression<Func<T, bool>> predicate)
        {
            return _db.Table<T>().FirstOrDefault(predicate);
        }

        public T Single<U>(Expression<Func<T, bool>> predicate, Expression<Func<T, U>> orderBy, bool desc = false)
        {
            if (desc)
            {
                return _db.Table<T>().Where(predicate).OrderByDescending(orderBy).FirstOrDefault();
            }
            return _db.Table<T>().Where(predicate).OrderBy(orderBy).FirstOrDefault();
        }

        public T Single<U>(Expression<Func<T, U>> orderBy, bool desc = false)
        {
            if (desc)
            {
                return _db.Table<T>().OrderByDescending(orderBy).FirstOrDefault();
            }
            return _db.Table<T>().OrderBy(orderBy).FirstOrDefault();
        }

        public IEnumerable<T> SelectBy(int N, Expression<Func<T, bool>> predicate)
        {
            return _db.Table<T>().Where(predicate).Take(N);
        }

        public long Count()
        {
            return _db.Table<T>().Count();
        }

        public long Count(Expression<Func<T, bool>> predicate)
        {
            return _db.Table<T>().Count(predicate);
        }

        public int Delete(Expression<Func<T, bool>> predicate)
        {
            return _db.Table<T>().Delete(predicate);
        }

        public int Update(T record)
        {
            return _db.Update(record);
        }

        protected static string PrepareDb(string path)
        {
            if (Path.IsPathRooted(path) == false)
            {
                path = Path.Combine(FSUtils.GetAppLocalDbDirectory(), path);
            }
            return Path.GetFullPath(path);
        }

        protected abstract void DisposeStorageData();

        public void Dispose()
        {
            DisposeStorageData();
            try
            {
                _db?.Close();
                _db?.Dispose();
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[BaseSqLiteDB] Fault close db connection");
            }            
        }
    }
}
