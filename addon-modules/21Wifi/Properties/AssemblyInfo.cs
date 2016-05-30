using System.IO;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Addins;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Diva.Wifi")]
[assembly: AssemblyDescription("Diva's Web Interface for OpenSim")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Metaverse Ink")]
[assembly: AssemblyProduct("Diva Distribution")]
[assembly: AssemblyCopyright("Crista Lopes aka Diva Canto, and contributors")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e9d1bda2-a1f9-42b5-beb0-9ef5e144eff2")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly : AssemblyVersion(Diva.Interfaces.Info.AssemblyNumber)]

[assembly: Addin("Diva.Wifi", OpenSim.VersionInfo.VersionNumber + "." + Diva.Wifi.Info.VersionNumber, Url = "http://metaverseink.com", Category = "RobustPlugin")]
[assembly: AddinDependency("Diva.Interfaces", OpenSim.VersionInfo.VersionNumber + "." + Diva.Interfaces.Info.VersionNumber)]
[assembly: AddinDependency("Robust", OpenSim.VersionInfo.VersionNumber)]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]

[assembly: AddinDescription("Diva Wifi, a Web application for OpenSim grids")]
[assembly: AddinAuthor("Diva Canto")]

[assembly: ImportAddinAssembly("Diva.Data.dll")]
[assembly: ImportAddinAssembly("Diva.Data.MySQL.dll")]
[assembly: ImportAddinAssembly("Diva.OpenSimServices.dll")]
[assembly: ImportAddinAssembly("Diva.Utils.dll")]
[assembly: ImportAddinAssembly("Diva.Wifi.ScriptEngine.dll")]

[assembly: ImportAddinAssembly("de/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("en/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("es/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("fr/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("pt/Diva.Wifi.resources.dll")]

[assembly: ImportAddinFile("Wifi.ini")]
[assembly: ImportAddinFile("Diva.Wifi.pot")]

namespace Diva.Wifi
{
    class Info
    {
        public const string VersionNumber = "13";

        public static string AssemblyDirectory
        {
            get
            {
                string location = Assembly.GetExecutingAssembly().Location;
                return Path.GetDirectoryName(location);
            }
        }

    }
}