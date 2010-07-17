using System;
using System.Collections.Generic;

using log4net;
using OpenMetaverse;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using Diva.OpenSimServices;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string InventoryGetRequest(Environment env)
        {
            if (!m_WebApp.IsInstalled)
            {
                m_log.DebugFormat("[Wifi]: warning: someone is trying to access UserAccountGetRequest and Wifi isn't isntalled!");
                return m_WebApp.ReadFile(env, "index.html");
            }

            m_log.DebugFormat("[Wifi]: InventoryGetRequest");
            Request request = env.Request;

            SessionInfo sinfo;
            if (TryGetSessionInfo(request, out sinfo))
            {
                env.Session = sinfo;
                InventoryTreeNode tree = m_InventoryService.GetInventoryTree(sinfo.Account.PrincipalID);
                List<object> loo = new List<object>();
                foreach (InventoryTreeNode n in tree.Children) // skip the artificial first level
                    loo.Add(n);
                //loo.Add(tree);
                env.Data = loo;
                env.Flags = Flags.IsLoggedIn;
                env.State = State.InventoryListForm;
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            else
            {
                return m_WebApp.ReadFile(env, "index.html");
            }
        }
    }
}