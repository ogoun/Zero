﻿using System.Collections.Generic;
using System.Threading.Tasks;

namespace ZeroLevel.Services
{
    public static class AsyncExtensions
    {
        /*public static async IAsyncEnumerable<T> ParallelEnumerateAsync<T>(this IEnumerable<Task<T>> tasks)
        {
            var remaining = new List<Task<T>>(tasks);

            while (remaining.Count != 0)
            {
                var task = await Task.WhenAny(remaining);
                remaining.Remove(task);
                yield return (await task);
            }
        }*/
    }
}
