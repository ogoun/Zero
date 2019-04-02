using System;
using System.Runtime.Serialization;
using ZeroLevel.Services.Serialization;

namespace ZeroLevel.Models
{
    /// <summary>
    /// Action result
    /// </summary>
    [DataContract]
    public class InvokeResult :
        IBinarySerializable
    {
        #region Static
        private static readonly InvokeResult _successResultWitoutComment = new InvokeResult(true, String.Empty);
        #endregion

        #region Ctor
        public InvokeResult()
        {
        }

        public InvokeResult(bool success, string comment)
        {
            Success = success;
            Comment = comment;
        }
        #endregion

        #region Properties
        /// <summary>
        /// true when action successfully invoked
        /// </summary>
        [DataMember]
        public bool Success;
        /// <summary>
        /// Comment
        /// </summary>
        [DataMember]
        public string Comment;
        #endregion

        #region Fabric methods
        /// <summary>
        /// Error when action invoking
        /// </summary>
        public static InvokeResult Fault(string comment) { return new InvokeResult(false, comment); }
        /// <summary>
        /// Successfully
        /// </summary>        
        public static InvokeResult Succeeding(string comment = "") { return new InvokeResult(true, comment); }
        /// <summary>
        /// Successfully
        /// </summary>
        public static InvokeResult Succeeding() { return _successResultWitoutComment; }
        #endregion

        public virtual void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(this.Success);
            writer.WriteString(this.Comment);
        }

        public virtual void Deserialize(IBinaryReader reader)
        {
            this.Success = reader.ReadBoolean();
            this.Comment = reader.ReadString();
        }
    }

    public sealed class InvokeResult<T> :
        InvokeResult
    {
        private T _value;
        public T Value { get { return _value; } }

        #region Ctor
        public InvokeResult(bool success, string comment)
        {
            Success = success;
            Comment = comment;
        }

        public InvokeResult(T value, bool success, string comment)
        {
            _value = value;
            Success = success;
            Comment = comment;
        }
        #endregion

        #region Fabric methods
        public static InvokeResult<T> Succeeding(T value, string comment = "") { return new InvokeResult<T>(value, true, comment); }
        public static InvokeResult<T> Fault(string comment) { return new InvokeResult<T>(false, comment); }
        #endregion

        public override void Serialize(IBinaryWriter writer)
        {
            writer.WriteBoolean(this.Success);
            writer.WriteString(this.Comment);
            writer.WriteCompatible(this.Value);
        }

        public override void Deserialize(IBinaryReader reader)
        {
            this.Success = reader.ReadBoolean();
            this.Comment = reader.ReadString();
            this._value = reader.ReadCompatible<T>();
        }
    }
}
