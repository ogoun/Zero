namespace ZeroLevel.Services.Collections
{
    public interface ITransactable
    {
        bool StartTransction();
        bool Commit();
        bool Rollback();
    }
}
