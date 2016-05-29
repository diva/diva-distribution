using System.Reflection;
using System.Runtime.InteropServices;
using Mono.Addins;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("Diva.AddinExample")]
[assembly: AssemblyDescription("Example of how to develop OpenSim addins")]
[assembly: AssemblyConfiguration("")]
[assembly: AssemblyCompany("")]
[assembly: AssemblyProduct("Diva.AddinExample.Properties")]
[assembly: AssemblyCopyright("Diva Canto")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("a915261b-15fc-4107-b65d-58ad6640f128")]

// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
[assembly: AssemblyVersion(OpenSim.VersionInfo.AssemblyVersionNumber)]

[assembly: Addin("Diva.AddinExample", OpenSim.VersionInfo.VersionNumber + ".2")]
[assembly: AddinDependency("OpenSim.Region.Framework", OpenSim.VersionInfo.VersionNumber)]
[assembly: AddinDescription("Example of how to create OpenSim Addins")]
[assembly: AddinAuthor("Diva Canto")]

[assembly: ImportAddinAssembly("CsvHelper.dll")]
[assembly: ImportAddinFile("AddinExample.ini")]
[assembly: ImportAddinFile("AddinExample.html")]
