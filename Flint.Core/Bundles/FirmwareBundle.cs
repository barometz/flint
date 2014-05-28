using System;
using System.IO;

namespace Flint.Core.Bundles
{
    public class FirmwareBundle : BundleBase
    {
        public virtual byte[] Firmware { get; private set; }

        protected override void LoadData(IZip zip)
        {
            if (string.IsNullOrWhiteSpace(Manifest.Firmware.Filename))
                throw new InvalidOperationException("Bundle does not contain firmware");

            using (Stream binStream = zip.OpenEntryStream(Manifest.Firmware.Filename))
            {
                Firmware = Util.GetBytes(binStream);
            }
        }

        public override string ToString()
        {
            // This is pretty ugly, but will do for now.
            return string.Format("fw version {0} for hw rev {1}",
                                 Manifest.Resources.FriendlyVersion, Manifest.Firmware.HardwareRevision);
        }
    }
}