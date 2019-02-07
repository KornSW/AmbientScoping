Imports System
Imports System.Collections.Generic

Public Module AmbientScopingExtensions

  Private _Instances As New Dictionary(Of Type, Object)

  Public Sub ApplyInstance(Of T)(ByRef target As T)
    Dim targeType As Type = GetType(T)
    SyncLock _Instances
      If (_Instances.ContainsKey(targeType)) Then
        target = DirectCast(_Instances(targeType), T)
        Exit Sub
      End If
    End SyncLock
    target = DirectCast(Activator.CreateInstance(targeType), T)
    SyncLock _Instances
      If (_Instances.ContainsKey(targeType)) Then
        target = DirectCast(_Instances(targeType), T)
      Else
        _Instances.Add(targeType, target)
      End If
    End SyncLock

  End Sub

End Module
