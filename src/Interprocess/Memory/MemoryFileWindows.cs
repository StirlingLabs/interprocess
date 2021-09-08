﻿using System.IO;
using System.IO.MemoryMappedFiles;

namespace Cloudtoid.Interprocess.Memory.Windows
{
    internal sealed class MemoryFileWindows : IMemoryFile
    {
        private const string MapNamePrefix = @"Global\CT_IP_";

        internal MemoryFileWindows(QueueOptions options)
        {
#if NET5_0
            if (!System.OperatingSystem.IsWindows())
                throw new System.PlatformNotSupportedException();
#endif
            MappedFile = MemoryMappedFile.CreateOrOpen(
                mapName: MapNamePrefix + options.QueueName,
                options.BytesCapacity,
                MemoryMappedFileAccess.ReadWrite,
                MemoryMappedFileOptions.None,
                HandleInheritability.None);
        }

        public MemoryMappedFile MappedFile { get; }

        public void Dispose()
            => MappedFile.Dispose();
    }
}
