using System;
using System.Collections.Generic;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

using Diva.Wifi;
using Diva.OpenSimServices;
using Diva.Wifi.WifiScript;

namespace Diva.Wifi
{
    public static class ExtensionMethods
    {
        public static string GetX(this GridRegion gr, IEnvironment env)
        {
            int result = gr.RegionLocX / (int)Constants.RegionSize;
            return result.ToString();
        }

        public static string GetY(this GridRegion gr, IEnvironment env)
        {
            int result = gr.RegionLocY / (int)Constants.RegionSize;
            return result.ToString();
        }

        public static string GetInventoryChildren(this InventoryTreeNode node, IEnvironment env)
        {
            //Console.WriteLine(" GetInventoryChildren --> " + node.Name);
            if (node.Children == null)
                return string.Empty;
        
            // else it's a folder
            string invListStr = string.Empty;
            Environment env2 = (Environment)env;
            //Console.WriteLine(" GetInventoryChildren --> child count " + node.Children.Count);
            foreach (InventoryTreeNode child in node.Children)
            {
                // Create a new environment
                Environment newEnv = new Environment(env2.Request);
                newEnv.Flags = env2.Flags;
                newEnv.Session = env2.Session;
                newEnv.State = env2.State;
                newEnv.Data = new List<object>();
                newEnv.Data.Add(child);

                //Console.WriteLine("-------- " + child.Name + " ------");
                invListStr += WebApp.WebAppInstance.ReadFile(newEnv, "inventorylistitem.html");
            }

            return invListStr;
        }

        public static string Indent(this InventoryTreeNode node, IEnvironment env)
        {
            string indent = string.Empty;
            for (int i = 0; i < node.Depth; i++)
                indent += "&nbsp;&nbsp;&nbsp;&nbsp;";

            if (node.Children != null)
                indent += "&raquo; ";
            return indent;
        }

        public static string GetFolders(this InventoryTreeNode node, IEnvironment env)
        {
            Environment env2 = (Environment)env;
            if (env2.Data == null || env2.Data.Count == 0)
                return string.Empty;

            string result = string.Empty;
            foreach (object obj in env2.Data)
            {
                try
                {
                    InventoryTreeNode n = (InventoryTreeNode)obj;
                    if (n.Children != null) // it's a folder
                    {
                        // first node is the very top root, UUID.Zero
                        foreach (InventoryTreeNode child in n.Children)
                        {
                            string name = child.Name;
                            if (name.Length > 40)
                                name = name.Substring(0, 40);
                            result += "<option value=\"" + child.ID + "\">" + name + "</option>\n";
                            foreach (InventoryTreeNode gchild in child.Children)
                            {
                                name = child.Name + "/" + gchild.Name;
                                if (name.Length > 40)
                                    name = name.Substring(0, 40);
                                result += "<option value=\"" + gchild.ID + "\">" + name + "</option>\n";
                            }
                        }
                    }
                }
                catch { /* */ }
            }

            return result;
        }

    }
}
