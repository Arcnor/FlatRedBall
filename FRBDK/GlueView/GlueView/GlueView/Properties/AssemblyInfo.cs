﻿using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

// General Information about an assembly is controlled through the following 
// set of attributes. Change these attribute values to modify the information
// associated with an assembly.
[assembly: AssemblyTitle("GlueView")]
[assembly: AssemblyProduct("GlueView")]
[assembly: AssemblyDescription("")]
[assembly: AssemblyCompany("FlatRedBall")]

[assembly: AssemblyCopyright("Copyright © FlatRedBall 2009")]
[assembly: AssemblyTrademark("")]
[assembly: AssemblyCulture("")]

// Setting ComVisible to false makes the types in this assembly not visible 
// to COM components.  If you need to access a type in this assembly from 
// COM, set the ComVisible attribute to true on that type.
[assembly: ComVisible(false)]

// The following GUID is for the ID of the typelib if this project is exposed to COM
[assembly: Guid("8b523e4f-fef6-4d3d-ab80-fcdb494f64d3")]


// Version information for an assembly consists of the following four values:
//
//      Major Version
//      Minor Version 
//      Build Number
//      Revision
//
// 1.1.1 fixes sprites not deselecting when Glue user selects a folder node like "Objects"
// 1.2.0.0
//  - Adds Drag+move objects in Screens/Entities
//  - Re-adds selection back in GlueView to go back to Glue
//  - Adds camera bounds
//  - Moves camera and localization to plugins
//  - GlueView now remembers collapsed states.
// 1.2.1.0
//  - Cursor selection now prioritizes already-selected objects.
// 1.2.1.1
//  - More MSBuild reference fixes
[assembly: AssemblyVersion("1.2.1.1")]
