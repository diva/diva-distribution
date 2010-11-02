/**
 * Copyright (c) Marcus Kirsch (aka Marck). All rights reserved.
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
using OpenSim.Services.Interfaces;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;

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
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                    env.Flags |= Flags.IsAdmin & Flags.AllowHyperlinks;
                if (sinfo.Account.UserLevel >= m_WebApp.HyperlinksUserLevel)
                    env.Flags |= Flags.AllowHyperlinks;
                if ((env.Flags & Flags.AllowHyperlinks) == 0)
                    env.State = State.HyperlinkList;
                else
                    env.State = State.HyperlinkListForm;
                env.Data = GetHyperlinks(env, sinfo.Account.PrincipalID);

                return PadURLs(env, sinfo.Sid, m_WebApp.ReadFile(env, "index.html"));
            }
            return m_WebApp.ReadFile(env, "index.html");
        }

        public string HyperlinkAddRequest(Environment env, string address, uint xloc, uint yloc)
        {
            m_log.Debug("[Wifi]: HyperlinkAddRequest");

            SessionInfo sinfo;
            if (TryGetSessionInfo(env.Request, out sinfo))
            {
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                    env.Flags |= Flags.IsAdmin;

                if (sinfo.Account.UserLevel >= m_WebApp.HyperlinksUserLevel ||
                    sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                {
                    if (address != string.Empty)
                    {
                        string reason;
                        if (WifiUtils.IsValidRegionAddress(address))
                        {
                            UUID owner = sinfo.Account.PrincipalID;
                            if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                                owner = UUID.Zero;
                            // Create hyperlink
                            xloc = xloc * Constants.RegionSize;
                            yloc = yloc * Constants.RegionSize;
                            if (m_GridService.TryLinkRegionToCoords(UUID.Zero, address, xloc, yloc, owner, out reason) == null)
                                reason = "Failed to link region: " + reason;
                            else
                                reason = "Region link to " + address + " established. (If this link already existed, then it will remain at the original location.)";
                        }
                        else
                            reason = "Invalid region address.";
                        NotifyOK(env, reason, delegate(Environment e) { return HyperlinkGetRequest(e); });
                    }
                    else
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
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                    env.Flags |= Flags.IsAdmin;
                if (sinfo.Account.UserLevel >= m_WebApp.HyperlinksUserLevel || (env.Flags & Flags.IsAdmin) != 0)
                {
                    GridRegion region = m_GridService.GetRegionByUUID(UUID.Zero, regionID);
                    if (region != null)
                    {
                        if (((env.Flags & Flags.IsAdmin) != 0) || (region.EstateOwner == sinfo.Account.PrincipalID))
                        {
                            RegionInfo link = new RegionInfo(region);
                            UserAccount user = sinfo.Account;
                            if (region.EstateOwner != user.PrincipalID)
                                user = m_UserAccountService.GetUserAccount(UUID.Zero, region.EstateOwner);
                            if (user != null)
                                link.RegionOwner = user.Name;
                            List<object> loo = new List<object>();
                            loo.Add(link);
                            env.State = State.HyperlinkDeleteForm;
                            env.Data = loo;
                        }
                        else
                            m_log.WarnFormat("[Wifi]: Unauthorized attempt to delete hyperlink {0}:{1} ({2}) by {3} ({4})",
                                region.ExternalHostName, region.HttpPort, region.RegionName, sinfo.Account.Name, sinfo.Account.PrincipalID);
                    }
                    else
                    {
                        m_log.WarnFormat("[Wifi]: Attempt to delete an inexistent region link for UUID {0} by {1} ({2})",
                            regionID, sinfo.Account.Name, sinfo.Account.PrincipalID);
                        NotifyOK(env, "Region link not found", delegate(Environment e) { return HyperlinkGetRequest(e); });
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
                env.Session = sinfo;
                env.Flags = Flags.IsLoggedIn;
                if (sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                    env.Flags |= Flags.IsAdmin;
                if (sinfo.Account.UserLevel >= m_WebApp.HyperlinksUserLevel ||
                    sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel)
                {
                    // Try to delete hyperlink
                    GridRegion region = m_GridService.GetRegionByUUID(UUID.Zero, regionID);
                    if (region != null)
                    {
                        if ((sinfo.Account.UserLevel >= m_WebApp.AdminUserLevel) ||
                            (region.EstateOwner == sinfo.Account.PrincipalID))
                        {
                            if (m_GridService.TryUnlinkRegion(region.RegionName))
                                NotifyOK(env, "Deleted region link " + region.RegionName,
                                    delegate(Environment e) { return HyperlinkGetRequest(e); });
                            else
                                NotifyOK(env, "Deletion of region link " + region.RegionName + " failed.",
                                    delegate(Environment e) { return HyperlinkGetRequest(e); });
                        }
                        else
                            m_log.WarnFormat("[Wifi]: Unauthorized attempt to delete hyperlink {0}:{1} ({2}) by {3} ({4})",
                                region.ExternalHostName, region.HttpPort, region.RegionName, sinfo.Account.Name, sinfo.Account.PrincipalID);
                    }
                    else
                    {
                        m_log.WarnFormat("[Wifi]: Attempt to delete an inexistent region link for UUID {0} by {1} ({2})",
                            regionID, sinfo.Account.Name, sinfo.Account.PrincipalID);
                        NotifyOK(env, "Region link not found", delegate(Environment e) { return HyperlinkGetRequest(e); });
                    }
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
                foreach (GridRegion region in hyperlinks)
                {
                    RegionInfo link = new RegionInfo(region);
                    if (m_WebApp.HyperlinksShowAll ||
                        (env.Flags & Flags.IsAdmin) != 0 ||
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
    }
}
