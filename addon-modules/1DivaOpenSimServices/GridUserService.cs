/*
 * Copyright (c) Crista Lopes (aka Diva). All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without
 * modification, are permitted provided that the following conditions are met:
 *     * Redistributions of source code must retain the above copyright
 *       notice, this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright
 *       notice, this list of conditions and the following disclaimer in the
 *       documentation and/or other materials provided with the distribution.
 *     * Neither the name of the OpenSimulator Project nor the
 *       names of its contributors may be used to endorse or promote products
 *       derived from this software without specific prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE DEVELOPERS ``AS IS'' AND ANY
 * EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED
 * WARRANTIES OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE
 * DISCLAIMED. IN NO EVENT SHALL THE CONTRIBUTORS BE LIABLE FOR ANY
 * DIRECT, INDIRECT, INCIDENTAL, SPECIAL, EXEMPLARY, OR CONSEQUENTIAL DAMAGES
 * (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES;
 * LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND
 * ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT
 * (INCLUDING NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

using System;
using System.Collections.Generic;
using System.Reflection;
using log4net;
using Nini.Config;

using OpenMetaverse;
using OpenSim.Data;
using OpenSim.Framework;
using OpenSim.Services.Interfaces;

namespace Diva.OpenSimServices
{
    public class GridUserService : OpenSim.Services.UserAccountService.GridUserService, IGridUserService
    {
        private static readonly ILog m_log = LogManager.GetLogger(MethodBase.GetCurrentMethod().DeclaringType);
        private static readonly string m_CastWarning = "[DivaData]: Invalid cast for GridUser store. Diva.Data required for method {0}.";

        public GridUserService(IConfigSource config)
            : base(config)
        {
            try
            {
                ((Diva.Data.IGridUserData)m_Database).ResetOnline();
            }
            catch (InvalidCastException)
            {
                m_log.WarnFormat(m_CastWarning, MethodBase.GetCurrentMethod().Name);
            }
        }

        public List<DGridUserInfo> GetOnlineUsers()
        {
            List<DGridUserInfo> onlineList = new List<DGridUserInfo>();
            try
            {
                GridUserData[] onlines = ((Diva.Data.IGridUserData)m_Database).GetOnlineUsers();
                foreach (GridUserData d in onlines)
                    onlineList.Add(ToGridUserInfo(d));

            }
            catch (InvalidCastException)
            {
                m_log.WarnFormat(m_CastWarning, MethodBase.GetCurrentMethod().Name);
            }

            return onlineList;
        }

        public long GetOnlineUserCount()
        {
            try
            {
                return ((Diva.Data.IGridUserData)m_Database).GetOnlineUserCount();
            }
            catch (InvalidCastException)
            {
                m_log.WarnFormat(m_CastWarning, MethodBase.GetCurrentMethod().Name);
            }

            return 0;
        }

        public long GetActiveUserCount(int period)
        {
            try
            {
                return ((Diva.Data.IGridUserData)m_Database).GetActiveUserCount(period);
            }
            catch (InvalidCastException)
            {
                m_log.WarnFormat(m_CastWarning, MethodBase.GetCurrentMethod().Name);
            }

            return 0;
        }

        public override GridUserInfo GetGridUserInfo(string userID)
        {
            GridUserData d = GetGridUserData(userID);

            if (d == null)
                return null;

            return ToGridUserInfo(d);

        }

        public override GridUserInfo[] GetGridUserInfo(string[] userIDs)
        {
            List<GridUserInfo> ret = new List<GridUserInfo>();

            foreach (string id in userIDs)
                ret.Add(GetGridUserInfo(id));

            return ret.ToArray();
        }

        public DGridUserInfo[] GetGridUsers(string pattern)
        {
            GridUserData[] gusers = ((Diva.Data.IGridUserData)m_Database).GetUsers(pattern);

            if (gusers == null)
                return new DGridUserInfo[0];

            DGridUserInfo[] guinfos = new DGridUserInfo[gusers.Length];
            int i = 0;
            foreach (GridUserData gu in gusers)
            {
                guinfos[i++] = ToGridUserInfo(gu);
            }

            return guinfos;
        }

        public bool StoreTOS(DGridUserInfo info)
        {
            GridUserData d = m_Database.Get(info.UserID);
            if (d != null)
            {
                if (d.Data.ContainsKey("TOS"))
                {
                    d.Data["TOS"] = info.TOS;
                    return m_Database.Store(d);
                }
            }

            return false;
        }

        public void ResetTOS()
        {
            try
            {
                ((Diva.Data.IGridUserData)m_Database).ResetTOS();
            }
            catch (InvalidCastException)
            {
                m_log.WarnFormat(m_CastWarning, MethodBase.GetCurrentMethod().Name);
            }
        }

        protected DGridUserInfo ToGridUserInfo(GridUserData d)
        {
            DGridUserInfo info = new DGridUserInfo();
            info.UserID = d.UserID;

            Dictionary<string, string> kvp = d.Data;
            string tmpstr;

            if (kvp.TryGetValue("HomeRegionID", out tmpstr))
                info.HomeRegionID = new UUID(tmpstr);
            else
                info.HomeRegionID = UUID.Zero;

            if (kvp.TryGetValue("HomePosition", out tmpstr))
                info.HomePosition = Vector3.Parse(tmpstr);
            else
                info.HomePosition = Vector3.Zero;

            if (kvp.TryGetValue("HomeLookAt", out tmpstr))
                info.HomeLookAt = Vector3.Parse(tmpstr);
            else
                info.HomeLookAt = Vector3.Zero;

            if (kvp.TryGetValue("LastRegionID", out tmpstr))
                info.LastRegionID = new UUID(tmpstr);
            else
                info.LastRegionID = UUID.Zero;

            if (kvp.TryGetValue("LastPosition", out tmpstr))
                info.LastPosition = Vector3.Parse(tmpstr);
            else
                info.LastPosition = Vector3.Zero;

            if (kvp.TryGetValue("LastLookAt", out tmpstr))
                info.LastLookAt = Vector3.Parse(tmpstr);
            else
                info.LastLookAt = Vector3.Zero;

            if (kvp.TryGetValue("Online", out tmpstr))
                info.Online = bool.Parse(tmpstr);
            else
                info.Online = false;

            if (kvp.TryGetValue("Login", out tmpstr))
                info.Login = Util.ToDateTime(Convert.ToInt32(tmpstr));
            else
                info.Login = Util.UnixEpoch;

            if (kvp.TryGetValue("Logout", out tmpstr))
                info.Logout = Util.ToDateTime(Convert.ToInt32(tmpstr));
            else
                info.Logout = Util.UnixEpoch;

            if (kvp.TryGetValue("TOS", out tmpstr))
                info.TOS = tmpstr;
            else
                info.TOS = string.Empty;


            return info;
        }

        private static ExpiringCacheOS<string, GridUserData> cache = new ExpiringCacheOS<string, GridUserData>(00000);
        private GridUserData GetGridUserData(string userID)
        {
            if (userID.Length > 36)
                userID = userID.Substring(0, 36);

            if (cache.TryGetValue(userID, out GridUserData d))
                return d;

            GridUserData[] ds = m_Database.GetAll(userID);
            if (ds == null || ds.Length == 0)
            {
                cache.Add(userID, null, 300000);
                return null;
            }

            d = ds[0];
            if (ds.Length > 1)
            {
                // try find most recent record
                try
                {
                    int tsta = int.Parse(d.Data["Login"]);
                    int tstb = int.Parse(d.Data["Logout"]);
                    int cur = tstb > tsta ? tstb : tsta;

                    for (int i = 1; i < ds.Length; ++i)
                    {
                        GridUserData dd = ds[i];
                        tsta = int.Parse(dd.Data["Login"]);
                        tstb = int.Parse(dd.Data["Logout"]);
                        if (tsta > tstb)
                            tstb = tsta;
                        if (tstb > cur)
                        {
                            cur = tstb;
                            d = dd;
                        }
                    }
                }
                catch { }
            }
            cache.Add(userID, d, 300000);
            return d;
        }


    }

    /// <summary>
    /// Additional GridUser data for D2
    /// </summary>
    public class DGridUserInfo : GridUserInfo
    {
        public string TOS = String.Empty;

        public DGridUserInfo() { }

        public DGridUserInfo(Dictionary<string, object> kvp)
            : base(kvp)
        {
            if (kvp.ContainsKey("TOS"))
                TOS = kvp["TOS"].ToString();

        }

        public override Dictionary<string, object> ToKeyValuePairs()
        {
            Dictionary<string, object> result = base.ToKeyValuePairs();
            result["TOS"] = TOS;

            return result;
        }
    }
}
