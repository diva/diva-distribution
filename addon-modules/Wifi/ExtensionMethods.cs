using System;
using System.Collections.Generic;

using OpenSim.Framework;
using OpenSim.Services.Interfaces;

namespace Diva.Wifi
{
    public static class ExtensionMethods
    {
        public static int GetX(this GridRegion gr)
        {
            return gr.RegionLocX / (int)Constants.RegionSize;
        }

        public static int GetY(this GridRegion gr)
        {
            return gr.RegionLocY / (int)Constants.RegionSize;
        }
    }
}
