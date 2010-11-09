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
using System.Collections.Generic;

using MySql.Data.MySqlClient;

namespace Diva.Data.MySQL
{
    public class MySQLGenericTableHandler<T> : OpenSim.Data.MySQL.MySQLGenericTableHandler<T> where T : class, new()
    {
        public MySQLGenericTableHandler(string connectionString, string realm, string storeName)
            : base(connectionString, realm, storeName) { }

        public virtual long GetCount(string field, string key)
        {
            return GetCount(new string[] { field }, new string[] { key });
        }

        public virtual long GetCount(string[] fields, string[] keys)
        {
            if (fields.Length != keys.Length)
                return 0;

            List<string> terms = new List<string>();

            using (MySqlCommand cmd = new MySqlCommand())
            {
                for (int i = 0; i < fields.Length; i++)
                {
                    cmd.Parameters.AddWithValue(fields[i], keys[i]);
                    terms.Add("`" + fields[i] + "` = ?" + fields[i]);
                }

                string where = String.Join(" and ", terms.ToArray());

                string query = String.Format("select count(*) from {0} where {1}",
                                             m_Realm, where);

                cmd.CommandText = query;

                Object result = DoQueryScalar(cmd);

                return Convert.ToInt64(result);
            }
        }

        public virtual long GetCount(string where)
        {
            using (MySqlCommand cmd = new MySqlCommand())
            {
                string query = String.Format("select count(*) from {0} where {1}",
                                             m_Realm, where);

                cmd.CommandText = query;

                object result = DoQueryScalar(cmd);

                return Convert.ToInt64(result);
            }
        }

        public virtual object DoQueryScalar(MySqlCommand cmd)
        {
            using (MySqlConnection dbcon = new MySqlConnection(m_connectionString))
            {
                dbcon.Open();
                cmd.Connection = dbcon;

                return cmd.ExecuteScalar();
            }
        }

        new public virtual T[] DoQuery(MySqlCommand cmd)
        {
            return base.DoQuery(cmd);
        }

    }

}
