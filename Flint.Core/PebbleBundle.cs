using System;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Json;

namespace Flint.Core
{
    /// <summary> Represents a Pebble app bundle (.pbw file). </summary>
    //TODO: Break this into two sub classes AppBundle and FirmwareBundle.
    public class PebbleBundle
    {
        //TODO: Rename, singular
        public enum BundleTypes
        {
            Application,
            Firmware
        }

        /// <summary>
        ///     Create a new PebbleBundle from a .pwb file and parse its metadata.
        /// </summary>
        /// <param name="bundle">The stream to the bundle.</param>
        /// <param name="zip">The zip library implementation.</param>
        public PebbleBundle(Stream bundle, IZip zip)
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

            if (Manifest.Type == "firmware")
            {
                BundleType = BundleTypes.Firmware;

                using (Stream binStream = zip.OpenEntryStream(Manifest.Firmware.Filename))
                {
                    Firmware = Util.GetBytes(binStream);
                }
            }
            else
            {
                BundleType = BundleTypes.Application;
                using (Stream binStream = zip.OpenEntryStream(Manifest.Application.Filename))
                {
                    if (binStream == null)
                    {
                        throw new Exception(string.Format("App file {0} not found in archive", Manifest.Application.Filename));
                    }

                    AppMetadata = Util.ReadStruct<ApplicationMetadata>(binStream);
                }

                using (Stream appBinary = zip.OpenEntryStream(Manifest.Application.Filename))
                {
                    if (appBinary == null)
                        throw new PebbleException("Could find application entry in the bundle");

                    App = Util.GetBytes(appBinary);
                }
            }

            if (HasResources)
            {
                using (Stream resourcesBinary = zip.OpenEntryStream(Manifest.Resources.Filename))
                {
                    if (resourcesBinary == null)
                        throw new PebbleException("Could not find resource entry in the bundle");

                    Resources = Util.GetBytes(resourcesBinary);
                }
            }
        }

        public PebbleBundle()
        { }

        public virtual BundleTypes BundleType { get; private set; }
        public virtual bool HasResources { get; private set; }
        public ApplicationMetadata AppMetadata { get; private set; }
        public BundleManifest Manifest { get; private set; }
        public byte[] App { get; private set; }
        public virtual byte[] Firmware { get; private set; }
        public virtual byte[] Resources { get; private set; }

        public override string ToString()
        {
            if (BundleType == BundleTypes.Application)
            {
                return string.Format("watch app {0}", AppMetadata);
            }

            // This is pretty ugly, but will do for now.
            return string.Format("fw version {0} for hw rev {1}",
                                 Manifest.Resources.FriendlyVersion, Manifest.Firmware.HardwareRevision);
        }
    }
}