using System;
using System.Threading.Tasks;

namespace ZeroLevel.Services.Extensions
{
    public static class TaskExtension
    {
        public static T WaitResult<T>(this Task<T> task)
        {
            if (task == null!)
            {
                throw new ArgumentNullException(nameof(task));
            }
            task.Wait();
            if (task.IsFaulted)
            {
                if (task.Exception != null!) throw task.Exception;
            }
            return task.Result;
        }

        public static void WaitWithoutResult(this Task task)
        {
            if (task == null!)
            {
                throw new ArgumentNullException(nameof(task));
            }
            task.Wait();
            if (task.IsFaulted)
            {
                if (task.Exception != null!) throw task.Exception;
            }
        }
    }
}
