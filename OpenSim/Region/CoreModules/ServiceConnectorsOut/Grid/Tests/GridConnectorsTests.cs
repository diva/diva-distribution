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
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading;
using log4net.Config;
using NUnit.Framework;
using NUnit.Framework.SyntaxHelpers;
using OpenMetaverse;
using OpenSim.Framework;
using Nini.Config;

using OpenSim.Region.CoreModules.ServiceConnectorsOut.Grid;
using OpenSim.Region.Framework.Scenes;
using GridRegion = OpenSim.Services.Interfaces.GridRegion;
using OpenSim.Tests.Common;
using OpenSim.Tests.Common.Setup;

namespace OpenSim.Region.CoreModules.ServiceConnectorsOut.Grid.Tests
{
    [TestFixture]
    public class GridConnectorsTests
    {
        LocalGridServicesConnector m_LocalConnector;
        private void SetUp()
        {
            IConfigSource config = new IniConfigSource();
            config.AddConfig("Modules");
            config.AddConfig("GridService");
            config.Configs["Modules"].Set("GridServices", "LocalGridServicesConnector");
            config.Configs["GridService"].Set("LocalServiceModule", "OpenSim.Services.GridService.dll:GridService");
            config.Configs["GridService"].Set("StorageProvider", "OpenSim.Data.Null.dll:NullRegionData");

            m_LocalConnector = new LocalGridServicesConnector(config);
        }

        /// <summary>
        /// Test saving a V0.2 OpenSim Region Archive.
        /// </summary>
        [Test]
        public void TestRegisterRegionV0_2()
        {
            SetUp();

            // Create 3 regions
            GridRegion r1 = new GridRegion();
            r1.RegionName = "Test Region 1";
            r1.RegionID = new UUID(1);
            r1.RegionLocX = 1000 * (int)Constants.RegionSize;
            r1.RegionLocY = 1000 * (int)Constants.RegionSize;
            r1.ExternalHostName = "127.0.0.1";
            r1.HttpPort = 9001;
            r1.InternalEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("0.0.0.0"), 0);

            GridRegion r2 = new GridRegion();
            r2.RegionName = "Test Region 2";
            r2.RegionID = new UUID(2);
            r2.RegionLocX = 1001 * (int)Constants.RegionSize;
            r2.RegionLocY = 1000 * (int)Constants.RegionSize;
            r2.ExternalHostName = "127.0.0.1";
            r2.HttpPort = 9002;
            r2.InternalEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("0.0.0.0"), 0);

            GridRegion r3 = new GridRegion();
            r3.RegionName = "Test Region 3";
            r3.RegionID = new UUID(3);
            r3.RegionLocX = 1005 * (int)Constants.RegionSize;
            r3.RegionLocY = 1000 * (int)Constants.RegionSize;
            r3.ExternalHostName = "127.0.0.1";
            r3.HttpPort = 9003;
            r3.InternalEndPoint = new System.Net.IPEndPoint(System.Net.IPAddress.Parse("0.0.0.0"), 0);

            m_LocalConnector.RegisterRegion(UUID.Zero, r1);
            GridRegion result = m_LocalConnector.GetRegionByName(UUID.Zero, "Test");
            Assert.IsNotNull(result, "Retrieved GetRegionByName is null");
            Assert.That(result.RegionName, Is.EqualTo("Test Region 1"), "Retrieved region's name does not match");

            result = m_LocalConnector.GetRegionByUUID(UUID.Zero, new UUID(1));
            Assert.IsNotNull(result, "Retrieved GetRegionByUUID is null");
            Assert.That(result.RegionID, Is.EqualTo(new UUID(1)), "Retrieved region's UUID does not match");

            result = m_LocalConnector.GetRegionByPosition(UUID.Zero, 1000 * (int)Constants.RegionSize, 1000 * (int)Constants.RegionSize);
            Assert.IsNotNull(result, "Retrieved GetRegionByPosition is null");
            Assert.That(result.RegionLocX, Is.EqualTo(1000 * (int)Constants.RegionSize), "Retrieved region's position does not match");

            m_LocalConnector.RegisterRegion(UUID.Zero, r2);
            m_LocalConnector.RegisterRegion(UUID.Zero, r3);

            List<GridRegion> results = m_LocalConnector.GetNeighbours(UUID.Zero, new UUID(1));
            Assert.IsNotNull(results, "Retrieved neighbours list is null");
            Assert.That(results.Count, Is.EqualTo(1), "Retrieved neighbour collection is greater than expected");
            Assert.That(results[0].RegionID, Is.EqualTo(new UUID(2)), "Retrieved region's UUID does not match");

            results = m_LocalConnector.GetRegionsByName(UUID.Zero, "Test", 10);
            Assert.IsNotNull(results, "Retrieved GetRegionsByName list is null");
            Assert.That(results.Count, Is.EqualTo(3), "Retrieved neighbour collection is less than expected");

            results = m_LocalConnector.GetRegionRange(UUID.Zero, 900 * (int)Constants.RegionSize, 1002 * (int)Constants.RegionSize,
                900 * (int)Constants.RegionSize, 1100 * (int)Constants.RegionSize);
            Assert.IsNotNull(results, "Retrieved GetRegionRange list is null");
            Assert.That(results.Count, Is.EqualTo(2), "Retrieved neighbour collection is not the number expected");
        }
    }
}