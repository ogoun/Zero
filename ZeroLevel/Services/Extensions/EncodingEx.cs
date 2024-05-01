using System;
using System.Text;

namespace ZeroLevel.Services.Extensions
{
    public class EncodingEx : Encoding
    {
        private readonly Encoding _baseEncoding;

        public override string BodyName
        {
            get
            {
                if (_baseEncoding.CodePage == 1251)
                    return _baseEncoding.HeaderName;
                return _baseEncoding.BodyName;
            }
        }

        public EncodingEx(string name)
            : this(Encoding.GetEncoding(name))
        {

        }

        public EncodingEx(Encoding baseEncoding) : base(baseEncoding.CodePage)
        {
            if (baseEncoding == null!) throw new ArgumentNullException("baseEncoding");
            _baseEncoding = baseEncoding;
        }

        public override int GetByteCount(char[] chars, int index, int count)
        {
            return _baseEncoding.GetByteCount(chars, index, count);
        }

        public override int GetBytes(char[] chars, int charIndex, int charCount, byte[] bytes, int byteIndex)
        {
            return _baseEncoding.GetBytes(chars, charIndex, charCount, bytes, byteIndex);
        }

        public override int GetCharCount(byte[] bytes, int index, int count)
        {
            return _baseEncoding.GetCharCount(bytes, index, count);
        }

        public override int GetChars(byte[] bytes, int byteIndex, int byteCount, char[] chars, int charIndex)
        {
            return _baseEncoding.GetChars(bytes, byteIndex, byteCount, chars, charIndex);
        }

        public override int GetMaxByteCount(int charCount)
        {
            return _baseEncoding.GetMaxByteCount(charCount);
        }

        public override int GetMaxCharCount(int byteCount)
        {
            return _baseEncoding.GetMaxCharCount(byteCount);
        }
    }
}
