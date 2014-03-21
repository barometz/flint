using System;
using System.IO;

namespace Flint.Core.Dependencies
{
    public interface IZip : IDisposable
    {
        bool Open( string zipPath );
        Stream OpenEntryStream( string zipFileItem );
    }
}