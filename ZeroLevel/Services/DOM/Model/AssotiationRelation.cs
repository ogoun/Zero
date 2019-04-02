using System;

namespace ZeroLevel.DocumentObjectModel
{
    public enum AssotiationRelation : Int32
    {
        /// <summary>
        /// Relation type not defined
        /// </summary>
        Uncknown = 0,
        /// <summary>
        /// Mentioned
        /// </summary>
        Mentions = 1,
        /// <summary>
        /// It tells about
        /// </summary>
        About = 2,
        /// <summary>
        /// Previous version update
        /// </summary>
        UpdateOf = 3,
        /// <summary>
        /// Based on
        /// </summary>
        BasedOn = 4
    }
}
