using DOM.DSL.Contracts;
using DOM.DSL.Services;
using DOM.DSL.Tokens;

namespace DOM.DSL.Model
{
    internal sealed class TFlowRules
    {
        #region Rules
        public TBlockToken ListPrefix;
        public TBlockToken ListPostfix;
        public TBlockToken ListItemPrefix;
        public TBlockToken ListItemPostfix;
        public TBlockToken TextPrefix;
        public TBlockToken TextTemplate;
        public TBlockToken TextPostfix;
        public TBlockToken LinkPrefix;
        public TBlockToken LinkTemplate;
        public TBlockToken LinkPostfix;
        public TBlockToken ImagePrefix;
        public TBlockToken ImageTemplate;
        public TBlockToken ImagePostfix;
        public TBlockToken QuotePrefix;
        public TBlockToken QuoteTemplate;
        public TBlockToken QuotePostfix;
        public TBlockToken VideoPrefix;
        public TBlockToken VideoTemplate;
        public TBlockToken VideoPostfix;
        public TBlockToken AudioPrefix;
        public TBlockToken AudioTemplate;
        public TBlockToken AudioPostfix;
        public TBlockToken TablePrefix;
        public TBlockToken TablePostfix;
        public TBlockToken SectionPrefix;
        public TBlockToken SectionPostfix;
        public TBlockToken ParagraphPrefix;
        public TBlockToken ParagraphPostfix;
        public TBlockToken ColumnsPrefix;
        public TBlockToken ColumnsPostfix;
        public TBlockToken ColumnPrefix;
        public TBlockToken ColumnTemplate;
        public TBlockToken ColumnPostfix;
        public TBlockToken RowPrefix;
        public TBlockToken RowPostfix;
        public TBlockToken FirstRowCellPrefix;
        public TBlockToken FirstRowCellPostfix;
        public TBlockToken CellPrefix;
        public TBlockToken CellPostfix;
        public TBlockToken FormPrefix;
        public TBlockToken FormTemplate;
        public TBlockToken FormPostfix;
        public TBlockToken AudioplayerPrefix;
        public TBlockToken AudioplayerPostfix;
        public TBlockToken VideoplayerPrefix;
        public TBlockToken VideoplayerPostfix;
        public TBlockToken GalleryPrefix;
        public TBlockToken GalleryPostfix;
        #endregion

        #region Special table builder
        public bool UseSpecialTableBuilder = false;
        public ISpecialTableBuilder SpecialTableBuilder;
        #endregion

        public void Bootstrap()
        {
            if (null == SectionPrefix) SectionPrefix = null;
            if (null == SectionPostfix) SectionPostfix = null;
            if (null == ParagraphPrefix) ParagraphPrefix = null;
            if (null == ParagraphPostfix) ParagraphPostfix = null;
            if (null == ListPrefix) ListPrefix = null;
            if (null == ListPostfix) ListPostfix = null;
            if (null == ListItemPrefix) ListItemPrefix = null;
            if (null == ListItemPostfix) ListItemPostfix = null;
            if (null == TablePrefix) TablePrefix = null;
            if (null == TablePostfix) TablePostfix = null;
            if (null == ColumnsPrefix) ColumnsPrefix = null;
            if (null == ColumnsPostfix) ColumnsPostfix = null;
            if (null == ColumnPrefix) ColumnPrefix = null;
            if (null == ColumnTemplate) ColumnTemplate = null;
            if (null == ColumnPostfix) ColumnPostfix = null;
            if (null == RowPrefix) RowPrefix = null;
            if (null == RowPostfix) RowPostfix = null;
            if (null == CellPrefix) CellPrefix = null;
            if (null == CellPostfix) CellPostfix = null;
            if (null == FirstRowCellPrefix) FirstRowCellPrefix = null;
            if (null == FirstRowCellPostfix) FirstRowCellPostfix = null;
            if (null == AudioplayerPrefix) AudioplayerPrefix = null;
            if (null == AudioplayerPostfix) AudioplayerPostfix = null;
            if (null == VideoplayerPrefix) VideoplayerPrefix = null;
            if (null == VideoplayerPostfix) VideoplayerPostfix = null;
            if (null == GalleryPrefix) GalleryPrefix = null;
            if (null == GalleryPostfix) GalleryPostfix = null;
            if (null == FormPrefix) FormPrefix = null;
            if (null == FormTemplate) FormTemplate = null;
            if (null == FormPostfix) FormPostfix = null;
            if (null == VideoPrefix) VideoPrefix = null;
            if (null == VideoTemplate) VideoTemplate = null;
            if (null == VideoPostfix) VideoPostfix = null;
            if (null == AudioPrefix) AudioPrefix = null;
            if (null == AudioTemplate) AudioTemplate = null;
            if (null == AudioPostfix) AudioPostfix = null;
            if (null == ImagePrefix) ImagePrefix = null;
            if (null == ImageTemplate) ImageTemplate = null;
            if (null == ImagePostfix) ImagePostfix = null;
            if (null == LinkPrefix) LinkPrefix = null;
            if (null == LinkTemplate) LinkTemplate = null;
            if (null == LinkPostfix) LinkPostfix = null;
            if (null == QuotePrefix) QuotePrefix = null;
            if (null == QuoteTemplate) QuoteTemplate = new TBlockToken(new[] { new TElementToken { ElementName = "self" } });
            if (null == QuotePostfix) QuotePostfix = null;
            if (null == TextPrefix) TextPrefix = null;
            if (null == TextTemplate) TextTemplate = new TBlockToken(new[] { new TElementToken { ElementName = "self" } });
            if (null == TextPostfix) TextPostfix = null;
        }

        public void UpdateRule(string elementName, string functionName, TBlockToken rule_token, string special)
        {
            switch (elementName)
            {
                case "list":
                    switch (functionName)
                    {
                        case "prefix":
                            ListPrefix = rule_token;
                            break;
                        case "postfix":
                            ListPostfix = rule_token;
                            break;
                        case "ignore":
                            ListPostfix = ListPrefix = null;
                            break;
                    }
                    break;
                case "listitem":
                    switch (functionName)
                    {
                        case "prefix":
                            ListItemPrefix = rule_token;
                            break;
                        case "postfix":
                            ListItemPostfix = rule_token;
                            break;
                        case "ignore":
                            ListItemPrefix = ListItemPostfix = null;
                            break;
                    }
                    break;
                case "text":
                    switch (functionName)
                    {
                        case "prefix":
                            TextPrefix = rule_token;
                            break;
                        case "template":
                            TextTemplate = rule_token;
                            break;
                        case "postfix":
                            TextPostfix = rule_token;
                            break;
                        case "ignore":
                            TextPrefix = TextTemplate = TextPostfix = null;
                            break;
                    }
                    break;
                case "link":
                    switch (functionName)
                    {
                        case "prefix":
                            LinkPrefix = rule_token;
                            break;
                        case "template":
                            LinkTemplate = rule_token;
                            break;
                        case "postfix":
                            LinkPostfix = rule_token;
                            break;
                        case "ignore":
                            LinkPrefix = LinkTemplate = LinkPostfix = null;
                            break;
                    }
                    break;
                case "image":
                    switch (functionName)
                    {
                        case "prefix":
                            ImagePrefix = rule_token;
                            break;
                        case "template":
                            ImageTemplate = rule_token;
                            break;
                        case "postfix":
                            ImagePostfix = rule_token;
                            break;
                        case "ignore":
                            ImagePrefix = ImageTemplate = ImagePostfix = null;
                            break;
                    }
                    break;
                case "quote":
                    switch (functionName)
                    {
                        case "prefix":
                            QuotePrefix = rule_token;
                            break;
                        case "template":
                            QuoteTemplate = rule_token;
                            break;
                        case "postfix":
                            QuotePostfix = rule_token;
                            break;
                        case "ignore":
                            QuotePrefix = QuoteTemplate = QuotePostfix = null;
                            break;
                    }
                    break;
                case "form":
                    switch (functionName)
                    {
                        case "prefix":
                            FormPrefix = rule_token;
                            break;
                        case "template":
                            FormTemplate = rule_token;
                            break;
                        case "postfix":
                            FormPostfix = rule_token;
                            break;
                        case "ignore":
                            FormPrefix = FormTemplate = FormPostfix = null;
                            break;
                    }
                    break;
                case "video":
                    switch (functionName)
                    {
                        case "prefix":
                            VideoPrefix = rule_token;
                            break;
                        case "template":
                            VideoTemplate = rule_token;
                            break;
                        case "postfix":
                            VideoPostfix = rule_token;
                            break;
                        case "ignore":
                            VideoPrefix = VideoTemplate = VideoPostfix = null;
                            break;
                    }
                    break;
                case "audio":
                    switch (functionName)
                    {
                        case "prefix":
                            AudioPrefix = rule_token;
                            break;
                        case "template":
                            AudioTemplate = rule_token;
                            break;
                        case "postfix":
                            AudioPostfix = rule_token;
                            break;
                        case "ignore":
                            AudioPrefix = AudioTemplate = AudioPostfix = null;
                            break;
                    }
                    break;
                case "section":
                    switch (functionName)
                    {
                        case "prefix":
                            SectionPrefix = rule_token;
                            break;
                        case "postfix":
                            SectionPostfix = rule_token;
                            break;
                        case "ignore":
                            SectionPrefix = SectionPostfix = null;
                            break;
                    }
                    break;
                case "paragraph":
                    switch (functionName)
                    {
                        case "prefix":
                            ParagraphPrefix = rule_token;
                            break;
                        case "postfix":
                            ParagraphPostfix = rule_token;
                            break;
                        case "ignore":
                            ParagraphPrefix = ParagraphPostfix = null;
                            break;
                    }
                    break;
                case "table":
                    switch (functionName)
                    {
                        case "prefix":
                            TablePrefix = rule_token;
                            break;
                        case "postfix":
                            TablePostfix = rule_token;
                            break;
                        case "ignore":
                            TablePrefix = TablePostfix = null;
                            break;
                        case "special": // Использование захардкоженного преобразования таблицы
                                        //TablePrefix = TablePostfix = null;
                            ColumnsPrefix = ColumnsPostfix = null;
                            ColumnPrefix = ColumnTemplate = ColumnPostfix = null;
                            RowPrefix = RowPostfix = null;
                            CellPrefix = CellPostfix = null;
                            // Аргументы: (style, paddings l-t-r-b, maxcellwidth, maxtablewidth)                            
                            UseSpecialTableBuilder = true;
                            SpecialTableBuilder = SpecialTableBuilderFactory.CreateSpecialTableBuilder(special);
                            if (SpecialTableBuilder == null) UseSpecialTableBuilder = false;
                            break;
                    }
                    break;
                case "columns":
                    switch (functionName)
                    {
                        case "prefix":
                            ColumnsPrefix = rule_token;
                            break;
                        case "postfix":
                            ColumnsPostfix = rule_token;
                            break;
                        case "ignore":
                            ColumnsPrefix = ColumnsPostfix = null;
                            break;
                    }
                    break;
                case "column":
                    switch (functionName)
                    {
                        case "prefix":
                            ColumnPrefix = rule_token;
                            break;
                        case "template":
                            ColumnTemplate = rule_token;
                            break;
                        case "postfix":
                            ColumnPostfix = rule_token;
                            break;
                        case "ignore":
                            ColumnPrefix = ColumnTemplate = ColumnPostfix = null;
                            break;

                    }
                    break;
                case "tablerow":
                    switch (functionName)
                    {
                        case "prefix":
                            RowPrefix = rule_token;
                            break;
                        case "postfix":
                            RowPostfix = rule_token;
                            break;
                        case "ignore":
                            RowPrefix = RowPostfix = null;
                            break;
                    }
                    break;
                case "tablecell":
                    switch (functionName)
                    {
                        case "prefix":
                            CellPrefix = rule_token;
                            break;
                        case "postfix":
                            CellPostfix = rule_token;
                            break;
                        case "ignore":
                            CellPrefix = CellPostfix = null;
                            break;
                    }
                    break;
                case "videoplayer":
                    switch (functionName)
                    {
                        case "prefix":
                            VideoplayerPrefix = rule_token;
                            break;
                        case "postfix":
                            VideoplayerPostfix = rule_token;
                            break;
                        case "ignore":
                            VideoplayerPrefix = VideoplayerPostfix = null;
                            break;
                    }
                    break;
                case "audioplayer":
                    switch (functionName)
                    {
                        case "prefix":
                            AudioplayerPrefix = rule_token;
                            break;
                        case "postfix":
                            AudioplayerPostfix = rule_token;
                            break;
                        case "ignore":
                            AudioplayerPrefix = AudioplayerPostfix = null;
                            break;
                    }
                    break;
                case "gallery":
                    switch (functionName)
                    {
                        case "prefix":
                            GalleryPrefix = rule_token;
                            break;
                        case "postfix":
                            GalleryPostfix = rule_token;
                            break;
                        case "ignore":
                            GalleryPrefix = GalleryPostfix = null;
                            break;
                    }
                    break;
            }
        }
    }
}
