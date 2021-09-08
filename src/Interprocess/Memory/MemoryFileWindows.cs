using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.IO.MemoryMappedFiles;
using System.Runtime.InteropServices;

namespace Cloudtoid.Interprocess.Memory.Windows
{
    [SuppressMessage("Interoperability", "CA1416", Justification = "Used only on Windows platforms")]
    internal sealed class MemoryFileWindows : IMemoryFile
    {
        internal MemoryFileWindows(QueueOptions options)
        {
            if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
                throw new PlatformNotSupportedException();

            var name = options.QueueName + "_File";

            try
            {
                MappedFile = MemoryMappedFile.OpenExisting(name);
            }
            catch (FileNotFoundException)
            {
                MappedFile = MemoryMappedFile.CreateNew(
                    name,
                    options.BytesCapacity,
                    MemoryMappedFileAccess.ReadWrite,
                    MemoryMappedFileOptions.None,
                    HandleInheritability.None);
            }
        }

        public MemoryMappedFile MappedFile { get; }

        public void Dispose()
            => MappedFile.Dispose();
    }
}
