using System;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    /// <summary>
    /// Content Offset in flow
    /// </summary>
    public enum FlowAlign : Int32
    {
        /// <summary>
        /// None
        /// </summary>
        None = 0,

        /// <summary>
        /// Flow: left
        /// </summary>
        Left = 1,

        /// <summary>
        /// Flow: right
        /// </summary>
        Right = 2,

        /// <summary>
        /// Block
        /// </summary>
        Center = 3
    }
}