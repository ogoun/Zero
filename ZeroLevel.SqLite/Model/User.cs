using SQLite;

namespace ZeroLevel.SqLite
{
    public class User
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [Indexed]
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        [Indexed]
        public byte[] PasswordHash { get; set; }
        public long Timestamp { get; set; }
        public long Creator { get; set; }
        public UserRole Role { get; set; }
    }
}
