namespace ZeroLevel.SqlServer
{
    public struct SqlDbObjectInfo
    {
        public string Name;
        public string Header;
        public string Text;

        public static bool operator ==(SqlDbObjectInfo first, SqlDbObjectInfo second)
        {
            return first.Equals(second);
        }

        public static bool operator !=(SqlDbObjectInfo first, SqlDbObjectInfo second)
        {
            return !first.Equals(second);
        }

        public bool Equals(SqlDbObjectInfo other)
        {
            bool eq = true;
            eq &= string.Compare(Name, other.Name, System.StringComparison.Ordinal) == 0;
            eq &= string.Compare(Header, other.Header, System.StringComparison.Ordinal) == 0;
            eq &= string.Compare(Text, other.Text, System.StringComparison.Ordinal) == 0;
            return eq;
        }

        public override bool Equals(object obj)
        {
            return Equals((SqlDbObjectInfo)obj);
        }

        public override int GetHashCode()
        {
            return Name.GetHashCode() ^ Header.GetHashCode() ^ Text.GetHashCode();
        }
    }
}
