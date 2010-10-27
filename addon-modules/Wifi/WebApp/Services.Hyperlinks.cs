/**
 * Copyright (c) Marck. All rights reserved.
 * 
 * Redistribution and use in source and binary forms, with or without modification, 
 * are permitted provided that the following conditions are met:
 * 
 *     * Redistributions of source code must retain the above copyright notice, 
 *       this list of conditions and the following disclaimer.
 *     * Redistributions in binary form must reproduce the above copyright notice, 
 *       this list of conditions and the following disclaimer in the documentation 
 *       and/or other materials provided with the distribution.
 *     * Neither the name of the Organizations nor the names of Individual
 *       Contributors may be used to endorse or promote products derived from 
 *       this software without specific prior written permission.
 * 
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS "AS IS" AND 
 * ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT LIMITED TO, THE IMPLIED WARRANTIES 
 * OF MERCHANTABILITY AND FITNESS FOR A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL 
 * THE COPYRIGHT OWNER OR CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL, 
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO, PROCUREMENT OF SUBSTITUTE 
 * GOODS OR SERVICES; LOSS OF USE, DATA, OR PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED 
 * AND ON ANY THEORY OF LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING 
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS SOFTWARE, EVEN IF ADVISED 
 * OF THE POSSIBILITY OF SUCH DAMAGE.
 * 
 */
using System.Collections.Generic;

using OpenMetaverse;

using OpenSim.Framework;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public partial class Services
    {
        public string HyperlinkGetRequest(Environment env)
        {
            m_log.Debug("[Wifi]: HyperlinkGetRequest");

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                if (sinfo.Account.UserLevel >= 100)
                    env.Flags |= Flags.IsAdmin;
                if (m_WebApp.EnableHyperlinks || (env.Flags & Flags.IsAdmin) != 0)
                {
                    env.State = State.HyperlinkListForm;
                    env.Data = GetHyperlinks(env, sinfo.Account.PrincipalID);
                }
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            return m_WebApp.ReadFile(env, "index.html");
        }

        private List<object> GetHyperlinks(Environment env, UUID owner)
        {
            List<GridRegion> hyperlinks = m_GridService.GetHyperlinks(UUID.Zero);
            List<RegionInfo> links = new List<RegionInfo>();
            if (hyperlinks != null)
            {
                foreach(GridRegion region in hyperlinks)
                {
                    RegionInfo link = new RegionInfo(region);
                    if ((env.Flags & Flags.IsAdmin) != 0 ||
                        (link.RegionOwnerID == owner) ||
                        (link.RegionOwnerID == UUID.Zero))
                    {
                        if (link.RegionOwnerID != UUID.Zero)
                        {
                            UserAccount user = m_UserAccountService.GetUserAccount(UUID.Zero, link.RegionOwnerID);
                            if (user != null)
                                link.RegionOwner = user.Name;
                        }
                        links.Add(link);
                    }
                }
            }
            return Objectify<RegionInfo>(links);
        }
        
        public string HyperlinkAddRequest(Environment env, string address, uint xloc, uint yloc)
        {
            m_log.Debug("[Wifi]: HyperlinkAddRequest");

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                if (m_WebApp.EnableHyperlinks || sinfo.Account.UserLevel >= 100)
                {
                    if (address != string.Empty)
                    {
                        UUID owner = sinfo.Account.PrincipalID;
                        if (sinfo.Account.UserLevel >= 100)
                            owner = UUID.Zero;
                        // Create hyperlink
                        xloc = xloc * Constants.RegionSize;
                        yloc = yloc * Constants.RegionSize;
                        string reason = string.Empty;
                        if (m_GridService.TryLinkRegionToCoords(UUID.Zero, address, xloc, yloc, owner, out reason) == null)
                            m_log.Debug("[Wifi]: Failed to link region: " + reason);
                        else
                            m_log.Debug("[Wifi]: Hyperlink established");
                    }
                    return HyperlinkGetRequest(env);
                }
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string HyperlinkDeleteGetRequest(Environment env, UUID regionID)
        {
            m_log.DebugFormat("[Wifi]: HyperlinkDeleteGetRequest {0}", regionID);

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                if (sinfo.Account.UserLevel >= 100)
                    env.Flags |= Flags.IsAdmin;
                if (m_WebApp.EnableHyperlinks || (env.Flags & Flags.IsAdmin) != 0)
                {
                    GridRegion region = m_GridService.GetRegionByUUID(UUID.Zero, regionID);
                    if (region != null)
                    {
                        if ((sinfo.Account.UserLevel >= 100) || (region.EstateOwner == sinfo.Account.PrincipalID))
                        {
                            RegionInfo link = new RegionInfo(region);
                            UserAccount user = m_UserAccountService.GetUserAccount(UUID.Zero, region.EstateOwner);
                            if (user != null)
                                link.RegionOwner = user.Name;
                            List<object> loo = new List<object>();
                            loo.Add(link);
                            env.State = State.HyperlinkDeleteForm;
                            env.Data = loo;
                        }
                        else // Not allowed to delete this hyperlink
                            return HyperlinkGetRequest(env);
                    }
                }
            }
            return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
        }

        public string HyperlinkDeletePostRequest(Environment env, UUID regionID)
        {
            m_log.DebugFormat("[Wifi]: HyperlinkDeletePostRequest {0}", regionID);

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                if (m_WebApp.EnableHyperlinks || sinfo.Account.UserLevel >= 100)
                {
                    // Try to delete hyperlink
                    GridRegion region = m_GridService.GetRegionByUUID(UUID.Zero, regionID);
                    if (region != null)
                    {
                        if ((sinfo.Account.UserLevel >= 100) ||
                            (region.EstateOwner == sinfo.Account.PrincipalID))
                        {
                            if (m_GridService.TryUnlinkRegion(region.RegionName))
                                m_log.DebugFormat("[Wifi]: Deleted link to region {0}", region.RegionName);
                        }
                        else
                            m_log.WarnFormat("[Wifi]: Unauthorized attempt at deleteing link to region {0}", region.RegionName);
                    }
                    else
                        m_log.Debug("[Wifi]: Attempt at deleting an inexistent region link");
                    return HyperlinkGetRequest(env);
                }
                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            return m_WebApp.ReadFile(env, "index.html");
        }
    }
}
