using System;
using System.IO;
using System.Linq;
using System.Reflection;

namespace Flint.Core.Tests.Resources
{
    public class ResourceManager
    {
        public static Stream GetTestBundle()
        {
            return GetResourceByName("demo.pbw");
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