using System;

using SysPath = System.IO.Path;

namespace Cloudtoid.Interprocess
{
    public sealed class QueueOptions
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="QueueOptions"/> class.
        /// </summary>
        /// <param name="queueName">The unique name of the queue.</param>
        /// <param name="bytesCapacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
        public QueueOptions(string queueName, long bytesCapacity)
            : this(queueName, SysPath.GetTempPath(), bytesCapacity)
        {
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="QueueOptions"/> class.
        /// </summary>
        /// <param name="queueName">The unique name of the queue.</param>
        /// <param name="path">The path to the directory/folder in which the memory mapped and other files are stored in</param>
        /// <param name="bytesCapacity">The maximum capacity of the queue in bytes. This should be at least 16 bytes long and in the multiples of 8</param>
        public unsafe QueueOptions(string queueName, string path, long bytesCapacity)
        {
            if (string.IsNullOrEmpty(queueName)) throw new ArgumentNullException(nameof(queueName));
            if (path is null) throw new ArgumentNullException(nameof(path));
            if (bytesCapacity <= sizeof(QueueHeader)) throw new ArgumentOutOfRangeException(nameof(bytesCapacity), "Should be greater than {sizeof(QueueHeader)}.");
            if ((bytesCapacity % 8) != 0) throw new ArgumentOutOfRangeException(nameof(bytesCapacity), $"{nameof(bytesCapacity)} should be a multiple of 8 (8 bytes = 64 bits).");
            QueueName = queueName;
            Path = path;
            BytesCapacity = bytesCapacity;
        }

        /// <summary>
        /// Gets the unique name of the queue.
        /// </summary>
        public string QueueName { get; }

        /// <summary>
        /// Gets the path to the directory/folder in which the memory mapped and other files are stored in.
        /// </summary>
        public string Path { get; }

        /// <summary>
        /// Gets the maximum capacity of the queue in bytes.
        /// </summary>
        public long BytesCapacity { get; }
    }
}
