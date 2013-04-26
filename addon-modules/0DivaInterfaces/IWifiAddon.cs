using System;
using System.Collections.Generic;

using Nini.Config;

using OpenSim.Framework.Servers.HttpServer;

namespace Diva.Interfaces
{
    public interface IWifiAddon
    {
    }

    public struct WifiAddon
    {
        public IWifiAddon Addon;
        public string MenuAnchor;
        public string Path;
    }

}
