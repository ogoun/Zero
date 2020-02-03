using System;

namespace ZeroLevel.SqLite
{
    public class UserInfo
    {
        private readonly static UserInfo _anon = new UserInfo
        {
            Created = DateTime.MinValue,
            DisplayName = "Anonimus",
            Role = UserRole.Anonimus,
            UserId = -2,
            UserName = "anonimus"
        };

        public static UserInfo GetAnonimus() => _anon;

        public long UserId { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public DateTime Created { get; set; }
        public UserRole Role { get; set; }
    }
}
