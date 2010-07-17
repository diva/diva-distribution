using System;
using System.Collections.Generic;
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

        public InventoryTreeNode GetInventoryTree(UUID userID)
        {
            XInventoryFolder[] folders = m_Database.GetFolders(new string[] { "agentID" }, new string[] { userID.ToString() });
            XInventoryItem[] items = m_Database.GetItems(new string[] { "avatarID" }, new string[] { userID.ToString() });

            List<XInventoryFolder> folderList = null;
            if (folders != null)
                folderList = new List<XInventoryFolder>(folders);
            else
                folderList = new List<XInventoryFolder>();

            List<XInventoryItem> itemList = null;
            if (items != null)
                itemList = new List<XInventoryItem>(items);
            else
                itemList = new List<XInventoryItem>();

            InventoryTreeNode root = new InventoryTreeNode(UUID.Zero, "root", AssetType.Unknown, -1, true);
            FillIn(root, folderList, itemList);

            Dump(root, string.Empty);
            return root;
            
        }

        private void FillIn(InventoryTreeNode root, List<XInventoryFolder> folders, List<XInventoryItem> items)
        {
            List<XInventoryItem> childrenItems = items.FindAll(delegate(XInventoryItem i) { return i.parentFolderID == root.ID; });
            List<XInventoryFolder> childrenFolders = folders.FindAll(delegate(XInventoryFolder f) { return f.parentFolderID == root.ID; });

            if (childrenItems != null)
                foreach (XInventoryItem it in childrenItems)
                {
                    root.Children.Add(new InventoryTreeNode(it.inventoryID, it.inventoryName, (AssetType)it.invType, root.Depth + 1, false));
                    items.Remove(it);
                }
            if (childrenFolders != null)
                foreach (XInventoryFolder fo in childrenFolders)
                {
                    InventoryTreeNode node = new InventoryTreeNode(fo.folderID, fo.folderName, (AssetType)fo.type, root.Depth + 1, true);
                    root.Children.Add(node);
                    folders.Remove(fo);
                    // Recurse
                    FillIn(node, folders, items);
                }
        }

        private void Dump(InventoryTreeNode node, string _ident)
        {
            Console.WriteLine(_ident + node.Name);
            if (node.Children != null)
            {
                foreach (InventoryTreeNode n in node.Children)
                    Dump(n, _ident + "\t");
            }
        }
    }

    public class InventoryTreeNode
    {
        public UUID ID;
        public string Name;
        public AssetType Type;
        public int Depth;
        // null means item; non-null means folder
        public List<InventoryTreeNode> Children;

        public InventoryTreeNode(UUID _id, string _name, AssetType _type, int _depth, bool isFolder)
        {
            ID = _id;
            Name = _name;
            Type = _type;
            Depth = _depth;
            if (isFolder)
                Children = new List<InventoryTreeNode>();
        }

        public bool IsFolder()
        {
            return !(Children == null);
        }

        public override string ToString()
        {
            return "Name: " + Name;
        }
    }
}
