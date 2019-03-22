using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.DocumentObjectModel.Flow;
using ZeroLevel.Services.Serialization;

namespace DOM.Services
{
    /// <summary>
    /// Read/write content elements from/to binary stream
    /// </summary>
    internal static class ContentElementSerializer
    {
        private static IContentElement Read(IBinaryReader reader)
        {
            var type = reader.ReadInt32();
            switch ((ContentElementType)type)
            {
                // Primitives
                case ContentElementType.Text:
                    return new ZeroLevel.DocumentObjectModel.Flow.Text(reader);
                case ContentElementType.Quote:
                    return new Quote(reader);
                case ContentElementType.Link:
                    return new Link(reader);
                case ContentElementType.Image:
                    return new Image(reader);
                case ContentElementType.Audio:
                    return new Audio(reader);
                case ContentElementType.Video:
                    return new Video(reader);
                // Containers
                case ContentElementType.Row:
                    return new Row(reader);
                case ContentElementType.Paragraph:
                    return new Paragraph(reader);
                case ContentElementType.Section:
                    return new Section(reader);
                case ContentElementType.List:
                    return new List(reader);
                case ContentElementType.Table:
                    return new Table(reader);
                case ContentElementType.Gallery:
                    return new Gallery(reader);
                case ContentElementType.Audioplayer:
                    return new Audioplayer(reader);
                case ContentElementType.Videoplayer:
                    return new Videoplayer(reader);
                // Feature
                case ContentElementType.Form:
                    return new FormContent(reader);
            }
            throw new InvalidCastException(string.Format("Uncknown element type: {0}", type));
        }

        public static List<IContentElement> ReadCollection(IBinaryReader reader)
        {
            var count = reader.ReadInt32();
            var collection = new List<IContentElement>(count);
            for (int i = 0; i < count; i++)
            {
                collection.Add(Read(reader));
            }
            return collection;
        }

        public static void WriteCollection(IBinaryWriter writer, IEnumerable<IContentElement> collection)
        {
            writer.WriteInt32(collection.Count());
            foreach (var p in collection)
            {
                writer.WriteInt32((Int32)p.Type);
                p.Serialize(writer);
            }
        }
    }
}
