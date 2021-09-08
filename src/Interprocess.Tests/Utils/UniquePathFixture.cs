using System;
using System.Globalization;
using System.IO;
using Microsoft.VisualStudio.TestPlatform.ObjectModel.Engine.ClientProtocol;

namespace Cloudtoid.Interprocess.Tests
{
    public class UniquePathFixture : IDisposable
    {
        private static readonly string Root = System.IO.Path.GetTempPath();

        public UniquePathFixture()
        {
            while (true)
            {
                var folder = (DateTime.UtcNow.Ticks % 0xFFFFF).ToString("X5", CultureInfo.InvariantCulture);
                Path = System.IO.Path.Combine(Root, folder);
                if (!Directory.Exists(Path))
                {
                    Directory.CreateDirectory(Path);
                    break;
                }
            }
        }

        internal string Path { get; }

        public void Dispose()
        {
            try
            {
                Directory.Delete(Path, true);
            }
            catch
            {
                // ok
            }
        }
    }
}
