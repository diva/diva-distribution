using System;
using System.Collections.Generic;

namespace Diva.Interfaces
{
    public interface IWifiApp
    {
        void Register(IWifiAddon addon, string menuAnchor, string path);
        T GetServiceObject<T>();
    }
}
