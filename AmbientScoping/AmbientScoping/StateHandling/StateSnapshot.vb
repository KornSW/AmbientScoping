Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

'Friend Module Test

'  Sub Tester()



'    ProfileScope.Current.SwitchProfile("Profil1")



'    'UMDREHEN  Singleton VS MetadataDict-> immer ein dict mit KEY as string und darin ein dict nabens RuntimeContainer("Singletons")

'    '2 CONTAINER  
'    ' RuntimeContainer +PersistentContainer



'    ' ProfileScope.Current.GetOrCreateSingleton(Of T) '<<<<< extension

'    ' ProfileScope.Current.ComponentDiscoveryClearances  '<<<<< extension die dann ins haupt-dict geht


'    'ProfileScope.Current.
'    'ScopedSingleton.GetOrCreateInstance(Of MyTestservice)(ProfileScope.Current)





'    'Container.ShutdownSingletons()
'    'Container.ExportPersistentContainer

'    'Container.ImportPersistentContainer()
'    'Container.RecoverSingletons()
'    '+ event imported -> dann werden die singltons neu hochgefharen
'    'SINGELTONS MÜSSEN DEN CONTAINER NURTZEN


'    'ProfileScope.Current.AutoShudownIfUnsend = True
'    ' ProfileScope.OnAfterProfileInitialized(profileName, Container)   '<< globae hooks zum persistieren oder recoven(auch application und usescope)
'    'ProfileScope.OnBeforeProfileShutown(profileName, Container)

'    'AmbientContext.

'    ' CallScope.Current.

'  End Sub

'End Module
'Public Class MyTestservice

'End Class

Public Interface ISupportsStateSnapshot

  Sub CreateStateSnapshot(snapshotValueWriter As Action(Of String, Object))
  Sub RecoverStateSnapshot(snapshotValueReader As Func(Of String, Object))

End Interface
