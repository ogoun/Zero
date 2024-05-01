using System;
using System.Collections.Generic;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.DocumentObjectModel.Flow;

namespace DOM.Services
{
    /// <summary>
    /// Allows to build the content of document.
    /// </summary>
    public class ContentBuilder
    {
        private readonly Document _parent;
        private readonly FlowContent _content;
        private readonly Stack<IContentElement> _containers;

        public ContentBuilder(Document document)
        {
            if (document == null!)
            {
                throw new ArgumentNullException(nameof(document));
            }
            _parent = document;
            _content = new FlowContent();
            _containers = new Stack<IContentElement>();
        }

        #region Helpers

        #region Map

        private readonly Dictionary<ContentElementType, HashSet<ContentElementType>> _includingMap =
            new Dictionary<ContentElementType, HashSet<ContentElementType>>
            {
                {
                    ContentElementType.Audio, new HashSet<ContentElementType>
                    {
                        ContentElementType.Audioplayer,
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Audioplayer, new HashSet<ContentElementType>
                    {
                        ContentElementType.Paragraph,
                        ContentElementType.Section
                    }
                },
                {
                    ContentElementType.Form, new HashSet<ContentElementType>
                    {
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Gallery, new HashSet<ContentElementType>
                    {
                        ContentElementType.Paragraph,
                        ContentElementType.Section
                    }
                },
                {
                    ContentElementType.Image, new HashSet<ContentElementType>
                    {
                        ContentElementType.Gallery,
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Link, new HashSet<ContentElementType>
                    {
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.List, new HashSet<ContentElementType>
                    {
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Paragraph, new HashSet<ContentElementType>
                    {
                        ContentElementType.Section
                    }
                },
                {
                    ContentElementType.Quote, new HashSet<ContentElementType>
                    {
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Row, new HashSet<ContentElementType>
                    {
                        ContentElementType.Table
                    }
                },
                {
                    ContentElementType.Section, new HashSet<ContentElementType>()
                },
                {
                    ContentElementType.Table, new HashSet<ContentElementType>
                    {
                        ContentElementType.Paragraph,
                        ContentElementType.Section
                    }
                },
                {
                    ContentElementType.Text, new HashSet<ContentElementType>
                    {
                        ContentElementType.Audioplayer,
                        ContentElementType.Videoplayer,
                        ContentElementType.Gallery,
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Column, new HashSet<ContentElementType>
                    {
                        ContentElementType.Table
                    }
                },
                {
                    ContentElementType.Video, new HashSet<ContentElementType>
                    {
                        ContentElementType.Videoplayer,
                        ContentElementType.List,
                        ContentElementType.Paragraph,
                        ContentElementType.Section,
                        ContentElementType.Row
                    }
                },
                {
                    ContentElementType.Videoplayer, new HashSet<ContentElementType>
                    {
                        ContentElementType.Paragraph,
                        ContentElementType.Section
                    }
                }
            };

        #endregion Map

        /// <summary>
        /// Verify that one element can be included as a child of the second element
        /// </summary>
        private bool AllowInclude(ContentElementType elementType, ContentElementType containerType)
        {
            return _includingMap[elementType].Contains(containerType);
        }

        /// <summary>
        /// Writing element to content, dependent on current context.
        /// </summary>
        private void WriteElement(IContentElement element)
        {
            if (_containers.Count == 0) EnterSection();
            var current = _containers.Peek();
            if (false == AllowInclude(element.Type, current.Type))
            {
                RaiseIncorrectContainerType(current.Type, element.Type);
            }
            switch (current.Type)
            {
                case ContentElementType.Section:
                    (current as Section)?.Parts?.Add(element);
                    break;

                case ContentElementType.Paragraph:
                    (current as Paragraph)?.Parts?.Add(element);
                    break;

                case ContentElementType.List:
                    (current as List)?.Items?.Add(element);
                    break;

                case ContentElementType.Row:
                    (current as Row)?.Cells?.Add(element);
                    break;

                case ContentElementType.Audioplayer:
                    if (element.Type == ContentElementType.Text)
                    {
                        (current as Audioplayer)?.SetTitle((element as ZeroLevel.DocumentObjectModel.Flow.Text)!);
                    }
                    else
                    {
                        (current as Audioplayer)?.Tracks?.Add((element as Audio)!);
                    }
                    break;

                case ContentElementType.Videoplayer:
                    if (element.Type == ContentElementType.Text)
                    {
                        (current as Videoplayer)?.SetTitle((element as ZeroLevel.DocumentObjectModel.Flow.Text)!);
                    }
                    else
                    {
                        (current as Videoplayer)?.Playlist?.Add((element as Video)!);
                    }
                    break;

                case ContentElementType.Gallery:
                    if (element.Type == ContentElementType.Text)
                    {
                        (current as Gallery)?.SetTitle((element as ZeroLevel.DocumentObjectModel.Flow.Text)!);
                    }
                    else
                    {
                        (current as Gallery)?.Images?.Add((element as Image)!);
                    }
                    break;

                case ContentElementType.Table:
                    if (element.Type == ContentElementType.Column)
                    {
                        (current as Table)?.Columns?.Add((element as Column)!);
                    }
                    else if (element.Type == ContentElementType.Row)
                    {
                        (current as Table)?.Rows?.Add((element as Row)!);
                    }
                    break;
            }
        }

        private void RaiseIncorrectTypeException(ContentElementType received, ContentElementType expected)
        {
            throw new InvalidCastException($"Type {received} received instead of {expected}");
        }

        private void RaiseIncorrectContainerType(ContentElementType containerType, ContentElementType elementType)
        {
            throw new Exception($"Type {elementType} can not be written to a container of type {containerType}");
        }

        private void ReduceContainers()
        {
            while (_containers.Count > 0)
            {
                var current = _containers.Peek();
                switch (current.Type)
                {
                    case ContentElementType.Section:
                        LeaveSection();
                        break;

                    case ContentElementType.Paragraph:
                        LeaveParagraph();
                        break;

                    case ContentElementType.Audioplayer:
                        LeaveAudioplayer();
                        break;

                    case ContentElementType.Videoplayer:
                        LeaveVideoplayer();
                        break;

                    case ContentElementType.Gallery:
                        LeaveGallery();
                        break;

                    case ContentElementType.List:
                        LeaveList();
                        break;

                    case ContentElementType.Table:
                        LeaveTable();
                        break;

                    case ContentElementType.Row:
                        LeaveRow();
                        break;

                    default:
                        throw new Exception($"Uncknown container type {current.Type}");
                }
            }
        }

        #endregion Helpers

        #region Containers

        public void EnterSection()
        {
            ReduceContainers();
            _containers.Push(new Section());
        }

        public void LeaveSection()
        {
            var section = _containers.Pop();
            if (section.Type != ContentElementType.Section)
            {
                RaiseIncorrectTypeException(section.Type, ContentElementType.Section);
            }
            _content.Sections.Add((section as Section)!);
        }

        public void EnterParagraph()
        {
            if (_containers.Count == 0) EnterSection();
            if (_containers.Peek().Type == ContentElementType.Paragraph) LeaveParagraph();
            if (false == AllowInclude(ContentElementType.Paragraph, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Paragraph);
            }
            _containers.Push(new Paragraph());
        }

        public void LeaveParagraph()
        {
            var paragraph = _containers.Pop();
            if (paragraph.Type != ContentElementType.Paragraph)
            {
                RaiseIncorrectTypeException(paragraph.Type, ContentElementType.Paragraph);
            }
            WriteElement(paragraph);
        }

        public void EnterGallery()
        {
            if (_containers.Count == 0) EnterSection();
            if (false == AllowInclude(ContentElementType.Gallery, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Gallery);
            }
            _containers.Push(new Gallery());
        }

        public void LeaveGallery()
        {
            var gallery = _containers.Pop();
            if (gallery.Type != ContentElementType.Gallery)
            {
                RaiseIncorrectTypeException(gallery.Type, ContentElementType.Gallery);
            }
            WriteElement(gallery);
        }

        public void EnterAudioplayer()
        {
            if (_containers.Count == 0) EnterSection();
            if (false == AllowInclude(ContentElementType.Audioplayer, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Audioplayer);
            }
            _containers.Push(new Audioplayer());
        }

        public void LeaveAudioplayer()
        {
            var audioplayer = _containers.Pop();
            if (audioplayer.Type != ContentElementType.Audioplayer)
            {
                RaiseIncorrectTypeException(audioplayer.Type, ContentElementType.Audioplayer);
            }
            WriteElement(audioplayer);
        }

        public void EnterVideoplayer()
        {
            if (_containers.Count == 0) EnterSection();
            if (false == AllowInclude(ContentElementType.Videoplayer, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Videoplayer);
            }
            _containers.Push(new Videoplayer());
        }

        public void LeaveVideoplayer()
        {
            var videoplayer = _containers.Pop();
            if (videoplayer.Type != ContentElementType.Videoplayer)
            {
                RaiseIncorrectTypeException(videoplayer.Type, ContentElementType.Videoplayer);
            }
            WriteElement(videoplayer);
        }

        #endregion Containers

        #region List

        public void EnterList()
        {
            if (_containers.Count == 0) EnterSection();
            if (_containers.Peek().Type == ContentElementType.Section)
                EnterParagraph();
            if (false == AllowInclude(ContentElementType.List, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.List);
            }
            _containers.Push(new List());
        }

        public void LeaveList()
        {
            var list = _containers.Pop();
            if (list.Type != ContentElementType.List)
            {
                RaiseIncorrectTypeException(list.Type, ContentElementType.List);
            }
            WriteElement(list);
        }

        #endregion List

        #region Table

        public void EnterTable(string name, string summary)
        {
            if (_containers.Count == 0) EnterSection();
            if (false == AllowInclude(ContentElementType.Table, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Table);
            }
            _containers.Push(new Table { Name = name, Abstract = summary });
        }

        public void EnterRow()
        {
            if (_containers.Count == 0)
                throw new Exception("Absent container");
            if (_containers.Peek().Type == ContentElementType.Row) LeaveRow();
            if (false == AllowInclude(ContentElementType.Row, _containers.Peek().Type))
            {
                RaiseIncorrectContainerType(_containers.Peek().Type, ContentElementType.Row);
            }
            _containers.Push(new Row());
        }

        public void LeaveRow()
        {
            var row = _containers.Pop();
            if (row.Type != ContentElementType.Row)
            {
                RaiseIncorrectTypeException(row.Type, ContentElementType.Row);
            }
            WriteElement(row);
        }

        public void LeaveTable()
        {
            if (_containers.Peek().Type == ContentElementType.Row)
                LeaveRow();
            var table = _containers.Pop();
            if (table.Type != ContentElementType.Table)
            {
                RaiseIncorrectTypeException(table.Type, ContentElementType.Table);
            }
            WriteElement(table);
        }

        #endregion Table

        #region Primitives

        public void WriteColumn(Column column)
        {
            if (column == null!)
            {
                throw new ArgumentNullException(nameof(column));
            }
            WriteElement(column);
        }

        public void WriteText(Text text)
        {
            if (text == null!)
            {
                throw new ArgumentNullException(nameof(text));
            }
            WriteElement(text);
        }

        public void WriteText(string text)
        {
            WriteElement(new Text(text));
        }

        public void WriteText(string text, TextStyle style)
        {
            WriteElement(new Text(text) { Style = style });
        }

        public void WriteHeader(string text)
        {
            WriteElement(new Text(text) { Style = new TextStyle { Size = TextSize.MediumHeader, Formatting = TextFormatting.None } });
        }

        public void WriteQuote(Quote quote)
        {
            if (quote == null!)
            {
                throw new ArgumentNullException(nameof(quote));
            }
            WriteElement(quote);
        }

        public void WriteQuote(string quote)
        {
            WriteElement(new Quote(quote));
        }

        public void WriteLink(Link link)
        {
            if (link == null!)
            {
                throw new ArgumentNullException(nameof(link));
            }
            WriteElement(link);
        }

        public void WriteLink(string href, string value)
        {
            WriteElement(new Link(href, value));
        }

        public void WriteForm(FormContent form)
        {
            if (form == null!)
            {
                throw new ArgumentNullException(nameof(form));
            }
            WriteElement(form);
        }

        public void WriteImage(Image image)
        {
            if (image == null!)
            {
                throw new ArgumentNullException(nameof(image));
            }
            WriteElement(image);
        }

        public void WriteAudio(Audio audio)
        {
            if (audio == null!)
            {
                throw new ArgumentNullException(nameof(audio));
            }
            WriteElement(audio);
        }

        public void WriteVideo(Video video)
        {
            if (video == null!)
            {
                throw new ArgumentNullException(nameof(video));
            }
            WriteElement(video);
        }

        #endregion Primitives

        public void Complete()
        {
            ReduceContainers();
            _parent.Content = _content;
        }
    }
}