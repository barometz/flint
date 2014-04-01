using System.IO;
using System.Linq;
using Ionic.Zip;

namespace Flint.Core.Tests.Dependencies
{
    public class ZipImplementation : IZip
    {
        private ZipFile _zipFile;

        public void Dispose()
        {
            _zipFile.Dispose();
            _zipFile = null;
        }

        public bool Open(Stream zipStream)
        {
            _zipFile = ZipFile.Read(zipStream);
            return true;
        }

        public Stream OpenEntryStream(string zipEntryName)
        {
            ZipEntry entry = _zipFile.Entries.FirstOrDefault(x => x.FileName == zipEntryName);
            if (entry != null)
                return entry.OpenReader();
            return null;
        }
    }
}