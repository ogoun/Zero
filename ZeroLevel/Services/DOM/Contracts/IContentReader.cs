using ZeroLevel.DocumentObjectModel.Flow;

namespace ZeroLevel.DocumentObjectModel
{
    public interface IContentReader<T>
    {
        // Primitives
        void ReadText(Text text);
        void ReadQuote(Quote quote);
        void ReadLink(Link link, int order);
        void ReadImage(Image image, int order);
        void ReadAudio(Audio audio, int order);
        void ReadVideo(Video video, int order);

        // Containers
        void EnterSection(Section section);
        void LeaveSection(Section section);

        void EnterParagraph(Paragraph paragraph);
        void LeaveParagraph(Paragraph paragraph);

        void EnterList(List list);
        void EnterListItem(List list, IContentElement item, int order);
        void LeaveListItem(List list, IContentElement item, int order);
        void LeaveList(List list);

        void EnterTable(Table table);
        void EnterColumns(Table table);
        void ReadColumn(Table table, Column column, int order);
        void LeaveColumns(Table table);
        void EnterRow(Table table, Row row, int order);
        void EnterRowCell(Table table, Row row, IContentElement cell, int order);
        void LeaveRowCell(Table table, Row row, IContentElement cell, int order);
        void LeaveRow(Table table, Row row, int order);
        void LeaveTable(Table table);

        void EnterGallery(Gallery gallery);
        void LeaveGallery(Gallery gallery);

        void EnterAudioplayer(Audioplayer player);
        void LeaveAudioplayer(Audioplayer player);

        void EnterVideoplayer(Videoplayer player);
        void LeaveVideoplayer(Videoplayer player);

        // Feature
        void ReadForm(FormContent form);

        T Complete();
    }
}
