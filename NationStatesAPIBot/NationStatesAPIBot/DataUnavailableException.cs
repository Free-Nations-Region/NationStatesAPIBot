using System;
using System.Runtime.Serialization;

namespace NationStatesAPIBot
{
    [Serializable]
    internal class DataUnavailableException : Exception
    {
        public DataUnavailableException()
        {
        }

        public DataUnavailableException(string message) : base(message)
        {
        }

        public DataUnavailableException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected DataUnavailableException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}