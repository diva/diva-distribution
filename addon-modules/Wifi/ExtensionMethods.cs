using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using OpenSim.Services.Interfaces;


namespace Diva.Wifi
{
    public static class ExtensionMethods
    {
        public static int GetX(this GridRegion gr)
        {
            return gr.RegionLocX / 256;
        }

        public static int GetY(this GridRegion gr)
        {
            return gr.RegionLocY / 256;
        }
    }
}
