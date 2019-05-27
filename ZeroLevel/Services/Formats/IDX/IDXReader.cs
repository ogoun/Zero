using System;
using System.Collections.Generic;
using System.IO;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Services.Formats.IDX
{
    /*
   The basic format is

   magic number 
   size in dimension 0 
   size in dimension 1 
   size in dimension 2 
   ..... 
   size in dimension N 
   data

   The magic number is an integer (MSB first). The first 2 bytes are always 0.

   The third byte codes the type of the data: 
   0x08: unsigned byte 
   0x09: signed byte 
   0x0B: short (2 bytes) 
   0x0C: int (4 bytes) 
   0x0D: float (4 bytes) 
   0x0E: double (8 bytes)

   The 4-th byte codes the number of dimensions of the vector/matrix: 1 for vectors, 2 for matrices....
   The sizes in each dimension are 4-byte integers (MSB first, high endian, like in most non-Intel processors).
   The data is stored like in a C array, i.e. the index in the last dimension changes the fastest. 

   */
    public class IDXReader
        : IDisposable
    {
        public int DimensionsCount { get; private set; }
        public int[] DimentionMeasures { get; private set; }
        public IDXDataType DataType { get; private set; }
        private IDXIndex _index;
        private readonly MemoryStreamReader _reader;

        public int[] CurrentIndex => _index.Cursor;

        public IDXReader(string filePath)
        {
            _reader = new MemoryStreamReader(new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite));
            _reader.ReverseByteOrder(true);
            // Header
            // skip zero bytes
            _reader.ReadByte();
            _reader.ReadByte();
            // read data type
            switch (_reader.ReadByte())
            {
                case 0x08:
                    DataType = IDXDataType.UNSIGNED_BYTE;
                    break;
                case 0x09:
                    DataType = IDXDataType.SIGNED_BYTE;
                    break;
                case 0x0B:
                    DataType = IDXDataType.SHORT;
                    break;
                case 0x0C:
                    DataType = IDXDataType.INT;
                    break;
                case 0x0D:
                    DataType = IDXDataType.FLOAT;
                    break;
                case 0x0E:
                    DataType = IDXDataType.DOUBLE;
                    break;
            }
            // read dimensions count
            DimensionsCount = _reader.ReadByte();
            DimentionMeasures = new int[DimensionsCount];
            for (int i = 0; i < DimensionsCount; i++)
            {
                DimentionMeasures[i] = _reader.ReadInt32();
            }
            _index = new IDXIndex(DimentionMeasures);
        }

        public IEnumerable<Byte> ReadUnsignedBytes()
        {
            if (DataType != IDXDataType.UNSIGNED_BYTE)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return _reader.ReadByte();
            }
        }

        public IEnumerable<SByte> ReadSignedBytes()
        {
            if (DataType != IDXDataType.SIGNED_BYTE)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return unchecked((sbyte)_reader.ReadByte());
            }
        }

        public IEnumerable<short> ReadShorts()
        {
            if (DataType != IDXDataType.SHORT)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return BitConverter.ToInt16(_reader.ReadBuffer(2), 0);
            }
        }

        public IEnumerable<Int32> ReadInts()
        {
            if (DataType != IDXDataType.INT)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return _reader.ReadInt32();
            }
        }

        public IEnumerable<float> ReadFloats()
        {
            if (DataType != IDXDataType.FLOAT)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return _reader.ReadFloat();
            }
        }

        public IEnumerable<double> ReadDoubles()
        {
            if (DataType != IDXDataType.DOUBLE)
                throw new InvalidOperationException($"Wrong data type read. File datatype: {DataType}");
            while (_index.MoveNext())
            {
                yield return _reader.ReadDouble();
            }
        }

        public void Dispose()
        {
            _reader.Dispose();
        }
    }
}
