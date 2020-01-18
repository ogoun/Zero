using System;
using System.Text;

namespace TFIDFbee
{
    public class RecordParser
    {
        private enum RPState
        {
            WaitKey,
            ParseKey,
            WaitKeyConfirm,
            WaitValue,
            ParseValue
        }
        private readonly StringBuilder _builder = new StringBuilder();
        private RPState State = RPState.WaitKey;
        private char _previous = '\0';
        private string _key;
        private string _value;
        private readonly Action<string, string> _callback;

        public RecordParser(Action<string, string> callback)
        {
            _callback = callback;
        }

        public void Append(string text)
        {
            foreach (var ch in text)
            {
                switch (State)
                {
                    case RPState.WaitKey:
                        if (ch.Equals('"'))
                        {
                            State = RPState.ParseKey;
                            _builder.Clear();
                        }
                        break;
                    case RPState.ParseKey:
                        if (ch.Equals('"') && _previous != '\\')
                        {
                            if (_builder.Length > 0)
                            {
                                State = RPState.WaitKeyConfirm;
                            }
                            else
                            {
                                State = RPState.WaitKey;
                            }
                        }
                        else
                        {
                            _builder.Append(ch);
                        }
                        break;
                    case RPState.WaitKeyConfirm:
                        if (ch.Equals(':'))
                        {
                            _key = _builder.ToString();
                            State = RPState.WaitValue;
                        }
                        else if (ch == ' ' || ch == '\r' || ch == '\n')
                        {
                            // nothing
                        }
                        else
                        {
                            State = RPState.WaitKey;
                        }
                        break;
                    case RPState.WaitValue:
                        if (ch.Equals('"'))
                        {
                            State = RPState.ParseValue;
                            _builder.Clear();
                        }
                        else if (ch == ' ' || ch == '\r' || ch == '\n')
                        {
                            // nothing
                        }
                        else
                        {
                            State = RPState.WaitKey;
                        }
                        break;
                    case RPState.ParseValue:
                        if (ch.Equals('"') && _previous != '\\')
                        {
                            if (_builder.Length > 0)
                            {
                                _value = _builder.ToString();
                                _callback(_key, _value);
                            }
                            State = RPState.WaitKey;
                        }
                        else
                        {
                            _builder.Append(ch);
                        }
                        break;
                }
                _previous = ch;
            }
        }
    }
}
