using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Addins;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Wifi Robust Connector")]
[assembly: AssemblyDescription("Allows Wifi to run on a Robust server")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("Metaverse Ink")]
[assembly: AssemblyProduct("Diva Wifi")]
[assembly: AssemblyCopyright("Diva Canto")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("6b5ecdd8-3620-4e9f-87f3-9e1ce5bc4fd2")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion("0.8.1.*")]

[assembly: Addin("Diva.Wifi.Robust", OpenSim.VersionInfo.VersionNumber + Diva.Wifi.WebApp.WifiVersion, Url = "http://metaverseink.com", Category = "RobustPlugin")]
[assembly: AddinDependency("Robust", OpenSim.VersionInfo.VersionNumber)]
[assembly: AddinDescription("Diva Wifi, a Web application for OpenSim grids")]
[assembly: AddinAuthor("Diva Canto")]

[assembly: ImportAddinAssembly("Diva.Data.dll")]
[assembly: ImportAddinAssembly("Diva.Data.MySQL.dll")]
[assembly: ImportAddinAssembly("Diva.Interfaces.dll")]
[assembly: ImportAddinAssembly("Diva.OpenSimServices.dll")]
[assembly: ImportAddinAssembly("Diva.Utils.dll")]
[assembly: ImportAddinAssembly("Diva.Wifi.dll")]
[assembly: ImportAddinAssembly("Diva.Wifi.ScriptEngine.dll")]

[assembly: ImportAddinAssembly("de/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("en/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("es/Diva.Wifi.resources.dll")]
[assembly: ImportAddinAssembly("fr/Diva.Wifi.resources.dll")]

[assembly: ImportAddinFile("Wifi.ini")]

