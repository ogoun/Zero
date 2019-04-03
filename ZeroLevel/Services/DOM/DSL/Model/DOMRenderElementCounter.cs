namespace DOM.DSL.Model
{
    internal sealed class DOMRenderElementCounter
    {
        public int SectionId { get; private set; } = -1;
        public int ParagraphId { get; private set; } = -1;
        public int ListId { get; private set; } = -1;
        public int ListItemId { get; private set; } = -1;
        public int TableId { get; private set; } = -1;
        public int ColumnId { get; private set; } = -1;
        public int RowId { get; private set; } = -1;
        public int CellId { get; private set; } = -1;

        public int FormId { get; private set; } = -1;
        public int LinkId { get; private set; } = -1;
        public int QuoteId { get; private set; } = -1;
        public int TextId { get; private set; } = -1;

        public int AudioplayerId { get; private set; } = -1;
        public int AudioId { get; private set; } = -1;

        public int VideoplayerId { get; private set; } = -1;
        public int VideoId { get; private set; } = -1;

        public int GalleryId { get; private set; } = -1;
        public int ImageId { get; private set; } = -1;

        public void IncSectionId()
        {
            SectionId++;
        }

        public void IncParagraphId()
        {
            ParagraphId++;
        }

        public void IncListId()
        {
            ListId++;
        }

        public void IncListItemId()
        {
            ListItemId++;
        }

        public void IncTableId()
        {
            TableId++;
        }

        public void IncColumnId()
        {
            ColumnId++;
        }

        public void IncRowId()
        {
            RowId++;
        }

        public void IncCellId()
        {
            CellId++;
        }

        public void IncFormId()
        {
            FormId++;
        }

        public void IncLinkId()
        {
            LinkId++;
        }

        public void IncQuoteId()
        {
            QuoteId++;
        }

        public void IncTextId()
        {
            TextId++;
        }

        public void IncAudioplayerId()
        {
            AudioplayerId++;
        }

        public void IncAudioId()
        {
            AudioId++;
        }

        public void IncVideoplayerId()
        {
            VideoplayerId++;
        }

        public void IncVideoId()
        {
            VideoId++;
        }

        public void IncGalleryId()
        {
            GalleryId++;
        }

        public void IncImageId()
        {
            ImageId++;
        }
    }
}