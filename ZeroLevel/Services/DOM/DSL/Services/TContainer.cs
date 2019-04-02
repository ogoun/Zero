using DOM.DSL.Model;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Security;
using System.Text;
using System.Threading;
using ZeroLevel;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.DocumentObjectModel.Flow;
using ZeroLevel.Services.FileSystem;
using ZeroLevel.Services.Reflection;
using ZeroLevel.Services.Serialization;
using ZeroLevel.Services.Web;

namespace DOM.DSL.Services
{
    internal class TContainer
    {
        #region Private classes
        private class TDList
        {
            private IList _list;
            private Type _elementType;

            public int Count => _list.Count;

            public void Append(object _item)
            {
                if (_item == null) return;
                object item;
                if (_item is TContainer)
                {
                    item = ((TContainer)_item).Current;
                }
                else
                {
                    item = _item;
                }
                if (_list == null)
                {
                    _elementType = item.GetType();
                    _list = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(_elementType));
                    _list.Add(item);
                }
                else if (_elementType == typeof(string))
                {
                    _list.Add(item.ToString());
                }
                else
                {
                    if (item.GetType() == _elementType)
                    {
                        _list.Add(item);
                    }
                    else
                    {
                        // Added elements of different types. All elements casts to string
                        var list = new List<string>();
                        foreach (var i in _list) list.Add(i.ToString());
                        _elementType = typeof(string);
                        _list.Clear();
                        _list = null;
                        _list = list;
                        list.Add(item.ToString());
                    }
                }
            }

            public object First()
            {
                return (_list.Count > 0) ? _list[0] : null;
            }

            public IList Complete()
            {
                return _list;
            }
        }
        #endregion

        private readonly TContainerFactory _factory;
        private readonly TRender _render;

        private object _current;
        public int Index { get; set; }

        public object Current
        {
            get { return _current; }
        }

        public bool IsNumeric
        {
            get
            {
                return _current != null &&
                    (_current is byte ||
                    _current is int ||
                    _current is uint ||
                    _current is long ||
                    _current is ulong ||
                    _current is short ||
                    _current is ushort ||
                    _current is float ||
                    _current is double ||
                    _current is decimal);
            }
        }

        public bool IsString
        {
            get
            {
                return _current != null && _current is string;
            }
        }

        public bool IsBoolean
        {
            get
            {
                return _current != null && _current is Boolean;
            }
        }

        public bool IsEnumerable
        {
            get
            {
                return _current != null && false == (_current is string) && _current is IEnumerable;
            }
        }

        public TContainer(TContainerFactory factory, TRender render)
        {
            this._factory = factory;
            this._render = render;
        }

        public void Reset(object value)
        {
            _current = value;
            Index = 0;
        }

        public void Copy(TContainer container)
        {
            _current = container.Current;
            Index = container.Index;
        }

        public void MoveToProperty(string propertyName, string propertyIndex)
        {
            if (propertyName.Equals("order", StringComparison.OrdinalIgnoreCase)) { Reset(Index); return; }
            if (_current == null) return;

            var buff_val = _current;
            var buff_index = Index;
            try
            {
                if (_current is string) SelectProperty((string)_current, propertyName, propertyIndex);
                else if (_current is DateTime) SelectProperty((DateTime)_current, propertyName, propertyIndex);
                else if (_current is TimeSpan) SelectProperty((TimeSpan)_current, propertyName, propertyIndex);
                else if (_current is Text) SelectProperty((Text)_current, propertyName, propertyIndex);
                else if (_current is Image) SelectProperty((Image)_current, propertyName, propertyIndex);
                else if (_current is Link) SelectProperty((Link)_current, propertyName, propertyIndex);
                else if (_current is Agency) SelectProperty((Agency)_current, propertyName, propertyIndex);
                else if (_current is Category) SelectProperty((Category)_current, propertyName, propertyIndex);
                else if (_current is Header) SelectProperty((Header)_current, propertyName, propertyIndex);
                else if (_current is Tag) SelectProperty((Tag)_current, propertyName, propertyIndex);
                else if (_current is AttachContent) SelectProperty((AttachContent)_current, propertyName, propertyIndex);
                else if (_current is Assotiation) SelectProperty((Assotiation)_current, propertyName, propertyIndex);
                else if (_current is List<Header>) SelectProperty((List<Header>)_current, propertyName, propertyIndex);
                else if (_current is Identifier) SelectProperty((Identifier)_current, propertyName, propertyIndex);
                else if (_current is TagMetadata) SelectProperty((TagMetadata)_current, propertyName, propertyIndex);
                else if (_current is DescriptiveMetadata) SelectProperty((DescriptiveMetadata)_current, propertyName, propertyIndex);
                else if (_current is TEnvironment) SelectProperty((TEnvironment)_current, propertyName, propertyIndex);
                else if (_current is DOMRenderElementCounter) SelectProperty((DOMRenderElementCounter)_current, propertyName, propertyIndex);
                else if (_current is IDictionary) SelectProperty((IDictionary)_current, propertyName, propertyIndex);
                else if (_current is Audio) SelectProperty((Audio)_current, propertyName, propertyIndex);
                else if (_current is Table) SelectProperty((Table)_current, propertyName, propertyIndex);
                else if (_current is FormContent) SelectProperty((FormContent)_current, propertyName, propertyIndex);
                else if (_current is Quote) SelectProperty((Quote)_current, propertyName, propertyIndex);
                else if (_current is Video) SelectProperty((Video)_current, propertyName, propertyIndex);
                else if (_current is Paragraph) SelectProperty((Paragraph)_current, propertyName, propertyIndex);
                else if (_current is Section) SelectProperty((Section)_current, propertyName, propertyIndex);
                else if (_current is Row) SelectProperty((Row)_current, propertyName, propertyIndex);
                else if (_current is Column) SelectProperty((Column)_current, propertyName, propertyIndex);
                else if (_current is List) SelectProperty((List)_current, propertyName, propertyIndex);
                else if (_current is Audioplayer) SelectProperty((Audioplayer)_current, propertyName, propertyIndex);
                else if (_current is Gallery) SelectProperty((Gallery)_current, propertyName, propertyIndex);
                else if (_current is Videoplayer) SelectProperty((Videoplayer)_current, propertyName, propertyIndex);
                else if (_current is TContentElement) SelectProperty((TContentElement)_current, propertyName, propertyIndex);
                else if (_current is TextStyle) SelectProperty((TextStyle)_current, propertyName, propertyIndex);
            }
            catch
            {
                _current = buff_val;
                Index = buff_index;
            }
        }

        public void ApplyFunction(string functionName, Func<TContainer, TContainer[]> args_getter)
        {
            var buff_val = _current;
            var buff_index = Index;
            TContainer[] args = null;
            try
            {
                switch (GetFunctionType(functionName))
                {
                    case FunctionType.String:
                        args = args_getter(this);
                        ApplyStringFunction(functionName, args);
                        break;
                    case FunctionType.Condition:
                        args = args_getter(this);
                        ApplyConditionFunction(functionName, args);
                        break;
                    case FunctionType.Extract:
                        unchecked
                        {
                            ApplyExtractionFunction(functionName, args_getter, out args);
                        }
                        break;
                    case FunctionType.Unknown:
                        break;
                }
            }
            catch
            {
                _current = buff_val;
                Index = buff_index;
            }
            if (args != null)
            {
                foreach (var a in args)
                    _factory.Release(a);
            }
        }

        #region As
        public T As<T>()
        {
            if (_current == null) return default(T);
            if (_current is T) return (T)_current;
            var type = typeof(T);
            if (_current is string)
            {
                return ConvertTo<T>((string)_current);
            }
            try
            {
                return (T)Convert.ChangeType(_current, type);
            }
            catch (Exception ex)
            {
                Log.SystemWarning($"[DOM.TContainer] Fault cast current value from type '{_current?.GetType()?.FullName ?? string.Empty}' to type '{type.FullName}'. {ex.ToString()}");
                return default(T);
            }
        }

        public object As(Type type)
        {
            if (_current == null) return TypeHelpers.CreateDefaultState(type);
            if (_current.GetType().IsAssignableFrom(type)) return _current;
            if (_current is string)
            {
                return ConvertTo((string)_current, type);
            }
            try
            {
                return Convert.ChangeType(_current, type);
            }
            catch(Exception ex)
            {
                Log.SystemWarning($"[DOM.TContainer] Fault cast current value from type '{_current?.GetType()?.FullName ?? string.Empty}' to type '{type.FullName}'. {ex.ToString()}");
                return TypeHelpers.CreateDefaultState(type);
            }
        }
        #endregion

        #region Detect function type
        private enum FunctionType
        {
            Extract,
            String,
            Condition,
            Unknown
        }
        private static FunctionType GetFunctionType(string function)
        {
            switch (function)
            {
                case "tolower":
                case "toupper":
                case "totitle":
                case "trim":
                case "insert":
                case "replace":
                case "remove":
                case "padleft":
                case "padright":
                case "substr":
                case "index":
                case "lastindex":
                case "joinright":
                case "joinleft":
                case "map_mc":
                case "map":
                case "xmlescape":
                case "htmlescape":
                case "jsonescape":
                case "join":
                case "format":
                case "tofilename":
                case "topath":
                    return FunctionType.String;
                case "contains":
                case "nocontains":
                case "any":
                case "empty":
                case "is":
                case "isnt":
                case "contains_mc":
                case "nocontains_mc":
                case "any_mc":
                case "is_mc":
                case "isnt_mc":
                case "lt":
                case "gt":
                case "lte":
                case "gte":
                case "lt_mc":
                case "gt_mc":
                case "lte_mc":
                case "gte_mc":
                case "isnum":
                case "isstring":
                case "isbool":
                case "isenum":
                case "isbox":
                    return FunctionType.Condition;
            }
            return FunctionType.Extract;
        }
        #endregion

        #region Properties

        #region Flow
        private void SelectProperty(TextStyle style, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "format":
                case "formatting":
                    Reset(style.Formatting);
                    break;
                case "size":
                    Reset(style.Size);
                    break;
                case "color":
                case "foreground":
                    Reset(style.HexColor);
                    break;
                case "background":
                    Reset(style.HexMarkerColor);
                    break;
            }
        }

        private void SelectProperty(Column column, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "name":
                case "title":
                case "caption":
                    Reset(column.Caption);
                    break;
                case "type":
                    Reset(column.Type);
                    break;
                default:
                    Reset(column.Caption);
                    break;
            }
        }

        private void SelectProperty(Audio audio, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "id":
                case "identity":
                case "identifier":
                    Reset(audio.Identifier);
                    break;
                case "title":
                case "name":
                    Reset(audio.Title);
                    break;
                case "type":
                    Reset(audio.Type);
                    break;
                case "source":
                    Reset(audio.Source);
                    break;
            }
        }

        private void SelectProperty(Table table, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "summary":
                case "abstract":
                case "lead":
                    Reset(table.Abstract);
                    break;
                case "columns":
                    Reset(table.Columns);
                    break;
                case "name":
                case "title":
                    Reset(table.Name);
                    break;
                case "rows":
                    Reset(table.Rows);
                    break;
                case "type":
                    Reset(table.Type);
                    break;
            }
        }

        private void SelectProperty(FormContent form, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "id":
                case "identity":
                case "identifier":
                    Reset(form.Identifier);
                    break;
                case "title":
                case "name":
                    Reset(form.Title);
                    break;
                case "type":
                    Reset(form.Type);
                    break;
                case "source":
                    Reset(form.Source);
                    break;
            }
        }

        private void SelectProperty(Video video, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "id":
                case "identity":
                case "identifier":
                    Reset(video.Identifier);
                    break;
                case "title":
                case "name":
                    Reset(video.Title);
                    break;
                case "type":
                    Reset(video.Type);
                    break;
                case "source":
                    Reset(video.Source);
                    break;
            }
        }

        private void SelectProperty(Image image, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "id":
                case "identity":
                case "identifier":
                    Reset(image.Identifier);
                    break;
                case "title":
                case "name":
                    Reset(image.Title);
                    break;
                case "type":
                    Reset(image.Type);
                    break;
                case "source":
                    Reset(image.Source);
                    break;
            }
        }

        private void SelectProperty(Quote quote, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "style":
                    Reset(quote.Style);
                    break;
                case "value":
                case "text":
                    Reset(quote.Value);
                    break;
                case "type":
                    Reset(quote.Type);
                    break;
            }
        }

        private void SelectProperty(Text text, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "style":
                    Reset(text.Style);
                    break;
                case "value":
                case "text":
                    Reset(text.Value);
                    break;
                case "type":
                    Reset(text.Type);
                    break;
            }
        }

        private void SelectProperty(Link link, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "link":
                case "href":
                case "url":
                    Reset(link.Href);
                    break;
                case "value":
                case "text":
                    Reset(link.Value);
                    break;
                case "type":
                    Reset(link.Type);
                    break;
            }
        }

        private void SelectProperty(Paragraph paragraph, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "parts":
                case "items":
                    Reset(paragraph.Parts);
                    break;
                case "type":
                    Reset(paragraph.Type);
                    break;
            }
        }

        private void SelectProperty(Section section, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "parts":
                case "items":
                    Reset(section.Parts);
                    break;
                case "type":
                    Reset(section.Type);
                    break;
            }
        }

        private void SelectProperty(Row row, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "cells":
                case "items":
                    Reset(row.Cells);
                    break;
                case "type":
                    Reset(row.Type);
                    break;
            }
        }

        private void SelectProperty(List list, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "items":
                    Reset(list.Items);
                    break;
                case "type":
                    Reset(list.Type);
                    break;
            }
        }

        private void SelectProperty(Audioplayer audioplayer, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "name":
                case "title":
                    Reset(audioplayer.Title);
                    break;
                case "items":
                case "tracks":
                    Reset(audioplayer.Tracks);
                    break;
                case "type":
                    Reset(audioplayer.Type);
                    break;
            }
        }

        private void SelectProperty(Gallery gallery, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "name":
                case "title":
                    Reset(gallery.Title);
                    break;
                case "items":
                case "images":
                    Reset(gallery.Images);
                    break;
                case "type":
                    Reset(gallery.Type);
                    break;
            }
        }

        private void SelectProperty(Videoplayer videoplayer, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "name":
                case "title":
                    Reset(videoplayer.Title);
                    break;
                case "items":
                case "playlist":
                    Reset(videoplayer.Playlist);
                    break;
                case "type":
                    Reset(videoplayer.Type);
                    break;
            }
        }
        #endregion

        private void SelectProperty(TimeSpan time, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "days":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.TotalDays);
                    }
                    else
                    {
                        Reset(time.TotalDays.ToString(propertyIndex));
                    }
                    break;
                case "hours":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.TotalHours);
                    }
                    else
                    {
                        Reset(time.TotalHours.ToString(propertyIndex));
                    }
                    break;
                case "minutes":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.TotalMinutes);
                    }
                    else
                    {
                        Reset(time.TotalMinutes.ToString(propertyIndex));
                    }
                    break;
                case "seconds":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.TotalSeconds);
                    }
                    else
                    {
                        Reset(time.TotalSeconds.ToString(propertyIndex));
                    }
                    break;
                case "milliseconds":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.TotalMilliseconds);
                    }
                    else
                    {
                        Reset(time.TotalMilliseconds.ToString(propertyIndex));
                    }
                    break;
                case "ticks":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(time.Ticks);
                    }
                    else
                    {
                        Reset(time.Ticks.ToString(propertyIndex));
                    }
                    break;
            }
        }

        private void SelectProperty(TContentElement content, string property, string propertyIndex)
        {
            Reset(content.Find(property, propertyIndex));
        }

        private void SelectProperty(DOMRenderElementCounter counter, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "list": Reset(counter.ListId); break;
                case "listitem": Reset(counter.ListItemId); break;
                case "text": Reset(counter.TextId); break;
                case "link": Reset(counter.LinkId); break;
                case "image": Reset(counter.ImageId); break;
                case "quote": Reset(counter.QuoteId); break;
                case "video": Reset(counter.VideoId); break;
                case "audio": Reset(counter.AudioId); break;
                case "form": Reset(counter.FormId); break;
                case "section": Reset(counter.SectionId); break;
                case "paragraph": Reset(counter.ParagraphId); break;
                case "table": Reset(counter.TableId); break;
                case "column": Reset(counter.ColumnId); break;
                case "tablerow": Reset(counter.RowId); break;
                case "tablecell": Reset(counter.CellId); break;
                case "videoplayer": Reset(counter.VideoplayerId); break;
                case "audioplayer": Reset(counter.AudioplayerId); break;
                case "gallery": Reset(counter.GalleryId); break;
            }
        }

        private void SelectProperty(TEnvironment env, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "user":
                case "username":
                    Reset(Environment.UserName);
                    break;
                case "domain":
                case "domainname":
                    Reset(Environment.UserDomainName);
                    break;
                case "host":
                case "hostname":
                case "machine":
                case "machinename":
                    Reset(Environment.MachineName);
                    break;
                case "tick":
                case "tickcount":
                    Reset(Environment.TickCount);
                    break;

                case "subs":
                    Reset(env.SubscriptionName);
                    break;
                case "subsid":
                case "subscriptionid":
                    Reset(env.SubscriptionId);
                    break;
                case "file":
                case "filename":
                    Reset(env.FileName);
                    break;
                case "delay":
                    Reset(env.Delay);
                    break;
                case "contract":
                    Reset(env.ContractName);
                    break;

                case "encoding":
                    Reset(env.Encoding.HeaderName);
                    break;
                case "encodingfullname":
                    Reset(env.Encoding.EncodingName);
                    break;
                case "encodingpage":
                case "encodingcode":
                case "encodingcodepage":
                    Reset(env.Encoding.CodePage);
                    break;
            }
        }

        private void SelectProperty(Identifier identifier, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "version":
                    Reset(identifier.Version);
                    break;
                case "time":
                case "timestamp":
                    Reset(identifier.Timestamp);
                    break;
                case "date":
                case "datelabel":
                    Reset(identifier.DateLabel);
                    break;
            }
        }

        private void SelectProperty(TagMetadata tags, string property, string propertyIndex)
        {
            IList enumerable = null;
            switch (property.Trim().ToLowerInvariant())
            {
                case "places":
                    enumerable = tags.Places;
                    break;
                case "companies":
                    enumerable = tags.Companies;
                    break;
                case "persons":
                    enumerable = tags.Persons;
                    break;
                case "keywords":
                    enumerable = tags.Keywords;
                    break;
            }

            if (enumerable != null)
            {
                int index;
                if (int.TryParse(propertyIndex, out index))
                {
                    if (index < 0) index = 0;
                    if (index >= enumerable.Count) index = enumerable.Count - 1;
                    Reset(enumerable[index]);
                }
                else
                {
                    Reset(enumerable);
                }
            }
        }

        private void SelectProperty(DescriptiveMetadata descriptive, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "byline":
                case "author":
                    Reset(descriptive.Byline);
                    break;
                case "copyright":
                    Reset(descriptive.CopyrightNotice);
                    break;
                case "created":
                case "date":
                case "time":
                case "datetime":
                    Reset(descriptive.Created);
                    break;
                case "lang":
                case "language":
                    Reset(descriptive.Language);
                    break;
                case "original":
                    Reset(descriptive.Original);
                    break;
                case "priority":
                    Reset(descriptive.Priority);
                    break;
                case "publisher":
                    Reset(descriptive.Publisher);
                    break;
                case "source":
                    Reset(descriptive.Source);
                    break;
                case "headers":
                    {
                        int index;
                        if (int.TryParse(propertyIndex, out index))
                        {
                            if (index < 0) index = 0;
                            if (index >= descriptive.Headers.Count) index = descriptive.Headers.Count - 1;
                            Reset(descriptive.Headers[index]);
                        }
                        else
                        {
                            Reset(descriptive.Headers);
                        }
                    }
                    break;
            }
        }

        private void SelectProperty(string line, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "length":
                case "count":
                    Reset(line.Length);
                    break;
            }
        }

        private void SelectProperty(DateTime dt, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "day":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Day);
                    }
                    else
                    {
                        Reset(dt.Day.ToString(propertyIndex));
                    }
                    break;
                case "year":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Year);
                    }
                    else
                    {
                        Reset(dt.Year.ToString(propertyIndex));
                    }
                    break;
                case "month":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Month);
                    }
                    else
                    {
                        Reset(dt.Month.ToString(propertyIndex));
                    }
                    break;
                case "date":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Date);
                    }
                    else
                    {
                        Reset(dt.Date.ToString(propertyIndex));
                    }
                    break;
                case "hour":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Hour);
                    }
                    else
                    {
                        Reset(dt.Hour.ToString(propertyIndex));
                    }
                    break;
                case "minute":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Minute);
                    }
                    else
                    {
                        Reset(dt.Minute.ToString(propertyIndex));
                    }
                    break;
                case "second":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Second);
                    }
                    else
                    {
                        Reset(dt.Second.ToString(propertyIndex));
                    }
                    break;
                case "millisecond":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Millisecond);
                    }
                    else
                    {
                        Reset(dt.Millisecond.ToString(propertyIndex));
                    }
                    break;
                case "ticks":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.Ticks);
                    }
                    else
                    {
                        Reset(dt.Ticks.ToString(propertyIndex));
                    }
                    break;
                case "time":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.TimeOfDay);
                    }
                    else
                    {
                        Reset(dt.TimeOfDay.ToString(propertyIndex));
                    }
                    break;
                case "dayofweek":
                    if (string.IsNullOrWhiteSpace(propertyIndex) == false)
                    {
                        try
                        {
                            Reset(CultureInfo.GetCultureInfo(propertyIndex).DateTimeFormat.GetDayName(dt.DayOfWeek));
                        }
                        catch { }
                    }
                    else
                    {
                        Reset(dt.DayOfWeek.ToString());
                    }
                    break;
                case "dayofyear":
                    if (string.IsNullOrWhiteSpace(propertyIndex))
                    {
                        Reset(dt.DayOfYear);
                    }
                    else
                    {
                        Reset(dt.DayOfYear.ToString(propertyIndex));
                    }
                    break;
            }
        }

        private void SelectProperty(Agency agency, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "title":
                    Reset(agency.Title);
                    break;
                case "url":
                    Reset(agency.Url);
                    break;
                case "description":
                    Reset(agency.Description);
                    break;
                default:
                    Reset(agency.Title);
                    break;
            }
        }

        private void SelectProperty(Category category, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "title":
                    Reset(category.Title);
                    break;
                case "code":
                    Reset(category.Code);
                    break;
                case "description":
                    Reset(category.Description);
                    break;
                case "direction":
                    Reset(category.DirectionCode);
                    break;
                case "system":
                    Reset(category.IsSystem);
                    break;
                default:
                    Reset(category.Title);
                    break;
            }
        }

        private void SelectProperty(Header header, string property, string propertyIndex)
        {
            switch (property)
            {
                case "name":
                    Reset(header.Name);
                    break;
                case "value":
                    Reset(header.Value);
                    break;
                case "type":
                    Reset(header.Type);
                    break;
                case "tag":
                    Reset(header.Tag);
                    break;
                default:
                    Reset(header.Value);
                    break;
            }
        }

        private void SelectProperty(Tag tag, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "name":
                    Reset(tag.Name);
                    break;
                case "value":
                    Reset(tag.Value);
                    break;
                default:
                    Reset(tag.Value);
                    break;
            }
        }

        private void SelectProperty(AttachContent aside, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "caption":
                    Reset(aside.Caption);
                    break;
                case "contenttype":
                    Reset(aside.ContentType);
                    break;
                case "identity":
                    Reset(aside.Identity);
                    break;
                case "summary":
                    Reset(aside.Summary);
                    break;
                default:
                    Reset(aside.Caption);
                    break;
            }
        }

        private void SelectProperty(Assotiation assotiation, string property, string propertyIndex)
        {
            switch (property.Trim().ToLowerInvariant())
            {
                case "title":
                    Reset(assotiation.Title);
                    break;
                case "documentid":
                    Reset(assotiation.DocumentId);
                    break;
                case "relation":
                    Reset(assotiation.Relation);
                    break;
                case "description":
                    Reset(assotiation.Description);
                    break;
                default:
                    Reset(assotiation.Title);
                    break;
            }
        }

        private void SelectProperty(List<Header> header_list, string property, string propertyIndex)
        {
            var headers = header_list.
                Where(h => h.Name.ToLowerInvariant().
                Equals(property.Trim().ToLowerInvariant(), StringComparison.OrdinalIgnoreCase)).
                ToList();
            if (headers.Any())
            {
                int index;
                if (int.TryParse(propertyIndex, out index))
                {
                    if (index < 0) index = 0;
                    if (index >= headers.Count) index = headers.Count - 1;
                    Reset(headers[index]);
                }
                else
                {
                    if (headers.Count == 0)
                    {
                        Reset(null);
                    }
                    else if (headers.Count == 1)
                    {
                        Reset(headers[0]);
                    }
                    else
                    {
                        Reset(headers);
                    }
                }
            }
            else
            {
                Reset(null);
            }
        }

        private void SelectProperty(IDictionary dictionary, string property, string propertyIndex)
        {
            var typing = dictionary.GetType().GetGenericArguments();
            Type keyType = typing[0];
            Reset(dictionary[ConvertTo(property, keyType)]);
        }
        #endregion

        #region Functions
        private static string HtmlEncode(string text)
        {
            return HtmlUtility.EncodeHtmlEntities(text);
        }

        private static Func<string, string> MakeMapFunc(string[] args, bool ignorecase)
        {
            var map_dict = new Dictionary<string, string>();
            foreach (var arg in args)
            {
                var map_args = arg?.Split(new[] { "->" }, StringSplitOptions.RemoveEmptyEntries);
                if (map_args != null && map_args.Length == 2)
                {
                    if (ignorecase)
                    {
                        map_dict.Add(map_args[0].Trim().ToLowerInvariant(), map_args[1]);
                    }
                    else
                    {
                        map_dict.Add(map_args[0].Trim(), map_args[1]);
                    }
                }
            }
            return new Func<string, string>(s =>
            {
                var key = s.Trim();
                if (ignorecase) key = key.ToLowerInvariant();
                if (map_dict.ContainsKey(key))
                {
                    return map_dict[key];
                }
                return s;
            });
        }

        private void ApplyStringFunction(string function, TContainer[] args)
        {
            if (_current == null)
            {
                args = null;
                return;
            }
            //args = args_getter(this);
            switch (function)
            {
                case "tofilename":
                    {
                        Reset(FSUtils.FileNameCorrection(_current.ToString()));
                        break;
                    }
                case "topath":
                    {
                        Reset(FSUtils.PathCorrection(_current.ToString()));
                        break;
                    }
                case "join":
                    if (_current is IEnumerable && false == (_current is string))
                    {
                        if (args != null && args.Any())
                        {
                            var separator = args[0].ToString();
                            StringBuilder result = new StringBuilder();
                            foreach (var i in ((IEnumerable)_current))
                            {
                                var container = _factory.Get(i);
                                if (args.Length > 1)
                                {
                                    container.MoveToProperty(args[1].ToString(), null);
                                }
                                if (result.Length > 0) result.Append(separator);
                                result.Append(container.ToString());
                                _factory.Release(container);
                            }
                            Reset(result.ToString());
                        }
                    }
                    else if (_current is string)
                    {
                        var text = _current.ToString() + string.Join(string.Empty, args.Select(a => a.ToString()));
                        Reset(text);
                    }
                    break;
                case "format":
                    if (_current is DateTime)
                    {
                        var format = (args != null && args.Length > 0) ? args[0].ToString() : null;
                        var culture = (args != null && args.Length > 1) ? args[1].ToString() : null;
                        Reset(FormattedDateTime((DateTime)_current, format, culture));
                    }
                    break;
                case "tolower": Reset(this.ToString().ToLowerInvariant()); break;
                case "toupper": Reset(this.ToString().ToUpperInvariant()); break;
                case "xmlescape": Reset(XmlEscape(this.ToString())); break;
                case "jsonescape": Reset(JsonEscape(this.ToString())); break;
                case "htmlescape": Reset(HtmlEncode(this.ToString())); break;
                case "totitle":
                    {
                        CultureInfo cultureInfo = Thread.CurrentThread.CurrentCulture;
                        TextInfo textInfo = cultureInfo.TextInfo;
                        Reset(textInfo.ToTitleCase(this.ToString()));
                        break;
                    }
                case "trim": Reset(this.ToString().Trim()); break;
                case "joinright":
                    if (args.Length > 0)
                    {
                        Reset(string.Concat(this.ToString(), string.Join(string.Empty, args.Select(a => a.ToString()))));
                    }
                    break;
                case "joinleft":
                    if (args.Length > 0)
                    {
                        Reset(string.Concat(string.Join(string.Empty, args.Select(a => a.ToString())), this.ToString()));
                    }
                    break;
                case "insert":
                    {
                        int position;
                        string str;
                        if (args.Length == 1)
                        {
                            position = 0;
                            str = args[0].ToString();
                            Reset(this.ToString().Insert(position, str));
                        }
                        else if (args.Length == 2)
                        {
                            str = args[1].ToString();
                            int pos = args[0].As<int>();
                            var line = this.ToString();
                            if (pos >= 0 && pos < line.Length)
                            {
                                Reset(line.Insert(pos, str));
                            }
                            else
                            {
                                if (pos > line.Length)
                                {
                                    Reset(line.Insert(line.Length, str));
                                }
                                else
                                {
                                    Reset(line.Insert(0, str));
                                }
                            }
                        }
                    }
                    break;
                case "replace":
                    {
                        if (args.Length == 2)
                        {
                            Reset(this.ToString().Replace(args[0].ToString(), args[1].ToString()));
                        }
                    }
                    break;
                case "remove":
                    {
                        int start;
                        int count;
                        var line = this.ToString();
                        if (args.Length == 1)
                        {
                            start = args[0].As<int>();
                            if (start < line.Length)
                                Reset(line.Remove(start));
                            else
                                Reset(line);
                        }
                        else if (args.Length == 2)
                        {
                            start = args[0].As<int>();
                            count = args[1].As<int>();
                            start = args[0].As<int>();
                            if (start < line.Length)
                            {
                                Reset(line.Remove(start, count));
                            }
                            else
                                Reset(line);
                        }
                    }
                    break;
                case "index":
                    if (args.Length == 1)
                    {
                        Reset(this.ToString().IndexOf(args[0].ToString(), StringComparison.OrdinalIgnoreCase));
                    }
                    else if (args.Length == 2)
                    {
                        Reset(this.ToString().IndexOf(args[0].ToString(), args[1].As<int>(), StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        Reset(-1);
                    }
                    break;
                case "lastindex":
                    if (args.Length == 1)
                    {
                        Reset(this.ToString().LastIndexOf(args[0].ToString(), StringComparison.OrdinalIgnoreCase));
                    }
                    else if (args.Length == 2)
                    {
                        Reset(this.ToString().LastIndexOf(args[0].ToString(), args[1].As<int>(), StringComparison.OrdinalIgnoreCase));
                    }
                    else
                    {
                        Reset(-1);
                    }
                    break;
                case "map_mc":
                    if (args != null && args.Any())
                    {
                        string[] _args = args.Select(a => a.ToString()).ToArray();
                        Reset(MakeMapFunc(_args, false)(this.ToString()));
                    }
                    break;
                case "map":
                    if (args != null && args.Any())
                    {
                        string[] _args = args.Select(a => a.ToString()).ToArray();
                        Reset(MakeMapFunc(_args, true)(this.ToString()));
                    }
                    break;
                case "padleft":
                    {
                        if (args.Length > 0)
                        {
                            int pad = args[0].As<int>();
                            string sym = args[1].ToString();
                            if (args.Length == 2 && sym.Length > 0)
                            {
                                Reset(this.ToString().PadLeft(pad, sym[0]));
                            }
                            else if (args.Length == 1)
                            {
                                Reset(this.ToString().PadLeft(pad));
                            }
                            else
                            {
                                Reset(this.ToString());
                            }
                        }
                        else
                        {
                            Reset(this.ToString());
                        }
                    }
                    break;
                case "padright":
                    {
                        if (args.Length > 0)
                        {
                            int pad = args[0].As<int>();
                            string sym = args[1].ToString();
                            if (args.Length == 2 && sym.Length > 0)
                            {
                                Reset(this.ToString().PadRight(pad, sym[0]));
                            }
                            else if (args.Length == 1)
                            {
                                Reset(this.ToString().PadRight(pad));
                            }
                            else
                            {
                                Reset(this.ToString());
                            }
                        }
                        else
                        {
                            Reset(this.ToString());
                        }
                    }
                    break;
                case "substr":
                    {
                        if (args.Length == 1)
                        {
                            var line = this.ToString();
                            var start = args[0].As<int>();
                            if (start < 0) start = 0;
                            if (start >= line.Length) start = line.Length - 1;
                            Reset(this.ToString().Substring(start));
                        }
                        else if (args.Length == 2)
                        {
                            var line = this.ToString();
                            var start = args[0].As<int>();
                            if (start < 0) start = 0;
                            if (start >= line.Length) start = line.Length - 1;
                            var end = args[1].As<int>();
                            if (end < start) end = start;
                            if (end >= line.Length) end = line.Length;
                            Reset(this.ToString().Substring(start, end));
                        }
                        else
                        {
                            Reset(this.ToString());
                        }
                    }
                    break;
            }
        }

        private void ApplyConditionFunction(string function, TContainer[] args)
        {
            switch (function)
            {
                case "isnum": Reset(IsNumeric); break;
                case "isstring": Reset(IsString); break;
                case "isbool": Reset(IsBoolean); break;
                case "isenum": Reset(IsEnumerable); break;
                case "isbox": Reset(_current is TContainer); break;

                case "empty": Reset(IsEmpty()); break;
                case "contains": Reset(Contains(args, true)); break;
                case "contains_mc": Reset(Contains(args, false)); break;
                case "nocontains": Reset(NoContains(args, true)); break;
                case "nocontains_mc": Reset(NoContains(args, false)); break;
                case "any": Reset(Any(args, true)); break;
                case "any_mc": Reset(Any(args, false)); break;
                case "is":
                    if (args?.Length > 0)
                    {
                        Reset(Is(args[0], true));
                    }
                    break;
                case "is_mc":
                    if (args?.Length > 0)
                    {
                        Reset(Is(args[0], false));
                    }
                    break;
                case "isnt":
                    if (args?.Length > 0)
                    {
                        Reset(IsNot(args[0], true));
                    }
                    break;
                case "isnt_mc":
                    if (args?.Length > 0)
                    {
                        Reset(IsNot(args[0], false));
                    }
                    break;

                case "lt":
                    if (args?.Length > 0)
                    {
                        Reset(LessThan(args[0], true));
                    }
                    break;
                case "gt":
                    if (args?.Length > 0)
                    {
                        Reset(MoreThan(args[0], true));
                    }
                    break;
                case "lte":
                    if (args?.Length > 0)
                    {
                        Reset(LessOrEq(args[0], true));
                    }
                    break;
                case "gte":
                    if (args?.Length > 0)
                    {
                        Reset(MoreOrEq(args[0], true));
                    }
                    break;
                case "lt_mc":
                    if (args?.Length > 0)
                    {
                        Reset(LessThan(args[0], false));
                    }
                    break;
                case "gt_mc":
                    if (args?.Length > 0)
                    {
                        Reset(MoreThan(args[0], false));
                    }
                    break;
                case "lte_mc":
                    if (args?.Length > 0)
                    {
                        Reset(LessOrEq(args[0], false));
                    }
                    break;
                case "gte_mc":
                    if (args?.Length > 0)
                    {
                        Reset(MoreOrEq(args[0], false));
                    }
                    break;
            }
        }

        private void ApplyExtractionFunction(string function, Func<TContainer, TContainer[]> args_getter, out TContainer[] args)
        {
            if (_current == null)
            {
                if (function.Equals("append", StringComparison.OrdinalIgnoreCase))
                {
                    var list = new List<TContainer>();
                    args = args_getter(this);
                    foreach (var i in args)
                        list.Add(i);
                    Reset(list);
                }
                if (function.Equals("to", StringComparison.OrdinalIgnoreCase))
                {
                    args = args_getter(this);
                    if (args?.Length > 0)
                    {
                        var key = args[0].ToString();
                        if (_render.BufferDictionary.ContainsKey(key) == false)
                        {
                            _render.BufferDictionary.Add(key, this._current);
                        }
                        else
                        {
                            _render.BufferDictionary[key] = this._current;
                        }
                        Reset(null);
                    }
                }
                args = null;
                return;
            }
            if (function.Equals("where", StringComparison.OrdinalIgnoreCase))
            {
                if (args_getter != null)
                {
                    if (IsEnumerable)
                    {
                        var list = new TDList();
                        int index = 0;
                        foreach (var i in ((IEnumerable)_current))
                        {
                            if (i == null) continue;
                            var container = _factory.Get(i, index);
                            var conditions = args_getter(container);
                            if (conditions != null)
                            {
                                bool success = conditions.Any();
                                foreach (var c in conditions)
                                {
                                    success &= c.IsBoolean && c.As<bool>();
                                    _factory.Release(c);
                                }
                                if (success)
                                {
                                    list.Append(i);
                                }
                            }
                            _factory.Release(container);
                            index++;
                        }
                        Reset(list.Complete());
                    }
                }
                args = null;
                return;
            }
            else
            {
                args = args_getter(this);
                switch (function)
                {
                    case "to":
                        if (args?.Length > 0)
                        {
                            var key = args[0].ToString();
                            if (_render.BufferDictionary.ContainsKey(key) == false)
                            {
                                _render.BufferDictionary.Add(key, this._current);
                            }
                            else
                            {
                                _render.BufferDictionary[key] = this._current;
                            }
                            Reset(null);
                        }
                        break;

                    case "unbox": if (_current != null && _current is TContainer) Reset(((TContainer)_current).Current); break;

                    case "max": Max(); break;
                    case "min": Min(); break;
                    case "sort": Sort(args); break;
                    case "reverse": Reverse(); break;

                    case "inc": if (args?.Length > 0) Increment(args[0]); break;
                    case "dec": if (args?.Length > 0) Decrement(args[0]); break;
                    case "mul": if (args?.Length > 0) Multiply(args[0]); break;
                    case "div": if (args?.Length > 0) Divide(args[0]); break;
                    case "mod": if (args?.Length > 0) Mod(args[0]); break;

                    case "adddays": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Days); break;
                    case "addhours": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Hours); break;
                    case "addminutes": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Minutes); break;
                    case "addseconds": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Seconds); break;
                    case "addmonths": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Months); break;
                    case "addyears": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Years); break;
                    case "addmilliseconds": if (args?.Length > 0) ChangeDateTime(args[0], ChangeDateTimeType.Milliseconds); break;

                    case "count":
                        {
                            if (_current is IList)
                            {
                                Reset(((IList)_current).Count);
                            }
                            else if (_current is string)
                            {
                                var line = (string)_current;
                                Reset(line.Length);
                            }
                            else if (_current is IEnumerable)
                            {
                                int _i = 0;
                                foreach (var i in ((IEnumerable)_current)) _i++;
                                Reset(_i);
                            }
                            else if (_current is IDictionary)
                            {
                                Reset(((IDictionary)_current).Count);
                            }
                            else
                            {
                                Reset(_current == null ? 0 : 1);
                            }
                        }
                        break;
                    case "get":
                        {
                            if (args != null && args.Any())
                            {
                                int index = args[0].As<int>();
                                if (_current is IList)
                                {
                                    var list = (IList)_current;
                                    if (index >= 0 && index < list.Count)
                                    {
                                        Reset(list[index]);
                                    }
                                    else
                                    {
                                        Reset(null);
                                    }
                                }
                                else if (_current is string)
                                {
                                    var line = (string)_current;
                                    if (index >= 0 && index < line.Length)
                                    {
                                        Reset(line[index]);
                                    }
                                    else
                                    {
                                        Reset(null);
                                    }
                                }
                                else if (_current is IEnumerable)
                                {
                                    int _i = 0; bool found = false;
                                    foreach (var i in ((IEnumerable)_current))
                                    {
                                        if (_i == index)
                                        {
                                            found = true;
                                            Reset(i); break;
                                        }
                                        _i++;
                                    }
                                    if (found == false) Reset(null);
                                }
                            }
                        }
                        break;
                    case "select":
                        {
                            if (args?.Length > 0)
                            {
                                var property = args[0].ToString();
                                var property_index = args.Length > 1 ? args[1].ToString() : null;
                                if (_current is IEnumerable && false == (_current is string))
                                {
                                    var list = new TDList();
                                    foreach (var i in ((IEnumerable)_current))
                                    {
                                        if (i == null) continue;
                                        var container = _factory.Get(i);
                                        container.MoveToProperty(property, property_index);
                                        list.Append(container.Current);
                                        _factory.Release(container);
                                    }
                                    Reset(list.Complete());
                                }
                                else
                                {
                                    var container = _factory.Get(_current);
                                    container.MoveToProperty(property, property_index);
                                    Reset(container.Current);
                                    _factory.Release(container);
                                }
                            }
                        }
                        break;
                    case "apply":
                        {
                            if (args?.Length > 0)
                            {
                                var functionName = args[0].ToString();
                                var functionArgs = args.Skip(1);
                                var functionType = GetFunctionType(functionName);
                                if (_current is IEnumerable && false == (_current is string))
                                {
                                    var list = new TDList();
                                    foreach (var i in ((IEnumerable)_current))
                                    {
                                        if (i == null) continue;
                                        var container = _factory.Get(i);
                                        switch (functionType)
                                        {
                                            case FunctionType.String:
                                                container.ApplyStringFunction(functionName, functionArgs.ToArray());
                                                break;
                                            case FunctionType.Condition:
                                                container.ApplyConditionFunction(functionName, functionArgs.ToArray());
                                                break;
                                        }
                                        list.Append(container.Current);
                                        _factory.Release(container);
                                    }
                                    Reset(list.Complete());
                                }
                                else
                                {
                                    var container = _factory.Get(_current);
                                    switch (functionType)
                                    {
                                        case FunctionType.String:
                                            container.ApplyStringFunction(functionName, functionArgs.ToArray());
                                            break;
                                        case FunctionType.Condition:
                                            container.ApplyConditionFunction(functionName, functionArgs.ToArray());
                                            break;
                                    }
                                    Reset(container.Current);
                                    _factory.Release(container);
                                }
                            }
                        }
                        break;
                    case "distinct":
                        {
                            if (_current is IEnumerable)
                            {
                                Type elementType = ((IEnumerable)_current).GetType().GetGenericArguments()[0];
                                var genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                                foreach (var i in ((IEnumerable)_current))
                                {
                                    if (false == genericList.Contains(i))
                                    {
                                        genericList.Add(i);
                                    }
                                }
                                Reset(genericList);
                            }
                        }
                        break;
                    case "utc":
                        if (_current != null && (_current is DateTime))
                        {
                            Reset(((DateTime)_current).ToUniversalTime());
                        }
                        break;

                    case "tonum":
                    case "tonumber":
                        if (_current != null)
                        {
                            var buf = _current.ToString();
                            int num;
                            if (int.TryParse(buf, out num))
                            {
                                Reset(num);
                            }
                            else
                            {
                                Reset(0);
                            }
                        }
                        break;
                    case "append":
                        if (_current is List<TContainer>)
                        {
                            var list = _current as List<TContainer>;
                            foreach (var i in args)
                                list.Add(i);
                            Reset(list);
                            args = null;
                        }
                        break;
                }
            }
        }
        #endregion

        #region Math
        private void Multiply(TContainer value)
        {
            if (_current is byte) Reset(((byte)_current) * value.As<byte>());
            else if (_current is int) Reset(((int)_current) * value.As<int>());
            else if (_current is uint) Reset(((uint)_current) * value.As<uint>());
            else if (_current is long) Reset(((long)_current) * value.As<long>());
            else if (_current is ulong) Reset(((ulong)_current) * value.As<ulong>());
            else if (_current is short) Reset(((short)_current) * value.As<short>());
            else if (_current is ushort) Reset(((ushort)_current) * value.As<ushort>());
            else if (_current is float) Reset(((float)_current) * value.As<float>());
            else if (_current is double) Reset(((double)_current) * value.As<double>());
            else if (_current is decimal) Reset(((decimal)_current) * value.As<decimal>());
        }
        private void Divide(TContainer value)
        {
            if (_current is byte) Reset(((byte)_current) / value.As<byte>());
            else if (_current is int) Reset(((int)_current) / value.As<int>());
            else if (_current is uint) Reset(((uint)_current) / value.As<uint>());
            else if (_current is long) Reset(((long)_current) / value.As<long>());
            else if (_current is ulong) Reset(((ulong)_current) / value.As<ulong>());
            else if (_current is short) Reset(((short)_current) / value.As<short>());
            else if (_current is ushort) Reset(((ushort)_current) / value.As<ushort>());
            else if (_current is float) Reset(((float)_current) / value.As<float>());
            else if (_current is double) Reset(((double)_current) / value.As<double>());
            else if (_current is decimal) Reset(((decimal)_current) / value.As<decimal>());
        }
        private void Increment(TContainer value)
        {
            if (_current is byte) Reset(((byte)_current) + value.As<byte>());
            else if (_current is char) Reset((char)(((char)_current) + value.As<byte>()));
            else if (_current is int) Reset(((int)_current) + value.As<int>());
            else if (_current is uint) Reset(((uint)_current) + value.As<uint>());
            else if (_current is long) Reset(((long)_current) + value.As<long>());
            else if (_current is ulong) Reset(((ulong)_current) + value.As<ulong>());
            else if (_current is short) Reset(((short)_current) + value.As<short>());
            else if (_current is ushort) Reset(((ushort)_current) + value.As<ushort>());
            else if (_current is float) Reset(((float)_current) + value.As<float>());
            else if (_current is double) Reset(((double)_current) + value.As<double>());
            else if (_current is decimal) Reset(((decimal)_current) + value.As<decimal>());
            else if (_current is DateTime) Reset(new DateTime(((DateTime)_current).Ticks + value.As<long>()));
        }
        private void Decrement(TContainer value)
        {
            if (_current is byte) Reset(((byte)_current) - value.As<byte>());
            else if (_current is int) Reset(((int)_current) - value.As<int>());
            else if (_current is uint) Reset(((uint)_current) - value.As<uint>());
            else if (_current is long) Reset(((long)_current) - value.As<long>());
            else if (_current is ulong) Reset(((ulong)_current) - value.As<ulong>());
            else if (_current is short) Reset(((short)_current) - value.As<short>());
            else if (_current is ushort) Reset(((ushort)_current) - value.As<ushort>());
            else if (_current is float) Reset(((float)_current) - value.As<float>());
            else if (_current is double) Reset(((double)_current) - value.As<double>());
            else if (_current is decimal) Reset(((decimal)_current) - value.As<decimal>());
            else if (_current is DateTime) Reset(new DateTime(((DateTime)_current).Ticks - value.As<long>()));
        }
        private void Mod(TContainer value)
        {
            if (_current is byte) Reset(((byte)_current) % value.As<byte>());
            else if (_current is int) Reset(((int)_current) % value.As<int>());
            else if (_current is uint) Reset(((uint)_current) % value.As<uint>());
            else if (_current is long) Reset(((long)_current) % value.As<long>());
            else if (_current is ulong) Reset(((ulong)_current) % value.As<ulong>());
            else if (_current is short) Reset(((short)_current) % value.As<short>());
            else if (_current is ushort) Reset(((ushort)_current) % value.As<ushort>());
            else if (_current is float) Reset(((float)_current) % value.As<float>());
            else if (_current is double) Reset(((double)_current) % value.As<double>());
            else if (_current is decimal) Reset(((decimal)_current) % value.As<decimal>());
        }
        private void Max()
        {
            if (IsEnumerable)
            {
                Type elementType = ((IEnumerable)_current).GetType().GetGenericArguments()[0];
                if (elementType == typeof(byte))
                {
                    Reset(((IEnumerable<byte>)_current).Max());
                }
                else if (elementType == typeof(int))
                {
                    Reset(((IEnumerable<int>)_current).Max());
                }
                else if (elementType == typeof(uint))
                {
                    Reset(((IEnumerable<uint>)_current).Max());
                }
                else if (elementType == typeof(long))
                {
                    Reset(((IEnumerable<long>)_current).Max());
                }
                else if (elementType == typeof(ulong))
                {
                    Reset(((IEnumerable<ulong>)_current).Max());
                }
                else if (elementType == typeof(short))
                {
                    Reset(((IEnumerable<short>)_current).Max());
                }
                else if (elementType == typeof(ushort))
                {
                    Reset(((IEnumerable<ushort>)_current).Max());
                }
                else if (elementType == typeof(float))
                {
                    Reset(((IEnumerable<float>)_current).Max());
                }
                else if (elementType == typeof(double))
                {
                    Reset(((IEnumerable<double>)_current).Max());
                }
                else if (elementType == typeof(decimal))
                {
                    Reset(((IEnumerable<decimal>)_current).Max());
                }
                else if (elementType == typeof(string))
                {
                    Reset(((IEnumerable<string>)_current).Max());
                }
                else if (elementType == typeof(DateTime))
                {
                    Reset(((IEnumerable<DateTime>)_current).Max());
                }
            }
        }
        private void Min()
        {
            if (IsEnumerable)
            {
                Type elementType = ((IEnumerable)_current).GetType().GetGenericArguments()[0];
                if (elementType == typeof(byte))
                {
                    Reset(((IEnumerable<byte>)_current).Min());
                }
                else if (elementType == typeof(int))
                {
                    Reset(((IEnumerable<int>)_current).Min());
                }
                else if (elementType == typeof(uint))
                {
                    Reset(((IEnumerable<uint>)_current).Min());
                }
                else if (elementType == typeof(long))
                {
                    Reset(((IEnumerable<long>)_current).Min());
                }
                else if (elementType == typeof(ulong))
                {
                    Reset(((IEnumerable<ulong>)_current).Min());
                }
                else if (elementType == typeof(short))
                {
                    Reset(((IEnumerable<short>)_current).Min());
                }
                else if (elementType == typeof(ushort))
                {
                    Reset(((IEnumerable<ushort>)_current).Min());
                }
                else if (elementType == typeof(float))
                {
                    Reset(((IEnumerable<float>)_current).Min());
                }
                else if (elementType == typeof(double))
                {
                    Reset(((IEnumerable<double>)_current).Min());
                }
                else if (elementType == typeof(decimal))
                {
                    Reset(((IEnumerable<decimal>)_current).Min());
                }
                else if (elementType == typeof(string))
                {
                    Reset(((IEnumerable<string>)_current).Min());
                }
                else if (elementType == typeof(DateTime))
                {
                    Reset(((IEnumerable<DateTime>)_current).Min());
                }
            }
        }

        private void Sort(TContainer[] args)
        {
            if (IsEnumerable)
            {
                Type elementType = ((IEnumerable)_current).GetType().GetGenericArguments()[0];
                if (elementType == typeof(byte))
                {
                    Reset(((IEnumerable<byte>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(int))
                {
                    Reset(((IEnumerable<int>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(uint))
                {
                    Reset(((IEnumerable<uint>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(long))
                {
                    Reset(((IEnumerable<long>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(ulong))
                {
                    Reset(((IEnumerable<ulong>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(short))
                {
                    Reset(((IEnumerable<short>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(ushort))
                {
                    Reset(((IEnumerable<ushort>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(float))
                {
                    Reset(((IEnumerable<float>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(double))
                {
                    Reset(((IEnumerable<double>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(decimal))
                {
                    Reset(((IEnumerable<decimal>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(string))
                {
                    Reset(((IEnumerable<string>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(DateTime))
                {
                    Reset(((IEnumerable<DateTime>)_current).OrderBy(x => x));
                }
                else if (elementType == typeof(Category))
                {
                    string key = "title";
                    if (args?.Length == 1)
                    {
                        switch (args[0].ToString())
                        {
                            case "title": key = "title"; break;
                            case "code": key = "code"; break;
                            case "description": key = "description"; break;
                            case "direction": key = "direction"; break;
                            case "system": key = "system"; break;
                            default: key = "title"; break;
                        }
                    }
                    switch (key)
                    {
                        case "title": Reset(((IEnumerable<Category>)_current).OrderBy(x => x.Title)); break;
                        case "code": Reset(((IEnumerable<Category>)_current).OrderBy(x => x.Code)); break;
                        case "description": Reset(((IEnumerable<Category>)_current).OrderBy(x => x.Description)); break;
                        case "direction": Reset(((IEnumerable<Category>)_current).OrderBy(x => x.DirectionCode)); break;
                        case "system": Reset(((IEnumerable<Category>)_current).OrderBy(x => x.IsSystem)); break;
                    }
                }
                else if (elementType == typeof(Header))
                {
                    string key = "value";
                    if (args?.Length == 1)
                    {
                        switch (args[0].ToString())
                        {
                            case "name": key = "name"; break;
                            case "value": key = "value"; break;
                            case "type": key = "type"; break;
                            case "tag": key = "tag"; break;
                            default: key = "value"; break;
                        }
                    }
                    switch (key)
                    {
                        case "name": Reset(((IEnumerable<Header>)_current).OrderBy(x => x.Name)); break;
                        case "value": Reset(((IEnumerable<Header>)_current).OrderBy(x => x.Value)); break;
                        case "type": Reset(((IEnumerable<Header>)_current).OrderBy(x => x.Type)); break;
                        case "tag": Reset(((IEnumerable<Header>)_current).OrderBy(x => x.Tag)); break;
                    }
                }
                else if (elementType == typeof(Tag))
                {
                    string key = "name";
                    if (args?.Length == 1)
                    {
                        switch (args[0].ToString())
                        {
                            case "name": key = "name"; break;
                            case "value": key = "value"; break;
                            default: key = "name"; break;
                        }
                    }
                    switch (key)
                    {
                        case "name": Reset(((IEnumerable<Tag>)_current).OrderBy(x => x.Name)); break;
                        case "value": Reset(((IEnumerable<Tag>)_current).OrderBy(x => x.Value)); break;
                    }
                }
                else if (elementType == typeof(Agency))
                {
                    string key = "title";
                    if (args?.Length == 1)
                    {
                        switch (args[0].ToString())
                        {
                            case "title": key = "title"; break;
                            case "url": key = "url"; break;
                            case "description": key = "description"; break;
                            default: key = "title"; break;
                        }
                    }
                    switch (key)
                    {
                        case "title": Reset(((IEnumerable<Agency>)_current).OrderBy(x => x.Title)); break;
                        case "url": Reset(((IEnumerable<Agency>)_current).OrderBy(x => x.Url)); break;
                        case "description": Reset(((IEnumerable<Agency>)_current).OrderBy(x => x.Description)); break;
                    }
                }
            }
        }

        private void Reverse()
        {
            if (IsEnumerable)
            {
                Type elementType = ((IEnumerable)_current).GetType().GetGenericArguments()[0];
                var genericList = (IList)Activator.CreateInstance(typeof(List<>).MakeGenericType(elementType));
                foreach (var i in ((IEnumerable)_current))
                {
                    if (false == genericList.Contains(i))
                    {
                        genericList.Insert(0, i);
                    }
                }
                Reset(genericList);
            }
        }

        private enum ChangeDateTimeType
        {
            Days,
            Hours,
            Minutes,
            Seconds,
            Milliseconds,
            Months,
            Years
        }
        private void ChangeDateTime(TContainer value, ChangeDateTimeType type)
        {
            if (_current == null) return;
            if (_current is DateTime)
            {
                var dt = (DateTime)_current;
                switch (type)
                {
                    case ChangeDateTimeType.Days:
                        Reset(dt.AddDays(value.As<double>()));
                        break;
                    case ChangeDateTimeType.Hours:
                        Reset(dt.AddHours(value.As<double>()));
                        break;
                    case ChangeDateTimeType.Minutes:
                        Reset(dt.AddMinutes(value.As<double>()));
                        break;
                    case ChangeDateTimeType.Seconds:
                        Reset(dt.AddSeconds(value.As<double>()));
                        break;

                    case ChangeDateTimeType.Milliseconds:
                        Reset(dt.AddMilliseconds(value.As<double>()));
                        break;
                    case ChangeDateTimeType.Months:
                        Reset(dt.AddMonths(value.As<int>()));
                        break;
                    case ChangeDateTimeType.Years:
                        Reset(dt.AddYears(value.As<int>()));
                        break;
                }
            }
        }
        #endregion

        #region Conditions
        public bool Any(TContainer[] set = null, bool ignoreCase = true)
        {
            if (_current == null) return false;
            if (set.Any())
            {
                if (_current is IEnumerable && false == (_current is string))
                {
                    foreach (var c in (IEnumerable)_current)
                    {
                        foreach (var t in set)
                        {
                            if (CompareWith(c, t, ignoreCase) == 0) return true;
                        }
                    }
                }
                else
                {
                    foreach (var t in set)
                    {
                        if (CompareWith(t, ignoreCase) == 0) return true;
                    }
                }
                return false;
            }
            return string.IsNullOrWhiteSpace(this.ToString()) == false;
        }

        public bool Contains(TContainer[] set, bool ignoreCase)
        {
            if (_current == null) return false;
            if (set == null || set?.Length == 0) return false;
            if (_current is IEnumerable && false == (_current is string))
            {
                foreach (var t in set)
                {
                    bool contains = false;
                    foreach (var c in (IEnumerable)_current)
                    {
                        if (CompareWith(c, t, ignoreCase) == 0)
                        {
                            contains = true;
                        }
                    }
                    if (contains == false) return false;
                }
                return true;
            }
            else if (_current is string)
            {
                var line = (string)_current;
                foreach (var t in set)
                {
                    if (line.IndexOf(t.ToString(), StringComparison.OrdinalIgnoreCase) == -1) return false;
                }
                return true;
            }
            else
            {
                foreach (var t in set)
                {
                    if (CompareWith(t, ignoreCase) != 0) return false;
                }
            }
            return true;
        }

        public bool NoContains(TContainer[] test, bool ignoreCase)
        {
            if (_current == null) return false;
            if (_current == null) return false;
            if (_current is IEnumerable && false == (_current is string))
            {
                foreach (var c in (IEnumerable)_current)
                {
                    foreach (var t in test)
                    {
                        if (CompareWith(c, t, ignoreCase) == 0) return false;
                    }
                }
            }
            else
            {
                foreach (var t in test)
                {
                    if (CompareWith(t, ignoreCase) == 0) return false;
                }
            }
            return true;
        }

        public bool IsEmpty()
        {
            if (_current == null) return true;
            return String.IsNullOrWhiteSpace(_current.ToString());
        }

        public bool Is(TContainer test, bool ignoreCase)
        {
            if (_current == null) return test.Current == null;
            return CompareWith(test, ignoreCase) == 0;
        }

        public bool IsNot(TContainer test, bool ignoreCase)
        {
            if (_current == null) return test.Current != null;
            return CompareWith(test, ignoreCase) != 0;
        }

        public bool LessThan(TContainer test, bool ignoreCase)
        {
            if (_current == null) return false;
            return CompareWith(test, ignoreCase) < 0;
        }

        public bool MoreThan(TContainer test, bool ignoreCase)
        {
            if (_current == null) return false;
            return CompareWith(test, ignoreCase) > 0;
        }

        public bool LessOrEq(TContainer test, bool ignoreCase)
        {
            if (_current == null) return false;
            return CompareWith(test, ignoreCase) <= 0;
        }

        public bool MoreOrEq(TContainer test, bool ignoreCase)
        {
            if (_current == null) return false;
            return CompareWith(test, ignoreCase) >= 0;
        }

        private int CompareWith(TContainer test, bool ignoreCase)
        {
            return CompareWith(_current, test, ignoreCase);
        }

        private static int CompareWith(object val, TContainer test, bool ignoreCase)
        {
            if (val is string)
            {
                return string.Compare((string)val, test.As<string>(), ignoreCase);
            }
            else if (val is DateTime)
            {
                return DateTime.Compare((DateTime)val, test.As<DateTime>());
            }
            else if (val is bool)
            {
                var t = test.As<bool>();
                switch ((bool)val)
                {
                    case true:
                        if (t) return 0;
                        return 1;
                    case false:
                        if (!t) return 0;
                        return -1;
                }
            }
            else if (val is int)
            {
                return ((int)val).CompareTo(test.As<int>());
            }
            else if (val is long)
            {
                return ((long)val).CompareTo(test.As<long>());
            }
            else if (val is byte)
            {
                return ((byte)val).CompareTo(test.As<byte>());
            }
            else if (val is short)
            {
                return ((short)val).CompareTo(test.As<short>());
            }
            else if (val is uint)
            {
                return ((uint)val).CompareTo(test.As<uint>());
            }
            else if (val is ulong)
            {
                return ((ulong)val).CompareTo(test.As<ulong>());
            }
            else if (val is ushort)
            {
                return ((ushort)val).CompareTo(test.As<ushort>());
            }
            else if (val is float)
            {
                return ((float)val).CompareTo(test.As<float>());
            }
            else if (val is double)
            {
                return ((double)val).CompareTo(test.As<double>());
            }
            else if (val is decimal)
            {
                return ((decimal)val).CompareTo(test.As<decimal>());
            }
            else if (val is Guid)
            {
                return ((Guid)val).CompareTo(test.As<Guid>());
            }
            return string.Compare(val.ToString(), test.ToString(), ignoreCase);
        }

        private static T ConvertTo<T>(string line)
        {
            return (T)StringToTypeConverter.TryConvert(line, typeof(T));
        }
        private static object ConvertTo(string line, Type type)
        {
            return StringToTypeConverter.TryConvert(line, type);
        }
        #endregion

        #region Helpers
        private static string XmlEscape(string unescaped)
        {
            return SecurityElement.Escape(unescaped);
        }

        private static string JsonEscape(string s)
        {
            return JsonEscaper.EscapeString(s);
        }
        private const string DEFAULT_DATETIME_FORMAT = "yyyy-MM-dd HH:mm:ss";
        private static string FormattedDateTime(DateTime dt, string format = null, string culture = null)
        {
            CultureInfo ci;
            if (culture != null)
            {
                try
                {
                    ci = CultureInfo.GetCultureInfo(culture);
                }
                catch
                {
                    ci = CultureInfo.CurrentCulture;
                }
            }
            else
            {
                ci = CultureInfo.CurrentCulture;
            }
            if (false == string.IsNullOrWhiteSpace(format))
            {
                try
                {
                    return dt.ToString(format, ci);
                }
                catch { }
            }
            return dt.ToString(DEFAULT_DATETIME_FORMAT, ci);
        }
        #endregion

        public override string ToString()
        {
            if (_current == null) return string.Empty;

            if (_current is string) return (string)_current;
            else if (_current is DateTime) return FormattedDateTime((DateTime)_current);
            else if (_current is TimeSpan) return ((TimeSpan)_current).ToString();
            else if (_current is Agency) return ((Agency)_current).Title ?? string.Empty;
            else if (_current is Category) return ((Category)_current).Title ?? string.Empty;
            else if (_current is Header) return ((Header)_current).Value ?? string.Empty;
            else if (_current is Tag) return ((Tag)_current).Name ?? string.Empty;
            else if (_current is AttachContent) return ((AttachContent)_current).Caption ?? string.Empty;
            else if (_current is Assotiation) return ((Assotiation)_current).Title ?? string.Empty;
            else if (_current is List<Header>) return string.Join("; ", ((List<Header>)_current).Select(h => h.Name));
            else if (_current is Identifier) return string.Empty;
            else if (_current is TagMetadata) return string.Empty;
            else if (_current is DescriptiveMetadata) return string.Empty;
            else if (_current is TEnvironment) return string.Empty;
            else if (_current is CustomBlocks) return string.Empty;
            else if (_current is DOMRenderElementCounter) return string.Empty;
            else if (_current is Audio) return ((Audio)_current).Title ?? string.Empty;
            else if (_current is Table) return ((Table)_current).Name ?? string.Empty;
            else if (_current is FormContent) return ((FormContent)_current).Title ?? string.Empty;
            else if (_current is Image) return ((Image)_current).Title ?? string.Empty;
            else if (_current is Link) return ((Link)_current).Value ?? string.Empty;
            else if (_current is Quote) return ((Quote)_current).Value ?? string.Empty;
            else if (_current is Text) return ((Text)_current).Value ?? string.Empty;
            else if (_current is Video) return ((Video)_current).Title ?? string.Empty;
            else if (_current is Paragraph) return "Paragraph";
            else if (_current is Section) return "Section";
            else if (_current is Row) return "Row";
            else if (_current is Column) return ((Column)_current).Caption ?? string.Empty;
            else if (_current is List) return "List";
            else if (_current is Audioplayer) return ((Audioplayer)_current).Title.Value ?? string.Empty;
            else if (_current is Gallery) return ((Gallery)_current).Title.Value ?? string.Empty;
            else if (_current is Videoplayer) return ((Videoplayer)_current).Title.Value ?? string.Empty;
            else if (_current is TextStyle) return string.Empty;

            if (_current is IEnumerable)
            {
                var separator = "; ";
                StringBuilder result = new StringBuilder();
                foreach (var i in ((IEnumerable)_current))
                {
                    var container = (i is TContainer) ? (TContainer)i : _factory.Get(i);
                    if (result.Length > 0) result.Append(separator);
                    result.Append(container.ToString());
                    if (!(i is TContainer))
                        _factory.Release(container);
                }
                return result.ToString();
            }
            return _current?.ToString() ?? string.Empty;
        }
    }
}