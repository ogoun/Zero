namespace ZeroLevel.SqLite
{
    public class User
    {
        public long Id { get; set; }
        public string UserName { get; set; }
        public string DisplayName { get; set; }
        public byte[] PasswordHash { get; set; }
        public long Timestamp { get; set; }
        public long Creator { get; set; }
        public UserRole Role { get; set; }
    }
}
