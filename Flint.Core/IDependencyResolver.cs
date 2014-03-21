namespace Flint.Core
{
    public interface IDependencyResolver
    {
        T Resolve<T>();
        void Clear();
    }
}