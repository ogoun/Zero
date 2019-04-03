using System;
using System.ComponentModel;

namespace ZeroLevel.DocumentObjectModel
{
    public enum Priority
        : Int32
    {
        [Description("Normal")]
        Normal = 0,

        [Description("Express")]
        Express = 1,

        [Description("Flash")]
        Flash = 2
    }
}