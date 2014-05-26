using System;

namespace Flint.Core
{
    [Obsolete]
    public static class Dependencies
    {
        private static Func<IZip> _zipFactory;

        public static void RegisterZipImplementation(Func<IZip> zipFactory)
        {
            if (zipFactory == null) throw new ArgumentNullException("zipFactory");
            if (_zipFactory != null) throw new Exception("Zip Implementation may only be set once");
            _zipFactory = zipFactory;
        }

        public static IZip GetZip()
        {
            if (_zipFactory == null)
                throw new Exception("Zip implementation not set.");
            return _zipFactory();
        }
    }
}