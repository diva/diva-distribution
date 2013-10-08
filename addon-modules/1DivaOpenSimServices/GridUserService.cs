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
            GridUserData d = null;
            if (userID.Length > 36) // it's a UUI
                d = m_Database.Get(userID);
            else // it's a UUID
            {
                GridUserData[] ds = m_Database.GetAll(userID);
                if (ds == null)
                    return null;

                if (ds.Length > 0)
                {
                    d = ds[0];
                    foreach (GridUserData dd in ds)
                        if (dd.UserID.Length > d.UserID.Length) // find the longest
                            d = dd;
                }
            }

            if (d == null)
                return null;

            DGridUserInfo info = ToGridUserInfo(d);

            return info;

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
            info.HomeRegionID = new UUID(d.Data["HomeRegionID"]);
            info.HomePosition = Vector3.Parse(d.Data["HomePosition"]);
            info.HomeLookAt = Vector3.Parse(d.Data["HomeLookAt"]);

            info.LastRegionID = new UUID(d.Data["LastRegionID"]);
            info.LastPosition = Vector3.Parse(d.Data["LastPosition"]);
            info.LastLookAt = Vector3.Parse(d.Data["LastLookAt"]);

            info.Online = bool.Parse(d.Data["Online"]);
            info.Login = Util.ToDateTime(Convert.ToInt32(d.Data["Login"]));
            info.Logout = Util.ToDateTime(Convert.ToInt32(d.Data["Logout"]));

            if (d.Data.ContainsKey("TOS") && d.Data["TOS"] != null)
                info.TOS = d.Data["TOS"];
            else
                info.TOS = string.Empty;


            return info;
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
