using DOM.DSL.Model;
using ZeroLevel.DocumentObjectModel;
using ZeroLevel.DocumentObjectModel.Flow;

namespace DOM.Services
{
    /// <summary>
    /// Performs consecutive reading of content elements,
    /// or reading of metadata items
    /// </summary>
    public static class DocumentContentReader
    {
        private static void TraversElement<T>(IContentElement element,
            IContentReader<T> reader, DOMRenderElementCounter counter)
        {
            switch (element.Type)
            {
                // Primitives
                case ContentElementType.Text:
                    reader.ReadText((element as ZeroLevel.DocumentObjectModel.Flow.Text)!);
                    break;

                case ContentElementType.Quote:
                    reader.ReadQuote((element as Quote)!);
                    break;

                case ContentElementType.Link:
                    counter.IncLinkId();
                    reader.ReadLink((element as Link)!, counter.LinkId);
                    break;

                case ContentElementType.Image:
                    counter.IncImageId();
                    reader.ReadImage((element as Image)!, counter.ImageId);
                    break;

                case ContentElementType.Audio:
                    counter.IncAudioId();
                    reader.ReadAudio((element as Audio)!, counter.AudioId);
                    break;

                case ContentElementType.Video:
                    counter.IncVideoId();
                    reader.ReadVideo((element as Video)!, counter.VideoId);
                    break;

                case ContentElementType.Form:
                    reader.ReadForm((element as FormContent)!);
                    break;

                // Containers
                case ContentElementType.Content:
                    {
                        var content = (element as FlowContent);
                        if (content != null!)
                        {
                            for (int i = 0; i < content.Sections.Count; i++)
                            {
                                TraversElement(content.Sections[i], reader, counter);
                            }
                        }
                    }
                    break;

                case ContentElementType.Section:
                    var section = (element as Section);
                    if (section != null!)
                    {
                        reader.EnterSection(section);
                        for (int i = 0; i < section.Parts.Count; i++)
                        {
                            TraversElement(section.Parts[i], reader, counter);
                        }
                        reader.LeaveSection(section);
                    }
                    break;

                case ContentElementType.Paragraph:
                    var paragraph = (element as Paragraph);
                    if (paragraph != null!)
                    {
                        reader.EnterParagraph(paragraph);
                        for (int i = 0; i < paragraph.Parts.Count; i++)
                        {
                            TraversElement(paragraph.Parts[i], reader, counter);
                        }
                        reader.LeaveParagraph(paragraph);
                    }
                    break;

                case ContentElementType.List:
                    var list = (element as List);
                    if (list != null!)
                    {
                        reader.EnterList(list);
                        for (int i = 0; i < list.Items.Count; i++)
                        {
                            reader.EnterListItem(list, list.Items[i], i);
                            TraversElement(list.Items[i], reader, counter);
                            reader.LeaveListItem(list, list.Items[i], i);
                        }
                        reader.LeaveList(list);
                    }
                    break;

                case ContentElementType.Gallery:
                    var gallery = (element as Gallery);
                    if (gallery != null!)
                    {
                        reader.EnterGallery(gallery);
                        for (int i = 0; i < gallery.Images.Count; i++)
                        {
                            reader.ReadImage(gallery.Images[i], i);
                        }
                        reader.LeaveGallery(gallery);
                    }
                    break;

                case ContentElementType.Audioplayer:
                    var audioplayer = (element as Audioplayer);
                    if (audioplayer != null!)
                    {
                        reader.EnterAudioplayer(audioplayer);
                        for (int i = 0; i < audioplayer.Tracks.Count; i++)
                        {
                            reader.ReadAudio(audioplayer.Tracks[i], i);
                        }
                        reader.LeaveAudioplayer(audioplayer);
                    }
                    break;

                case ContentElementType.Videoplayer:
                    var videoplayer = (element as Videoplayer);
                    if (videoplayer != null!)
                    {
                        reader.EnterVideoplayer(videoplayer);
                        for (int i = 0; i < videoplayer.Playlist.Count; i++)
                        {
                            reader.ReadVideo(videoplayer.Playlist[i], i);
                        }
                        reader.LeaveVideoplayer(videoplayer);
                    }
                    break;

                case ContentElementType.Table:
                    var table = (element as Table);
                    if (table != null!)
                    {
                        reader.EnterTable(table);
                        reader.EnterColumns(table);
                        for (int i = 0; i < table.Columns.Count; i++)
                        {
                            reader.ReadColumn(table, table.Columns[i], i);
                        }
                        reader.LeaveColumns(table);
                        for (int ri = 0; ri < table.Rows.Count; ri++)
                        {
                            var row = table.Rows[ri];
                            reader.EnterRow(table, row, ri);
                            for (int i = 0; i < row.Cells.Count; i++)
                            {
                                reader.EnterRowCell(table, row, row.Cells[i], i);
                                TraversElement(row.Cells[i], reader, counter);
                                reader.LeaveRowCell(table, row, row.Cells[i], i);
                            }
                            reader.LeaveRow(table, row, ri);
                        }
                        reader.LeaveTable(table);
                    }
                    break;
            }
        }

        /// <summary>
        /// Reading metadata items
        /// </summary>
        public static T ReadMetadataAs<T>(Document doc, IMetadataReader<T> reader)
        {
            reader.ReadId(doc.Id);
            reader.ReadSummary(doc.Summary);
            reader.ReadHeader(doc.Header);

            reader.EnterIdentifier(doc.Identifier);
            reader.ReadVersion(doc.Identifier.Version);
            reader.ReadTimestamp(doc.Identifier.Timestamp);
            reader.ReadDateLabel(doc.Identifier.DateLabel);
            reader.LeaveIdentifier(doc.Identifier);

            reader.EnterTagsBlock(doc.TagMetadata);
            reader.EnterKeywords(doc.TagMetadata.Keywords);
            for (int i = 0; i < doc.TagMetadata.Keywords.Count; i++)
            {
                reader.ReadKeyword(doc.TagMetadata.Keywords[i], i);
            }
            reader.LeaveKeywords(doc.TagMetadata.Keywords);
            reader.EnterPlaces(doc.TagMetadata.Places);
            for (int i = 0; i < doc.TagMetadata.Places.Count; i++)
            {
                reader.ReadPlace(doc.TagMetadata.Places[i], i);
            }
            reader.LeavePlaces(doc.TagMetadata.Places);
            reader.EnterCompanies(doc.TagMetadata.Companies);
            for (int i = 0; i < doc.TagMetadata.Companies.Count; i++)
            {
                reader.ReadCompany(doc.TagMetadata.Companies[i], i);
            }
            reader.LeaveCompanies(doc.TagMetadata.Companies);
            reader.EnterPersons(doc.TagMetadata.Persons);
            for (int i = 0; i < doc.TagMetadata.Persons.Count; i++)
            {
                reader.ReadPerson(doc.TagMetadata.Persons[i], i);
            }
            reader.LeavePersons(doc.TagMetadata.Persons);
            reader.LeaveTagsBlock(doc.TagMetadata);

            reader.EnterDescriptioveBlock(doc.DescriptiveMetadata);
            reader.ReadAuthors(doc.DescriptiveMetadata.Byline);
            reader.ReadCopiright(doc.DescriptiveMetadata.CopyrightNotice);
            reader.ReadCreated(doc.DescriptiveMetadata.Created);
            reader.ReadLanguage(doc.DescriptiveMetadata.Language);
            reader.ReadPriority(doc.DescriptiveMetadata.Priority);
            reader.ReadSource(doc.DescriptiveMetadata.Source);
            reader.ReadPublisher(doc.DescriptiveMetadata.Publisher);
            reader.ReadOriginal(doc.DescriptiveMetadata.Original);
            reader.EnterHeaders(doc.DescriptiveMetadata.Headers);
            for (int i = 0; i < doc.DescriptiveMetadata.Headers.Count; i++)
            {
                reader.ReadHeader(doc.DescriptiveMetadata.Headers[i], i);
            }
            reader.LeaveHeaders(doc.DescriptiveMetadata.Headers);
            reader.LeaveDescriptioveBlock(doc.DescriptiveMetadata);

            reader.EnterAsides(doc.Attachments);
            for (int i = 0; i < doc.Attachments.Count; i++)
            {
                reader.ReadAside(doc.Attachments[i], i);
            }
            reader.LeaveAsides(doc.Attachments);

            reader.EnterAssotiations(doc.Assotiations);
            for (int i = 0; i < doc.Assotiations.Count; i++)
            {
                reader.ReadAssotiation(doc.Assotiations[i], i);
            }
            reader.LeaveAssotiations(doc.Assotiations);

            reader.EnterCategories(doc.Categories);
            for (int i = 0; i < doc.Categories.Count; i++)
            {
                reader.ReadCategory(doc.Categories[i], i);
            }
            reader.LeaveCategories(doc.Categories);
            return reader.Complete();
        }

        /// <summary>
        /// Reading content elements
        /// </summary>
        public static T ReadAs<T>(Document doc, IContentReader<T> reader)
        {
            DOMRenderElementCounter counter = new DOMRenderElementCounter();
            foreach (var section in doc.Content.Sections)
            {
                TraversElement(section, reader, counter);
            }
            return reader.Complete();
        }
    }
}