namespace ZeroLevel.Services.Config
{
    public interface IConfigRecordParser
    {
        object Parse(string line);
    }
}
