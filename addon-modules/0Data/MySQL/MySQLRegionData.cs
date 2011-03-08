/*
 * Copyright (c) Crista Lopes (aka Diva) and Marcus Kirsch (aka Marck). All rights reserved.
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

using MySql.Data.MySqlClient;
using OpenMetaverse;

using OpenSim.Data;
using OpenSim.Data.MySQL;

namespace Diva.Data.MySQL
{
    /// <summary>
    /// A RegionData Interface to the MySQL database
    /// </summary>
    public class MySQLRegionData : OpenSim.Data.MySQL.MySqlRegionData, IRegionData
    {
        protected override Assembly Assembly
        {
            get { return GetType().BaseType.Assembly; }
        }

        private MySQLGenericTableHandler<RegionData> m_DatabaseHandler;

        public MySQLRegionData(string connectionString, string realm)
            : base(connectionString, realm)
        {
            m_DatabaseHandler = new MySQLGenericTableHandler<RegionData>(connectionString, realm, "GridStore");
        }

        public RegionData[] Get(UUID scopeID, int regionFlags, int excludeFlags)
        {
            return m_DatabaseHandler.Get(CreateWhereClause(scopeID, regionFlags, excludeFlags));
        }

        public long GetCount(UUID scopeID, int regionFlags, int excludeFlags)
        {
            return m_DatabaseHandler.GetCount(CreateWhereClause(scopeID, regionFlags, excludeFlags));
        }

        private string CreateWhereClause(UUID scopeID, int regionFlags, int excludeFlags)
        {
            string where = "(flags & {0}) <> 0 and (flags & {1}) = 0";
            if (scopeID != UUID.Zero)
                where += " and ScopeID = " + scopeID.ToString();
            return string.Format(where, regionFlags.ToString(), excludeFlags.ToString());
        }
    }
}
