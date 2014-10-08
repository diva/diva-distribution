/*
 * Copyright (c) Marcus Kirsch (aka Marck). All rights reserved.
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
using System.Reflection;

using OpenSim.Data;
#if CSharpSqlite
    using Community.CsharpSqlite.Sqlite;
#else
using Mono.Data.Sqlite;
#endif

namespace Diva.Data.SQLite
{
    public class SQLiteGridUserData : OpenSim.Data.SQLite.SQLiteGridUserData, IGridUserData
    {
        private SQLiteGenericTableHandler<GridUserData> m_DatabaseHandler;
        
        protected override Assembly Assembly
        {
            get { return GetType().BaseType.Assembly; }
        }

        public SQLiteGridUserData(string connectionString, string realm) 
            : base(connectionString, realm)
        {
            m_DatabaseHandler = new SQLiteGenericTableHandler<GridUserData>(connectionString, realm, "GridUserStore");
        }

        public GridUserData[] GetOnlineUsers()
        {
            return m_DatabaseHandler.Get("Online", true.ToString());
        }

        public long GetOnlineUserCount()
        {
            return m_DatabaseHandler.GetCount("Online", true.ToString());
        }

        public long GetActiveUserCount(int period)
        {
            return m_DatabaseHandler.GetCount(string.Format("Online = '{0}' OR CAST(julianday('now')-julianday(datetime(Logout, 'unixepoch')) AS INTEGER) <= {1}", true, period));
        }

        public GridUserData[] GetUsers(string pattern)
        {
            return m_DatabaseHandler.Get(pattern);
        }

        public void ResetTOS()
        {
            using (SqliteCommand cmd = new SqliteCommand())
            {
                cmd.CommandText = String.Format("update {0} set TOS=?tos", m_Realm);
                cmd.Parameters.AddWithValue("?tos", "");
                DoQuery(cmd);
            }
        }

        public void ResetOnline()
        {
            using (SqliteCommand cmd = new SqliteCommand())
            {
                cmd.CommandText = String.Format("update {0} set Online='False'", m_Realm);
                DoQuery(cmd);
            }
        }

    }
}
