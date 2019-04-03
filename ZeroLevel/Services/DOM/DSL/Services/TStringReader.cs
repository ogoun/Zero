namespace DOM.DSL.Services
{
    public class TStringReader
    {
        private int _position;
        private readonly string _template;

        public bool EOF => _position >= _template?.Length;
        public bool StartPosition => _position == 0;
        public bool LastPosition => _position == _template?.Length - 1;
        public char Current => EOF ? char.MinValue : _template[_position];
        public char Next => EOF || LastPosition ? char.MinValue : _template[_position + 1];
        public char Preview => StartPosition ? char.MinValue : _template[_position - 1];

        public TStringReader(string template)
        {
            _template = template;
            _position = 0;
        }

        public bool Move(int offset = 1)
        {
            if (EOF) return false;
            if (LastPosition) { _position = _template.Length; return false; }
            _position += offset;
            if (_position >= _template.Length)
            {
                _position = _template.Length;
            }
            return true;
        }

        public int SkipSpaces()
        {
            int count = 0;
            while (EOF == false && char.IsWhiteSpace(Current)) { Move(); count++; }
            return count;
        }

        public void SkipBreaks()
        {
            while (EOF == false && char.IsWhiteSpace(Current)) Move();
        }

        public bool MoveBack()
        {
            _position = _position - 1;
            if (_position < 0)
            {
                _position = 0;
                return false;
            }
            return true;
        }

        public int FindOffsetTo(char symbol)
        {
            if (_position == -1 || EOF || LastPosition) return -1;
            var search_position = _position;
            var sym = _template[search_position];
            while (search_position < _template.Length && false == sym.Equals(symbol))
            {
                search_position++;
                sym = _template[search_position];
            }
            return sym.Equals(symbol) ? search_position - _position : -1;
        }

        public bool Test(char sym, int offset = 0)
        {
            var index = _position + offset;
            if (index < 0 || index >= _template.Length) return false;
            return _template[index].Equals(sym);
        }

        public string ReadIdentity()
        {
            string identity = string.Empty;
            var offset = _position;
            if (offset < _template.Length && char.IsLetter(_template[offset]))
            {
                var index = offset + 1;
                while (index < _template.Length && (char.IsLetterOrDigit(_template[index]) || _template[index] == '_' || _template[index] == '-'))
                    index++;
                identity = _template.Substring(offset, index - offset);
            }
            return identity.ToLowerInvariant();
        }

        public string ReadWord()
        {
            string identity = string.Empty;
            var offset = _position;
            if (offset < _template.Length && char.IsLetterOrDigit(_template[offset]))
            {
                var index = offset + 1;
                while (index < _template.Length && char.IsLetterOrDigit(_template[index]))
                    index++;
                identity = _template.Substring(offset, index - offset);
            }
            return identity;
        }
    }
}