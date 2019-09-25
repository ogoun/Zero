namespace ZeroLevel.Services.AsService
{
    public class Credentials
    {
        public Credentials(string username, string password, ServiceAccount account)
        {
            Username = username;
            Account = account;
            Password = password;
        }

        public string Username { get; }
        public string Password { get; }
        public ServiceAccount Account { get; }
    }
}
