using System;
using System.Collections.Generic;
using System.Text;

namespace Reactor
{
    public interface IDuplexable<T> : IReadable<T>, IWriteable<T>
    {

    }
}
