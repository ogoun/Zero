using System;

namespace ZeroInvokingTest.Models
{
    public class FakeClass : BaseFakeClass
    {
        public string GetString(string line) => line;
        internal int GetNumber(int number) => number;
        private DateTime GetDateTime(DateTime date) => date;
    }
}
