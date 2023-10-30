using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Providers;

public interface IProviderFactory
{
    Provider Get(string name);
    IList<Provider> Providers { get; }
}
