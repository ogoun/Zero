using System;
using System.IO;
using System.Reflection;

namespace LemmaSharp
{
    [Serializable]
    public class LemmatizerPrebuiltCompact : LemmatizerPrebuilt
    {
        public const string FILEMASK = "compact7z-{0}.lem";

        #region Constructor(s) & Destructor(s)
        public LemmatizerPrebuiltCompact(LanguagePrebuilt lang)
            : base(lang)
        {
            Stream stream = GetResourceStream(GetResourceFileName(FILEMASK));
            this.Deserialize(stream);
            stream.Close();
        }
        #endregion

        #region Resource Management Functions
        protected override Assembly GetExecutingAssembly()
        {
            return Assembly.GetExecutingAssembly();
        }
        #endregion
    }
}
