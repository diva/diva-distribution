using System;
using System.Reflection;

using Nini.Config;
using OpenMetaverse;
using log4net;

using OpenSim.Data;

using OpenSim.Services.Interfaces;
using OpenSim.Services.InventoryService;

namespace Diva.OpenSimServices
{
    public class InventoryService : OpenSim.Services.InventoryService.XInventoryService
    {
        public InventoryService(IConfigSource config)
            : base(config)
        {
        }

        public void DeleteUserInventory(UUID userID)
        {
            m_Database.DeleteFolders("agentID", userID.ToString());
            m_Database.DeleteItems("avatarID", userID.ToString());
        }
    }
}
