using System;
using System.Collections.Generic;

namespace Diva.Interfaces
{
    public interface IWifiApp
    {
        int AdminUserLevel
        {
            get;
        }

        void Register(IWifiAddon addon, string menuAnchor, string path);
        T GetServiceObject<T>();
        bool TryGetSessionInfo(IRequest req, out ISessionInfo sinfo);
        string ReadFile(IEnvironment env, string path);
        string ReadFile(IEnvironment env, string path, List<object> loo);
    }

    public interface IRequest
    {
    }

    public interface ISessionInfo
    {
    }
}
