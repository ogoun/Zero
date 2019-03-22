using System;

namespace ZeroLevel.DocumentObjectModel
{
    public enum ContentType: Int32
    {
        Raw = 0,
        Text = 1,
        Audio = 2,
        Video = 3,
        Image = 4,
        Docx = 5,
        Xlsx = 6,
        Zip = 7,
        Pdf = 8
    }
}
