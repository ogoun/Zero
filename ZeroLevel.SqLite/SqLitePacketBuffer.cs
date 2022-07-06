using SQLite;
using System;
using System.Threading;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.SqLite
{

    public sealed class PacketRecord
    {
        [PrimaryKey, AutoIncrement]
        public long Id { get; set; }
        [Indexed]
        public long Timestamp { get; set; }
        public byte[] Data { get; set; }
    }

    /// <summary>
    /// Промежуточное/временное хранилище пакетов данных, для случаев сбоя доставок через шину данных
    /// </summary>
    public sealed class SqLitePacketBuffer<T>
        : BaseSqLiteDB<PacketRecord>
        where T : IBinarySerializable
    {
        private sealed class PacketBufferRecord
        {
            public int Id { get; set; }
            public byte[] Body { get; set; }
        }

        #region Fields

        private readonly ReaderWriterLockSlim _rwLock = new ReaderWriterLockSlim();

        #endregion Fields

        public SqLitePacketBuffer(string database_file_path)
            : base(database_file_path)
        {
            CreateTable();
        }

        public void Push(T frame)
        {
            long id = -1;
            _rwLock.EnterWriteLock();
            var creationTime = DateTime.Now.Ticks;
            try
            {
                id = Append(new PacketRecord 
                {
                    Data = MessageSerializer.Serialize(frame),
                    Timestamp = creationTime
                }).Id;                
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLitePacketBuffer] Fault insert record in buffer storage.");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        public bool Pop(Func<T, bool> pop_callback)
        {
            bool success = false;
            long id = -1;
            _rwLock.EnterReadLock();
            try
            {
                var record = Single(r => r.Timestamp);
                id = record.Id;
                var body = record.Data;
                try
                {
                    success = pop_callback(MessageSerializer.Deserialize<T>(body));
                }
                catch (Exception ex)
                {
                    Log.Error(ex, "Fault handle buffered data");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "[SqLitePacketBuffer] Fault preload datafrom db");
            }
            finally
            {
                _rwLock.ExitReadLock();
            }
            if (success)
            {
                RemoveRecordById(id);
            }
            return success;
        }

        private void RemoveRecordById(long id)
        {
            _rwLock.EnterWriteLock();
            try
            {
                Delete(r => r.Id == id);
            }
            catch (Exception ex)
            {
                Log.Error(ex, $"[SqLitePacketBuffer] Fault remove record by id '{id}'");
            }
            finally
            {
                _rwLock.ExitWriteLock();
            }
        }

        protected override void DisposeStorageData()
        {
        }
    }
}
