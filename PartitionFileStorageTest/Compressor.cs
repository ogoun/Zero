namespace PartitionFileStorageTest
{
    public static class Compressor
    {
        /// <summary>
        /// Упаковка набора чисел в массив байтов
        /// </summary>
        public static byte[] GetEncodedBytes(IEnumerable<ulong> list, ref ulong last)
        {
            byte[] segmentsBytes;
            using (var memoryStream = new MemoryStream())
            {
                foreach (var current in list)
                {
                    var value = current - last;
                    memoryStream.Write7BitEncodedULong(value);
                    last = current;
                }
                segmentsBytes = memoryStream.ToArray();
            }
            return segmentsBytes;
        }

        public static IEnumerable<ulong> DecodeBytesContent(byte[] bytes)
        {
            ulong last = 0;
            using (var memoryStream = new MemoryStream(bytes))
            {
                while (memoryStream.Position != memoryStream.Length)
                {
                    var value = memoryStream.Read7BitEncodedULong();
                    var current = last + value;

                    yield return current;

                    last = current;
                }
            }
        }


        public static void Write7BitEncodedULong(this MemoryStream writer, ulong value)
        {
            var first = true;
            while (first || value > 0)
            {
                first = false;
                var lower7bits = (byte)(value & 0x7f);
                value >>= 7;
                if (value > 0)
                    lower7bits |= 128;
                writer.WriteByte(lower7bits);
            }
        }

        public static ulong Read7BitEncodedULong(this MemoryStream reader)
        {
            if (reader == null)
                throw new ArgumentNullException(nameof(reader));

            var more = true;
            ulong value = 0;
            var shift = 0;
            while (more)
            {
                ulong lower7bits = (byte)reader.ReadByte();
                more = (lower7bits & 128) != 0;
                value |= (lower7bits & 0x7f) << shift;
                shift += 7;
            }
            return value;
        }
    }
}
