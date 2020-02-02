using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using ZeroLevel.Models;

namespace ZeroLevel.SqLite
{
    public class AuthRepository
    {
        private static byte[] DEFAULT_ADMIN_PWD_HASH = null;

        private readonly SqLiteUserRepository _userRepository = new SqLiteUserRepository();

        public UserInfo GetUserInfo(string username, string password)
        {
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            {
                return UserInfo.GetAnonimus();
            }
            // Check built-in admin
            if (DEFAULT_ADMIN_PWD_HASH != null && DEFAULT_ADMIN_PWD_HASH.Length > 0 && (username.Equals("root", System.StringComparison.Ordinal) || username.Equals("admin", System.StringComparison.Ordinal))
                && DEFAULT_ADMIN_PWD_HASH.SequenceEqual(ComputeHash(password)))
            {
                return new UserInfo
                {
                    Role = UserRole.SysAdmin,
                    UserId = -1,
                    UserName = "sysadmin",
                    DisplayName = "System Administrator",
                    Created = DateTime.Now
                };
            }
            else
            {
                var user = _userRepository.Get(username, ComputeHash(password));
                if (user != null)
                {
                    return new UserInfo
                    {
                        Created = new DateTime(user.Timestamp, DateTimeKind.Utc),
                        DisplayName = user.DisplayName,
                        Role = user.Role,
                        UserId = user.Id,
                        UserName = user.UserName
                    };
                }
            }
            return null;
        }

        public InvokeResult<long> CreateUser(string username, string pwd, string displayName, UserRole role, long currentUserId)
        {
            return _userRepository.SaveUser(new User
            {
                Creator = currentUserId,
                DisplayName = displayName,
                PasswordHash = ComputeHash(pwd),
                Role = role,
                Timestamp = DateTime.UtcNow.Ticks,
                UserName = username
            });
        }

        public InvokeResult<IEnumerable<User>> GetUsers()
        {
            try
            {
                return InvokeResult<IEnumerable<User>>.Succeeding(_userRepository.GetAll());
            }
            catch (Exception ex)
            {
                return InvokeResult<IEnumerable<User>>.Fault<IEnumerable<User>>(ex.Message);
            }
        }

        public InvokeResult RemoveUser(string login)
        {
            return _userRepository.RemoveUser(login);
        }

        public void SetAdminPassword(string rootPwd) => DEFAULT_ADMIN_PWD_HASH = ComputeHash(rootPwd);

        private byte[] ComputeHash(string pwd)
        {
            using (SHA256 shaM = new SHA256Managed())
            {
                return shaM.ComputeHash(Encoding.UTF8.GetBytes(pwd));
            }
        }
    }
}
