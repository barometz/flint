using System;
using System.IO;
using System.Runtime.Serialization.Json;

namespace Flint.Core.Bundles
{
    /// <summary> Represents a Pebble app bundle (.pbw file). </summary>
    public abstract class BundleBase
    {
        public virtual bool HasResources { get; private set; }
        public BundleManifest Manifest { get; private set; }
        public virtual byte[] Resources { get; private set; }

        protected abstract void LoadData(IZip zip);

        /// <summary>
        ///     Create a new PebbleBundle from a .pwb file and parse its metadata.
        /// </summary>
        /// <param name="bundle">The stream to the bundle.</param>
        /// <param name="zip">The zip library implementation.</param>
        public void Load(Stream bundle, IZip zip)
        {
            //TODO: This needs to be refactored, probably put into a Load method
            if (false == zip.Open(bundle))
                throw new InvalidOperationException("Failed to open pebble bundle");

            using (Stream manifestStream = zip.OpenEntryStream("manifest.json"))
            {
                if (manifestStream == null)
                {
                    throw new InvalidOperationException("manifest.json not found in archive - not a valid Pebble bundle.");
                }
                var serializer = new DataContractJsonSerializer(typeof(BundleManifest));
                Manifest = (BundleManifest)serializer.ReadObject(manifestStream);
            }

            HasResources = (Manifest.Resources.Size != 0);

            if (HasResources)
            {
                using (Stream resourcesBinary = zip.OpenEntryStream(Manifest.Resources.Filename))
                {
                    if (resourcesBinary == null)
                        throw new PebbleException("Could not find resource entry in the bundle");

                    Resources = Util.GetBytes(resourcesBinary);
                }
            }

            LoadData(zip);
        }
    }
}