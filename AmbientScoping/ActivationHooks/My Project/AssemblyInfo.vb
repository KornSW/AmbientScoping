Imports System
Imports System.Reflection
Imports System.Runtime.InteropServices

<Assembly: AssemblyTitle("ActivationHooks")>
<Assembly: AssemblyDescription("ActivationHooks")>
<Assembly: AssemblyProduct("ActivationHooks")>

<Assembly: AssemblyTrademark("KornSW")>
<Assembly: AssemblyCompany("KornSW")>
<Assembly: AssemblyCopyright("KornSW")>

<Assembly: CLSCompliant(True)>
<Assembly: ComVisible(False)>
<Assembly: Guid("c8bc83b2-35f4-4084-a62b-a302bafadc80")>

<Assembly: AssemblyVersion(Major + "." + Minor + "." + Fix + "." + BuildNumber)>
<Assembly: AssemblyInformationalVersion(Major + "." + Minor + "." + Fix + "-" + BuildType)>

Public Module SemanticVersion

  'increment this on breaking change:
  Public Const Major = "3"

  'increment this on new feature (w/o breaking change):
  Public Const Minor = "0"

  'increment this on internal fix (w/o breaking change):
  Public Const Fix = "0"

  'AND DONT FORGET TO UPDATE THE VERSION-INFO OF THE *.nuspec FILE!!!
#Region "..."

  'dont touch this, beacuse it will be replaced ONLY by the build process!!!

  Public Const BuildNumber = "*"
  Public Const BuildType = "LOCALBUILD"

#End Region
End Module
