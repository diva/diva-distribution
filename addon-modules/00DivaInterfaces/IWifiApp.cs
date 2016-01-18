using System;
using System.Collections.Generic;
using OpenMetaverse;

namespace Diva.Interfaces
{
    public interface IWifiApp
    {
        int AdminUserLevel
        { get; }
        string GridName
        { get; }
        string LoginURL
        { get; }
        string WebAddress
        { get; }


        void Register(IWifiAddon addon, string menuAnchor, string path, PrivilegeLevel level = PrivilegeLevel.Admins);
        void RegisterEstateServiceObject(Object estateService);
        T GetServiceObject<T>();
        bool TryGetSessionInfo(IRequest req, out ISessionInfo sinfo);
        bool IsValidSessionForUser(string uid, string sid);
        
        string ReadFile(IEnvironment env, string path);
        string ReadFile(IEnvironment env, string path, List<object> loo);
        void NotifyWithoutButton(IEnvironment env, string message);

        void SetAvatar(IEnvironment env, UUID userID, string avatarType);
        bool SendEMail(string to, string subject, string message);
        bool SendEMailSync(string to, string cc, string bcc, string subject, string message);
    }

    public interface IRequest
    {
    }

    public interface ISessionInfo
    {
    }
}
