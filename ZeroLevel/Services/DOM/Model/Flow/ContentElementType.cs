using System;

namespace ZeroLevel.DocumentObjectModel.Flow
{
    public enum ContentElementType : Int32
    {
        // Primitives
        Text = 0,
        Quote = 1,
        Link = 2,
        Image = 3,
        Audio = 4,
        Video = 5,
        // Containers        
        Section = 100,
        Paragraph = 101,
        List = 102,
        Table = 103,
        Gallery = 104,
        Audioplayer = 105,
        Videoplayer = 106,
        Row = 107,
        Column = 108,
        // Feature
        Form = 200,
        Content = 500,
        Unknown = 1000
    }
}
