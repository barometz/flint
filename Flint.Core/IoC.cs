using System;

namespace Flint.Core
{
    public static class IoC
    {
        private static IDependencyResolver _resolver;

        public static void RegisterResolver( IDependencyResolver resolver )
        {
            if ( resolver == null ) throw new ArgumentNullException( "resolver" );
            if ( _resolver != null ) throw new InvalidOperationException( "The resolver can only be set once" );
            _resolver = resolver;
        }

        public static T Resolve<T>()
        {
            if ( _resolver == null )
                throw new InvalidOperationException( "The dependency resolver must be set" );
            return _resolver.Resolve<T>();
        }

        public static void Clear()
        {
            if ( _resolver == null )
                throw new InvalidOperationException( "The dependency resolver must be set" );
            _resolver.Clear();
        }
    }
}