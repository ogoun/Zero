using System;
using System.Reflection;

namespace LemmaSharp
{
    [Serializable]
    public class LemmatizerPrebuiltFull : LemmatizerPrebuilt
    {
        public const string FILEMASK = "full7z-{0}.lem";

        #region Constructor(s)
        public LemmatizerPrebuiltFull(LanguagePrebuilt lang)
            : base(lang)
        {
            using (var stream = GetResourceStream(GetResourceFileName(FILEMASK)))
            {
                this.Deserialize(stream);
                stream.Close();
            }
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
