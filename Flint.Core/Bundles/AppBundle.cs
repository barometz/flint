using System;
using System.IO;
using Flint.Core.Serialization;

namespace Flint.Core.Bundles
{
    public class AppBundle : BundleBase
    {
        public byte[] App { get; private set; }

        public ApplicationMetadata AppMetadata { get; private set; }

        protected override void LoadData(IZip zip)
        {
            if (string.IsNullOrWhiteSpace(Manifest.Application.Filename))
                throw new InvalidOperationException("Bundle does not contain pebble app");

            using (Stream binStream = zip.OpenEntryStream(Manifest.Application.Filename))
            {
                if (binStream == null)
                    throw new Exception(string.Format("App file {0} not found in archive", Manifest.Application.Filename));

                App = Util.GetBytes(binStream);

                AppMetadata = BinarySerializer.ReadObject<ApplicationMetadata>(App);
            }
        }

        public override string ToString()
        {
            return string.Format("watch app {0}", AppMetadata);
        }
    }
}