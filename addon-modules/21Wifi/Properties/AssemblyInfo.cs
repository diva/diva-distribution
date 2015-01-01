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
[assembly: AssemblyCompany("")]
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
[assembly: AssemblyVersion("0.8.1.*")]

[assembly: Addin("Diva.Wifi(Robust)", OpenSim.VersionInfo.VersionNumber + Diva.Wifi.WebApp.WifiVersion, Url = "http://metaverseink.com", Category = "RobustPlugin")]
[assembly: AddinDependency("Robust", OpenSim.VersionInfo.VersionNumber)]
[assembly: AddinDescription("Diva Wifi, a Web application for OpenSim grids")]
[assembly: AddinAuthor("Diva Canto")]
[assembly: AssemblyCopyright("Copyright (C) Diva Canto")]
