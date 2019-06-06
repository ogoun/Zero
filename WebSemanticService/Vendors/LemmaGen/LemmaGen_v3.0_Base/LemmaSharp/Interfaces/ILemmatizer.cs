using System.Runtime.Serialization;

namespace LemmaSharp
{
    public interface ILemmatizer : ISerializable
    {
        string Lemmatize(string sWord);
    }
}
