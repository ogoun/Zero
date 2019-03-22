﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Text;

namespace ZeroLevel.Services.Serialization
{
    /// <summary>
    /// Обертка над MemoryStream для чтения, с проверкой выхода за пределы потока
    /// </summary>
    public sealed class MemoryStreamReader
        : IBinaryReader
    {
        private readonly MemoryStream _stream;
        /// <summary>
        /// Конструктор
        /// </summary>
        /// <param name="data">Данные для чтения</param>
        public MemoryStreamReader(byte[] data)
        {
            if (data == null)
                throw new ArgumentNullException(nameof(data));
            _stream = new MemoryStream(data);
        }

        /// <summary>
        /// Чтение флага
        /// </summary>
        public bool ReadBoolean()
        {
            if (CheckOutOfRange(_stream, 1))
                throw new OutOfMemoryException("Array index out of bounds");
            return BitConverter.ToBoolean(new byte[1] { ReadByte() }, 0);
        }
        /// <summary>
        /// Чтение байта
        /// </summary>
        public byte ReadByte()
        {
            if (CheckOutOfRange(_stream, 1))
                throw new OutOfMemoryException("Array index out of bounds");
            return (byte)_stream.ReadByte();
        }
        /// <summary>
        /// Чтение байт-массива
        /// </summary>
        /// <returns></returns>
        public byte[] ReadBytes()
        {
            var length = BitConverter.ToInt32(ReadBuffer(4), 0);
            if (length == 0) return new byte[0];
            return ReadBuffer(length);
        }
        /// <summary>
        /// Чтение целого 32-хбитного числа (4 байта)
        /// </summary>
        public Int32 ReadInt32()
        {
            var buffer = ReadBuffer(4);
            return BitConverter.ToInt32(buffer, 0);
        }

        public decimal ReadDecimal()
        {
            var buffer = ReadBuffer(4);
            return BitConverter.ToInt32(buffer, 0);
        }
        /// <summary>
        /// Чтение целого 64-хбитного числа (8 байт)
        /// </summary>
        public Int64 ReadLong()
        {
            var buffer = ReadBuffer(8);
            return BitConverter.ToInt64(buffer, 0);
        }

        public TimeSpan ReadTimeSpan()
        {
            return new TimeSpan(ReadLong());
        }

        public double ReadDouble()
        {
            var buffer = ReadBuffer(8);
            return BitConverter.ToDouble(buffer, 0);
        }

        /// <summary>
        /// Чтение строки   (4 байта на длину + Length байт)
        /// </summary>
        public string ReadString()
        {
            var length = BitConverter.ToInt32(ReadBuffer(4), 0);
            if (length == 0) return null;
            var buffer = ReadBuffer(length);
            return Encoding.UTF8.GetString(buffer);
        }
        /// <summary>
        /// Чтение GUID (16 байт)
        /// </summary>
        public Guid ReadGuid()
        {
            var buffer = ReadBuffer(16);
            return new Guid(buffer);
        }
        /// <summary>
        ///  Чтение байт-пакета (читается размер из указанного количества байт и затем сам пакет прочитанного размера)
        /// </summary>
        public byte[] ReadBuffer(int count)
        {
            if (count == 0) return null;
            if (CheckOutOfRange(_stream, count))
                throw new OutOfMemoryException("Array index out of bounds");
            var buffer = new byte[count];
            var readedCount = _stream.Read(buffer, 0, count);
            if (count != readedCount)
                throw new InvalidOperationException(string.Format("The stream returned less data ({0} bytes) than expected ({1} bytes)", count, readedCount));
            return buffer;
        }
        /// <summary>
        /// Чтение даты времени
        /// </summary>
        /// <returns></returns>
        public DateTime? ReadDateTime()
        {
            var is_null = ReadByte();
            if (is_null == 0) return null;
            var buffer = ReadBuffer(8);
            long deserialized = BitConverter.ToInt64(buffer, 0);
            return DateTime.FromBinary(deserialized);
        }

        public IPAddress ReadIP()
        {
            var addr = ReadLong();
            return new IPAddress(addr);
        }

        public IPEndPoint ReadIPEndpoint()
        {
            var addr = ReadLong();
            var port = ReadInt32();
            return new IPEndPoint(addr, port);
        }

        /// <summary>
        /// Проверка не выходит ли чтение данных за пределы потока
        /// </summary>
        bool CheckOutOfRange(Stream stream, int offset)
        {
            return (stream.Position + offset) > stream.Length;
        }

        #region Extensions
        public List<T> ReadCollection<T>()
            where T : IBinarySerializable, new()
        {
            int count = ReadInt32();
            var collection = new List<T>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    var item = new T();
                    item.Deserialize(this);
                    collection.Add(item);
                }
            }
            return collection;
        }

        public T ReadCompatible<T>()
        {
            return MessageSerializer.DeserializeCompatible<T>(this);
        }

        public T Read<T>() where T : IBinarySerializable
        {
            byte type = ReadByte();
            if (type == 0) return default(T);
            var item = (T)Activator.CreateInstance<T>();
            item.Deserialize(this);
            return item;
        }

        public List<string> ReadStringCollection()
        {
            int count = ReadInt32();
            var collection = new List<string>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadString());
                }
            }
            return collection;
        }

        public List<IPAddress> ReadIPCollection()
        {
            int count = ReadInt32();
            var collection = new List<IPAddress>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadIP());
                }
            }
            return collection;
        }

        public List<IPEndPoint> ReadIPEndPointCollection()
        {
            int count = ReadInt32();
            var collection = new List<IPEndPoint>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadIPEndpoint());
                }
            }
            return collection;
        }

        public List<Guid> ReadGuidCollection()
        {
            int count = ReadInt32();
            var collection = new List<Guid>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadGuid());
                }
            }
            return collection;
        }

        public List<DateTime> ReadDateTimeCollection()
        {
            int count = ReadInt32();
            var collection = new List<DateTime>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadDateTime() ?? DateTime.MinValue);
                }
            }
            return collection;
        }
        public List<Int64> ReadInt64Collection()
        {
            int count = ReadInt32();
            var collection = new List<Int64>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadLong());
                }
            }
            return collection;
        }
        public List<Int32> ReadInt32Collection()
        {
            int count = ReadInt32();
            var collection = new List<Int32>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadInt32());
                }
            }
            return collection;
        }
        public List<Double> ReadDoubleCollection()
        {
            int count = ReadInt32();
            var collection = new List<Double>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadDouble());
                }
            }
            return collection;
        }
        public List<bool> ReadBooleanCollection()
        {
            int count = ReadInt32();
            var collection = new List<bool>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadBoolean());
                }
            }
            return collection;
        }
        public List<byte> ReadByteCollection()
        {
            int count = ReadInt32();
            var collection = new List<byte>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadByte());
                }
            }
            return collection;
        }
        public List<byte[]> ReadByteArrayCollection()
        {
            int count = ReadInt32();
            var collection = new List<byte[]>(count);
            if (count > 0)
            {
                for (int i = 0; i < count; i++)
                {
                    collection.Add(ReadBytes());
                }
            }
            return collection;
        }
        #endregion

        /// <summary>
        /// Очистка
        /// </summary>
        public void Dispose()
        {
            _stream.Dispose();
        }
    }
}
