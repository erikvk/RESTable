﻿using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace RESTable.Resources
{
    public class BinaryResult
    {
        public Func<Stream, CancellationToken, Task> WriteToStream { get; }
        public ContentType ContentType { get; }

        public BinaryResult(Func<Stream, CancellationToken, Task> writeToStream, ContentType contentType)
        {
            WriteToStream = writeToStream;
            ContentType = contentType;
        }
    }
}