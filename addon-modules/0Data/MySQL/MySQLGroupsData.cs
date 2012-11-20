/*
 * Copyright (c) Contributors, http://opensimulator.org/
 * See CONTRIBUTORS.TXT for a full list of copyright holders.
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
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

using OpenSim.Data.MySQL;

using OpenMetaverse;
using MySql.Data.MySqlClient;

namespace Diva.Data.MySQL
{
    public class MySQLGroupsData : IGroupsData
    {
        private MySqlGroupsGroupsHandler m_Groups;
        private MySqlGroupsMembershipHandler m_Membership;
        private MySqlGroupsRolesHandler m_Roles;
        private MySqlGroupsRoleMembershipHandler m_RoleMembership;
        private MySqlGroupsInvitesHandler m_Invites;
        private MySqlGroupsNoticesHandler m_Notices;
        private MySqlGroupsPrincipalsHandler m_Principals;

        public MySQLGroupsData(string connectionString, string realm)
        {
            m_Groups = new MySqlGroupsGroupsHandler(connectionString, realm + "_groups", realm + "_Store");
            m_Membership = new MySqlGroupsMembershipHandler(connectionString, realm + "_membership");
            m_Roles = new MySqlGroupsRolesHandler(connectionString, realm + "_roles");
            m_RoleMembership = new MySqlGroupsRoleMembershipHandler(connectionString, realm + "_rolemembership");
            m_Invites = new MySqlGroupsInvitesHandler(connectionString, realm + "_invites");
            m_Notices = new MySqlGroupsNoticesHandler(connectionString, realm + "_notices");
            m_Principals = new MySqlGroupsPrincipalsHandler(connectionString, realm + "_principals");
        }

        #region groups table
        public bool StoreGroup(GroupData data)
        {
            return m_Groups.Store(data);
        }

        public GroupData RetrieveGroup(UUID groupID)
        {
            GroupData[] groups = m_Groups.Get("GroupID", groupID.ToString());
            if (groups.Length > 0)
                return groups[0];

            return null;
        }

        public GroupData[] RetrieveGroups(string pattern)
        {
            return m_Groups.Get(pattern);
        }

        public bool DeleteGroup(UUID groupID)
        {
            return m_Groups.Delete("GroupID", groupID.ToString());
        }
        #endregion

        #region membership table
        public MembershipData[] RetrieveMembers(UUID groupID)
        {
            return m_Membership.Get("GroupID", groupID.ToString());
        }

        public bool StoreMember(MembershipData data)
        {
            return m_Membership.Store(data);
        }

        public bool DeleteMember(UUID groupID, string pricipalID)
        {
            return m_Membership.Delete(new string[] { "GroupID", "PrincipalID" }, 
                                       new string[] { groupID.ToString(), pricipalID });
        }
        #endregion

        #region roles table
        public bool StoreRole(RoleData data)
        {
            return m_Roles.Store(data);
        }

        public RoleData[] RetrieveRoles(UUID groupID)
        {
            return m_Roles.Get("GroupID", groupID.ToString());
        }

        public bool DeleteRole(UUID groupID, UUID roleID)
        {
            return m_Roles.Delete(new string[] { "GroupID", "RoleID" }, 
                                  new string[] { groupID.ToString(), roleID.ToString() });
        }
        #endregion

        #region rolememberhip table
        public RoleMembershipData[] RetrieveRoleMembers(UUID groupID)
        {
            return m_RoleMembership.Get("GroupID", groupID.ToString());
        }

        public bool StoreRoleMember(RoleMembershipData data)
        {
            return m_RoleMembership.Store(data);
        }

        public bool DeleteRoleMember(RoleMembershipData data)
        {
            return m_RoleMembership.Delete(new string[] { "GroupID", "RoleID", "PrincipalID"},
                                           new string[] { data.GroupID.ToString(), data.RoleID.ToString(), data.PrincipalID });
        }
        #endregion

        #region principals table
        public bool StorePrincipal(PrincipalData data)
        {
            return m_Principals.Store(data);
        }

        public PrincipalData RetrievePrincipal(string principalID)
        {
            PrincipalData[] p = m_Principals.Get("PrincipalID", principalID);
            if (p != null && p.Length > 0)
                return p[0];

            return null;
        }

        public bool DeletePrincipal(string principalID)
        {
            return m_Principals.Delete("PrincipalID", principalID);
        }
        #endregion

        #region invites table
        #endregion

        #region notices table
        #endregion

        #region combinations
        public MembershipData RetrievePrincipalGroupMembership(string principalID, UUID groupID)
        {
            // TODO
            return null;
        }
        public MembershipData[] RetrievePrincipalGroupMemberships(string principalID)
        {
            // TODO
            return null;
        }
        #endregion
    }

    public class MySqlGroupsGroupsHandler : MySQLGenericTableHandler<GroupData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsGroupsHandler(string connectionString, string realm, string store) 
            : base(connectionString, realm, store)
        {
        }
    }

    public class MySqlGroupsMembershipHandler : MySQLGenericTableHandler<MembershipData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsMembershipHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }

    public class MySqlGroupsRolesHandler : MySQLGenericTableHandler<RoleData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsRolesHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }

    public class MySqlGroupsRoleMembershipHandler : MySQLGenericTableHandler<RoleMembershipData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsRoleMembershipHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }

    public class MySqlGroupsInvitesHandler : MySQLGenericTableHandler<GroupData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsInvitesHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }

    public class MySqlGroupsNoticesHandler : MySQLGenericTableHandler<GroupData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsNoticesHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }

    public class MySqlGroupsPrincipalsHandler : MySQLGenericTableHandler<PrincipalData>
    {
        protected override Assembly Assembly
        {
            // WARNING! Moving migrations to this assembly!!!
            get { return GetType().Assembly; }
        }

        public MySqlGroupsPrincipalsHandler(string connectionString, string realm) 
            : base(connectionString, realm, string.Empty)
        {
        }
    }
}
