using System;
using System.Collections.Generic;

namespace ZeroLevel.DocumentObjectModel
{
    public interface IMetadataReader<T>
    {
        void ReadId(Guid Id);
        void ReadSummary(string summary);
        void ReadHeader(string header);

        void EnterIdentifier(Identifier identifier);
        void ReadVersion(int version);
        void ReadTimestamp(string timestamp);
        void ReadDateLabel(string datelabel);
        void LeaveIdentifier(Identifier identifier);

        void EnterTagsBlock(TagMetadata tagBlock);
        void EnterKeywords(IEnumerable<string> keywords);
        void ReadKeyword(string keyword, int order);
        void LeaveKeywords(IEnumerable<string> keywords);
        void EnterPlaces(IEnumerable<Tag> places);
        void ReadPlace(Tag place, int order);
        void LeavePlaces(IEnumerable<Tag> places);
        void EnterCompanies(IEnumerable<Tag> companies);
        void ReadCompany(Tag company, int order);
        void LeaveCompanies(IEnumerable<Tag> companies);
        void EnterPersons(IEnumerable<Tag> persons);
        void ReadPerson(Tag person, int order);
        void LeavePersons(IEnumerable<Tag> persons);
        void LeaveTagsBlock(TagMetadata tagBlock);

        void EnterDescriptioveBlock(DescriptiveMetadata metadata);
        void ReadAuthors(string byline);
        void ReadCopiright(string copyright);
        void ReadCreated(DateTime created);
        void ReadLanguage(string language);
        void ReadPriority(Priority priority);
        void ReadSource(Agency source);
        void ReadPublisher(Agency publisher);
        void ReadOriginal(Tag original);
        void EnterHeaders(IEnumerable<Header> headers);
        void ReadHeader(Header header, int order);
        void LeaveHeaders(IEnumerable<Header> headers);
        void LeaveDescriptioveBlock(DescriptiveMetadata metadata);

        void EnterAsides(IEnumerable<AsideContent> asides);
        void ReadAside(AsideContent aside, int order);
        void LeaveAsides(IEnumerable<AsideContent> asides);

        void EnterAssotiations(IEnumerable<Assotiation> assotiations);
        void ReadAssotiation(Assotiation assotiation, int order);
        void LeaveAssotiations(IEnumerable<Assotiation> assotiations);

        void EnterCategories(IEnumerable<Category> categories);
        void ReadCategory(Category category, int order);
        void LeaveCategories(IEnumerable<Category> categories);

        T Complete();
    }
}
