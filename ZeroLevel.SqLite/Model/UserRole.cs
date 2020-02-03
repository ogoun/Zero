using System;

namespace ZeroLevel.SqLite
{
    public enum UserRole
        : Int32
    {
        Anonimus = 0,
        Operator = 1,
        Editor = 512,
        Administrator = 1024,
        SysAdmin = 4096
    }
}
