using System;
using System.Collections.Generic;
using System.Linq;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.DocumentObjectModel.Flow;

namespace DOM.DSL.Model
{
    internal class TContentElement
    {
        private readonly Document _document;

        public TContentElement(Document document)
        {
            _document = document;
        }

        private static void TraversElement(IContentElement element, ContentElementType type, Action<IContentElement> handler)
        {
            if (element.Type == type)
            {
                handler(element);
            }
            switch (element.Type)
            {
                // Containers
                case ContentElementType.Section:
                    var section = (element as Section);
                    foreach (var item in section!.Parts)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.Paragraph:
                    var paragraph = (element as Paragraph);
                    foreach (var item in paragraph!.Parts)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.List:
                    var list = (element as List);
                    foreach (var item in list!.Items)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.Gallery:
                    var gallery = (element as Gallery);
                    foreach (var item in gallery!.Images)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.Audioplayer:
                    var audioplayer = (element as Audioplayer);
                    foreach (var item in audioplayer!.Tracks)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.Videoplayer:
                    var videoplayer = (element as Videoplayer);
                    foreach (var item in videoplayer!.Playlist)
                    {
                        TraversElement(item, type, handler);
                    }
                    break;

                case ContentElementType.Table:
                    var table = (element as Table);
                    foreach (var column in table!.Columns)
                    {
                        TraversElement(column, type, handler);
                    }
                    foreach (var row in table.Rows)
                    {
                        TraversElement(row, type, handler);
                        foreach (var cell in row.Cells)
                        {
                            TraversElement(cell, type, handler);
                        }
                    }
                    break;
            }
        }

        private ContentElementType ParseContentElementType(string element_name)
        {
            switch (element_name)
            {
                case "section":
                    return ContentElementType.Section;

                case "paragraph":
                    return ContentElementType.Paragraph;

                case "link":
                    return ContentElementType.Link;

                case "list":
                    return ContentElementType.List;

                case "table":
                    return ContentElementType.Table;

                case "audio":
                    return ContentElementType.Audio;

                case "audioplayer":
                    return ContentElementType.Audioplayer;

                case "form":
                    return ContentElementType.Form;

                case "gallery":
                    return ContentElementType.Gallery;

                case "image":
                    return ContentElementType.Image;

                case "video":
                    return ContentElementType.Video;

                case "videoplayer":
                    return ContentElementType.Videoplayer;

                case "quote":
                    return ContentElementType.Quote;

                case "text":
                    return ContentElementType.Text;

                case "column":
                    return ContentElementType.Column;

                case "row":
                    return ContentElementType.Row;
            }
            return ContentElementType.Unknown;
        }

        public IEnumerable<IContentElement> Find(string elementName, string index)
        {
            var type = ParseContentElementType(elementName);
            if (type == ContentElementType.Unknown) return Enumerable.Empty<IContentElement>();
            var list = new List<IContentElement>();
            foreach (var section in _document.Content.Sections)
            {
                TraversElement(section, type, e => list.Add(e));
            }
            return list;
        }

        public override string ToString()
        {
            return "Content";
        }
    }
}