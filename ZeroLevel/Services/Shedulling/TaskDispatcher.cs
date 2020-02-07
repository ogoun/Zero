using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Shedulling
{
    public interface ITaskDb
    {
        long GetLastCounter();
        void Store(TaskRecord task);
    }

    public class TaskRecord
    {
        public long Identity;
        public string TypeName;
        public string MethodName;
        public IBinarySerializable Parameter;

        public TaskGroup Parent;
    }

    public class TaskGroup
    {
        public long Identity;
        public IEnumerable<TaskRecord> Tasks;

        public TaskPipeline Parent;
    }

    public class TaskPipeline
    {
        public long Identity;
        IEnumerable<TaskGroup> Pipeline;

        public TaskQueue Parent;
    }

    public class TaskQueue
    {
        public long Identity;
    }

    public class TaskDispatcher
    {
        ISheduller sheduller;

        public TaskDispatcher(ITaskDb db)
        {
            sheduller.SetInitialIndex(db.GetLastCounter());
        }

        private MethodInfo FindMethod(TaskRecord task)
        {
            var type = Type.GetType(task.TypeName);
            var method = type.GetMethods().First(m => m.Name.Equals(task.MethodName, StringComparison.Ordinal));


            return method;
        }

        public static void Add<T>(Expression<Action<T>> methodCall)
        {
        }            
    }
}
