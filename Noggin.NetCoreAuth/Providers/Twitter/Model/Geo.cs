using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Providers.Twitter.Model
{
    public class Geo
    {
        public IList<double> Coordinates { get; set; }
        public string Type { get; set; }
    }
}