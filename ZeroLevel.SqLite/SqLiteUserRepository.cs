using System;
using System.Collections.Generic;
using System.Threading;
using ZeroLevel.Models;

namespace ZeroLevel.SqLite
{
    public class SqLiteUserRepository
        : BaseSqLiteDB<User>
    {
        #region Fields

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        #region Ctor

        public SqLiteUserRepository()
            : base("users.db")
        {
        }

        #endregion Ctor

        public IEnumerable<User> GetAll()
        {
            var list = new List<User>();
            _rwLock.EnterReadLock();
            try
            {
                foreach (var r in SelectAll())
                {
                    list.Add(r);
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLiteUserRepository] Fault get all users");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return list;
        }

        public User Get(long id)
        {
            User user = null;
            _rwLock.EnterReadLock();
            try
            {
                user = Single(r => r.Id == id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault get user by id '{id}'");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return user;
        }

        public User Get(string username, byte[] hash)
        {
            User user = null;
            _rwLock.EnterReadLock();
            try
            {
                user = Single(r => r.UserName == username && r.PasswordHash == hash);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault get user by username '{username}' and pwdhash");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            return user;
        }

        public InvokeResult<long> SaveUser(User user)
        {
            long id = -1;
            _rwLock.EnterWriteLock();
            var creationTime = DateTime.UtcNow.Ticks;
            try
            {
                var count_obj = Count(r => r.UserName == user.UserName);
                if (count_obj > 0)
                {
                    return InvokeResult<long>.Fault<long>("Пользователь с таким именем уже существует");
                }
                id = Append(user).Id;
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault insert user in storage.");
                InvokeResult<long>.Fault(ex.Message);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
            return InvokeResult<long>.Succeeding(id);
        }

        public InvokeResult RemoveUser(string login)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Delete(r => r.UserName == login);
                return InvokeResult.Succeeding();
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLiteUserRepository] Fault remove user '{login}'");
                return InvokeResult.Fault(ex.Message);
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        protected override void DisposeStorageData()
        {
        }
    }
}
