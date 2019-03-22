using DOM.DSL.Contracts;
using DOM.DSL.Model;
using DOM.DSL.Tokens;
using System.Text;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.DocumentObjectModel.Flow;

namespace DOM.DSL.Services
{
    internal sealed class TContentToStringConverter :
        IContentReader<string>
    {
        private readonly StringBuilder _builder;
        private readonly TRender _render;
        private readonly TFlowRules _transformRules;
        private readonly ISpecialTableBuilder _specialTableBuilder;

        private bool _useSpecialTableBuilder = false;

        public TContentToStringConverter(TRender render, TFlowRules rules)
        {
            _render = render;
            _transformRules = rules;
            _specialTableBuilder = rules.SpecialTableBuilder;
            _useSpecialTableBuilder = rules.UseSpecialTableBuilder && rules.SpecialTableBuilder != null;

            _builder = new StringBuilder();
        }

        public string Complete()
        {
            return _builder.ToString();
        }

        private void WriteText(string text)
        {
            if (_useSpecialTableBuilder && _specialTableBuilder.WaitCellBody)
            {
                _specialTableBuilder.WriteToCell(text);
            }
            else
            {
                _builder.Append(text);
            }
        }

        private string Resolve(TBlockToken token, object value, int order)
        {
            StringBuilder text = new StringBuilder();
            var self = _render.Factory.Get(value, order);
            _render.Resolve(token, c => text.Append(c.ToString()), true, self);
            _render.Factory.Release(self);
            return text.ToString();
        }

        #region Primitives
        public void ReadAudio(Audio audio, int order)
        {
            _render.Counter.IncAudioId();
            if (_transformRules.AudioPrefix != null)
            {
                WriteText(Resolve(_transformRules.AudioPrefix, audio, order));
            }
            if (_transformRules.AudioTemplate != null)
            {
                WriteText(Resolve(_transformRules.AudioTemplate, audio, order));
            }
            if (_transformRules.AudioPostfix != null)
            {
                WriteText(Resolve(_transformRules.AudioPostfix, audio, order));
            }
        }

        public void ReadColumn(Table table, Column column, int order)
        {
            _render.Counter.IncColumnId();
            if (_transformRules.ColumnPrefix != null)
            {
                WriteText(Resolve(_transformRules.ColumnPrefix, column, order));
            }
            if (_transformRules.ColumnTemplate != null)
            {
                WriteText(Resolve(_transformRules.ColumnTemplate, column, order));
            }
            if (_transformRules.ColumnPostfix != null)
            {
                WriteText(Resolve(_transformRules.ColumnPostfix, column, order));
            }
        }

        public void ReadForm(FormContent form)
        {
            _render.Counter.IncFormId();
            if (_transformRules.FormPrefix != null)
            {
                WriteText(Resolve(_transformRules.FormPrefix, form, 0));
            }
            if (_transformRules.FormTemplate != null)
            {
                WriteText(Resolve(_transformRules.FormTemplate, form, 0));
            }
            if (_transformRules.FormPostfix != null)
            {
                WriteText(Resolve(_transformRules.FormPostfix, form, 0));
            }
        }

        public void ReadImage(Image image, int order)
        {
            _render.Counter.IncImageId();
            if (_transformRules.ImagePrefix != null)
            {
                WriteText(Resolve(_transformRules.ImagePrefix, image, order));
            }
            if (_transformRules.ImageTemplate != null)
            {
                WriteText(Resolve(_transformRules.ImageTemplate, image, order));
            }
            if (_transformRules.ImagePostfix != null)
            {
                WriteText(Resolve(_transformRules.ImagePostfix, image, order));
            }
        }

        public void ReadLink(Link link, int order)
        {
            _render.Counter.IncLinkId();
            if (_transformRules.LinkPrefix != null)
            {
                WriteText(Resolve(_transformRules.LinkPrefix, link, order));
            }
            if (_transformRules.LinkTemplate != null)
            {
                WriteText(Resolve(_transformRules.LinkTemplate, link, order));
            }
            if (_transformRules.LinkPostfix != null)
            {
                WriteText(Resolve(_transformRules.LinkPostfix, link, order));
            }
        }

        public void ReadQuote(Quote quote)
        {
            _render.Counter.IncQuoteId();
            if (_transformRules.QuotePrefix != null)
            {
                WriteText(Resolve(_transformRules.QuotePrefix, quote, 0));
            }
            if (_transformRules.QuoteTemplate != null)
            {
                WriteText(Resolve(_transformRules.QuoteTemplate, quote, 0));
            }
            if (_transformRules.QuotePostfix != null)
            {
                WriteText(Resolve(_transformRules.QuotePostfix, quote, 0));
            }
        }

        public void ReadText(Text text)
        {
            _render.Counter.IncTextId();
            if (_transformRules.TextPrefix != null)
            {
                WriteText(Resolve(_transformRules.TextPrefix, text, 0));
            }
            if (_transformRules.TextTemplate != null)
            {
                WriteText(Resolve(_transformRules.TextTemplate, text, 0));
            }
            if (_transformRules.TextPostfix != null)
            {
                WriteText(Resolve(_transformRules.TextPostfix, text, 0));
            }
        }

        public void ReadVideo(Video video, int order)
        {
            _render.Counter.IncVideoId();
            if (_transformRules.VideoPrefix != null)
            {
                WriteText(Resolve(_transformRules.VideoPrefix, video, order));
            }
            if (_transformRules.VideoTemplate != null)
            {
                WriteText(Resolve(_transformRules.VideoTemplate, video, order));
            }
            if (_transformRules.VideoPostfix != null)
            {
                WriteText(Resolve(_transformRules.VideoPostfix, video, order));
            }
        }
        #endregion

        #region Containers
        public void EnterParagraph(Paragraph paragraph)
        {
            _render.Counter.IncParagraphId();
            if (_transformRules.ParagraphPrefix != null)
            {
                WriteText(Resolve(_transformRules.ParagraphPrefix, paragraph, _render.Counter.ParagraphId));
            }
        }
        public void LeaveParagraph(Paragraph paragraph)
        {
            if (_transformRules.ParagraphPostfix != null)
            {
                WriteText(Resolve(_transformRules.ParagraphPostfix, paragraph, _render.Counter.ParagraphId));
            }
        }
        public void EnterSection(Section section)
        {
            _render.Counter.IncSectionId();
            if (_transformRules.SectionPrefix != null)
            {
                WriteText(Resolve(_transformRules.SectionPrefix, section, _render.Counter.SectionId));
            }
        }
        public void LeaveSection(Section section)
        {
            if (_transformRules.SectionPostfix != null)
            {
                WriteText(Resolve(_transformRules.SectionPostfix, section, _render.Counter.SectionId));
            }
        }
        #endregion

        #region Table
        public void EnterTable(Table table)
        {
            _render.Counter.IncTableId();
            if (_transformRules.TablePrefix != null)
            {
                _builder.Append(Resolve(_transformRules.TablePrefix, table, _render.Counter.TableId));
            }
            if (_useSpecialTableBuilder)
            {
                _builder.AppendLine();
                _specialTableBuilder.EnterTable(table.Columns.ToArray());
            }
        }
        public void EnterColumns(Table table)
        {
            if (_useSpecialTableBuilder == false && _transformRules.ColumnsPrefix != null)
            {
                _builder.Append(Resolve(_transformRules.ColumnsPrefix, table.Columns, 0));
            }
        }
        public void EnterRow(Table table, Row row, int order)
        {
            _render.Counter.IncRowId();
            if (_useSpecialTableBuilder)
            {
                _specialTableBuilder.EnterRow(row.Cells.Count);
            }
            else if (_transformRules.RowPrefix != null)
            {
                _builder.Append(Resolve(_transformRules.RowPrefix, row, order));
            }
        }
        public void EnterRowCell(Table table, Row row, IContentElement cell, int order)
        {
            _render.Counter.IncCellId();
            if (_useSpecialTableBuilder)
            {
                _specialTableBuilder.EnterCell(order);
            }
            else
            {
                if (order == 0 && _transformRules.FirstRowCellPrefix != null)
                {
                    _builder.Append(Resolve(_transformRules.FirstRowCellPrefix, cell, order));
                }
                else if (_transformRules.CellPrefix != null)
                {
                    _builder.Append(Resolve(_transformRules.CellPrefix, cell, order));
                }
            }
        }
        public void LeaveColumns(Table table)
        {
            if (_useSpecialTableBuilder == false && _transformRules.ColumnsPostfix != null)
            {
                _builder.Append(Resolve(_transformRules.ColumnsPostfix, table.Columns, 0));
            }
        }
        public void LeaveRow(Table table, Row row, int order)
        {
            if (_useSpecialTableBuilder)
            {
                _specialTableBuilder.LeaveRow();
            }
            else if (_transformRules.RowPostfix != null)
            {
                _builder.Append(Resolve(_transformRules.RowPostfix, row, order));
            }
        }
        public void LeaveRowCell(Table table, Row row, IContentElement cell, int order)
        {
            if (_useSpecialTableBuilder)
            {
                _specialTableBuilder.LeaveCell();
            }
            else
            {
                if (order == 0 && _transformRules.FirstRowCellPostfix != null)
                {
                    _builder.Append(Resolve(_transformRules.FirstRowCellPostfix, cell, order));
                }
                else if (_transformRules.CellPostfix != null)
                {
                    _builder.Append(Resolve(_transformRules.CellPostfix, cell, order));
                }
            }
        }
        public void LeaveTable(Table table)
        {
            if (_useSpecialTableBuilder)
            {
                _specialTableBuilder.FlushTable(_builder);
            }
            if (_transformRules.TablePostfix != null)
            {
                _builder.Append(Resolve(_transformRules.TablePostfix, table, _render.Counter.TableId));
            }
        }
        #endregion

        #region List
        public void EnterList(List list)
        {
            _render.Counter.IncListId();
            if (_transformRules.ListPrefix != null)
            {
                WriteText(Resolve(_transformRules.ListPrefix, list, 0));
            }
        }
        public void EnterListItem(List list, IContentElement item, int order)
        {
            _render.Counter.IncListItemId();
            if (_transformRules.ListItemPrefix != null)
            {
                WriteText(Resolve(_transformRules.ListItemPrefix, item, order));
            }
        }
        public void LeaveList(List list)
        {
            if (_transformRules.ListPostfix != null)
            {
                WriteText(Resolve(_transformRules.ListPostfix, list, 0));
            }
        }
        public void LeaveListItem(List list, IContentElement item, int order)
        {
            if (_transformRules.ListItemPostfix != null)
            {
                WriteText(Resolve(_transformRules.ListItemPostfix, item, order));
            }
        }
        #endregion

        #region Media
        public void EnterAudioplayer(Audioplayer player)
        {
            _render.Counter.IncAudioplayerId();
            if (_transformRules.AudioplayerPrefix != null)
            {
                WriteText(Resolve(_transformRules.AudioplayerPrefix, player, 0));
            }
        }
        public void EnterGallery(Gallery gallery)
        {
            _render.Counter.IncGalleryId();
            if (_transformRules.GalleryPrefix != null)
            {
                WriteText(Resolve(_transformRules.GalleryPrefix, gallery, 0));
            }
        }
        public void EnterVideoplayer(Videoplayer player)
        {
            _render.Counter.IncVideoplayerId();
            if (_transformRules.VideoplayerPrefix != null)
            {
                WriteText(Resolve(_transformRules.VideoplayerPrefix, player, 0));
            }
        }
        public void LeaveAudioplayer(Audioplayer player)
        {
            if (_transformRules.AudioplayerPostfix != null)
            {
                WriteText(Resolve(_transformRules.AudioplayerPostfix, player, 0));
            }
        }
        public void LeaveGallery(Gallery gallery)
        {
            if (_transformRules.GalleryPostfix != null)
            {
                WriteText(Resolve(_transformRules.GalleryPostfix, gallery, 0));
            }
        }
        public void LeaveVideoplayer(Videoplayer player)
        {
            if (_transformRules.VideoplayerPostfix != null)
            {
                WriteText(Resolve(_transformRules.VideoplayerPostfix, player, 0));
            }
        }
        #endregion
    }
}
