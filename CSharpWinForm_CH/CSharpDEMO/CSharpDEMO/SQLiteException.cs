﻿using System;
using System.Runtime.Serialization;

namespace CSharpDEMO.WindowsForms_LeanCloud
{
    [Serializable]
    internal class SQLiteException : Exception
    {
        public SQLiteException()
        {
        }

        public SQLiteException(string message) : base(message)
        {
        }

        public SQLiteException(string message, Exception innerException) : base(message, innerException)
        {
        }

        protected SQLiteException(SerializationInfo info, StreamingContext context) : base(info, context)
        {
        }
    }
}