using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Mono.Addins;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Diva.Interfaces")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Diva Distribution")]
[assembly: AssemblyCopyright("Diva Canto aka Crista Lopes")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("e1d1d2f6-4928-4ac7-babb-3d443aceb6c8")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly : AssemblyVersion(Diva.Interfaces.Info.AssemblyNumber)]

[assembly: Addin("Diva.Interfaces", OpenSim.VersionInfo.VersionNumber + "." + Diva.Interfaces.Info.VersionNumber)]
[assembly: AddinDescription("Extension point for Wifi addons")]
[assembly: AddinAuthor("Diva Canto")]
