using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Flint.Core.Tests.Resources
{
    public class ResourceManager
    {
        public static Stream GetAppBundle()
        {
            return GetResourceByName("demo.pbw");
        }

        public static Stream GetFirmwareBundle()
        {
            return GetResourceByName("Pebble-2.1-v1_5.pbz");
        }

        private static Stream GetResourceByName(string name)
        {
            var assembly = Assembly.GetExecutingAssembly();
            string[] fullyQualifiedNames = assembly.GetManifestResourceNames();
            string resourceName = fullyQualifiedNames.FirstOrDefault(s => s.Contains(name));
            if (resourceName == null)
            {
                throw new ArgumentException(name + " could not be found.  Ensure that the Build Action is set to Embedded Resource.");
            }
            return assembly.GetManifestResourceStream(resourceName);
        }
    }
}