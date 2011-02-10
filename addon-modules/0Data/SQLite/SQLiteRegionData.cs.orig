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
using System.Linq;

using OpenMetaverse;
using OpenSim.Data;

namespace Diva.Data.SQLite
{
    public class SQLiteRegionData : OpenSim.Data.Null.NullRegionData, IRegionData
    {
        public SQLiteRegionData(string connectionString, string realm) 
            : base(connectionString, realm) { }

        public RegionData[] Get(UUID scopeID, int regionFlags, int excludeFlags)
        {
            return GetList(scopeID, regionFlags, excludeFlags).ToArray();
        }

        public long GetCount(UUID scopeID, int regionFlags, int excludeFlags)
        {
            return GetList(scopeID, regionFlags, excludeFlags).Count;
        }

        private List<RegionData> GetList(UUID scopeID, int regionFlags, int excludeFlags)
        {
            List<RegionData> regions = Get("%", scopeID);
            if (regions == null)
                regions = new List<RegionData>();

            int index = 0;
            while (index < regions.Count)
            {
                int flags = Convert.ToInt32(regions.ElementAt(index).Data["flags"]);
                if (((flags & regionFlags) != 0) && ((flags & excludeFlags) == 0))
                    ++index;
                else
                    regions.RemoveAt(index);
            }
            return regions;
        }
    }
}
