using System.Collections.Generic;

namespace Noggin.NetCoreAuth.Providers.Twitter.Model
{
    public class Geo
    {
        public List<double> Coordinates { get; set; }
        public string Type { get; set; }
    }
}