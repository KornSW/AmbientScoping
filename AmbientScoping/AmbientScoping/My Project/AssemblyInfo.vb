﻿Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

<Assembly: AssemblyTitle("AmbientScoping")>
<Assembly: AssemblyDescription("AmbientScoping")>
<Assembly: AssemblyProduct("AmbientScoping")>

<Assembly: AssemblyTrademark("KornSW")>
<Assembly: AssemblyCompany("KornSW")>
<Assembly: AssemblyCopyright("KornSW")>

<Assembly: CLSCompliant(True)>
<Assembly: ComVisible(False)>
<Assembly: Guid("0e5337e3-7602-46c2-84ce-e1df6bab68f9")>

<Assembly: AssemblyVersion(Major + "." + Minor + "." + Fix + "." + BuildNumber)>
<Assembly: AssemblyInformationalVersion(Major + "." + Minor + "." + Fix + "-" + BuildType)>

Public Module SemanticVersion

  'increment this on breaking change:
  Public Const Major = "0"

  'increment this on new feature (w/o breaking change):
  Public Const Minor = "5"

  'increment this on internal fix (w/o breaking change):
  Public Const Fix = "0"

  'AND DONT FORGET TO UPDATE THE VERSION-INFO OF THE *.nuspec FILE!!!
#Region "..."

  'dont touch this, beacuse it will be replaced ONLY by the build process!!!

  Public Const BuildNumber = "*"
  Public Const BuildType = "LOCALBUILD"

#End Region
End Module
