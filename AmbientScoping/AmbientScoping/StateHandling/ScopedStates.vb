Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public MustInherit Class ContextualStateContainer
  Inherits ConcurrentDictionary(Of String, Object)

  Public Function GetValue(Of TValue)(key As String, Optional [default] As TValue = Nothing) As TValue
    If (Me.ContainsKey(key)) Then
      Dim value As Object = Me(key)
      If (value Is Nothing) Then
        Return Nothing
      ElseIf (TypeOf (value) Is TValue) Then
        Return DirectCast(value, TValue)
      End If
    Else
      Return [default]
    End If
  End Function

  Public Sub RemoveAndDisposeValue(key As String)
    If (Me.ContainsKey(key)) Then
      Dim value As Object = Nothing
      If (Me.TryRemove(key, value)) Then
        If (TypeOf (value) Is IDisposable) Then
          DirectCast(value, IDisposable).Dispose()
        End If
      End If
    End If
  End Sub

End Class

''' <summary> stores all contextual or environment-related metadata scoped to the current application</summary>
Public NotInheritable Class ApplicationState
  Inherits ContextualStateContainer

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As ApplicationState
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of ApplicationState)(ApplicationScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or environment-related metadata scoped to the current profile</summary>
Public NotInheritable Class ProfileState
  Inherits ContextualStateContainer

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As ProfileState
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of ProfileState)(ProfileScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or environment-related metadata scoped to the current tenant</summary>
Public NotInheritable Class TenantState
  Inherits ContextualStateContainer

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As TenantState
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of TenantState)(TenantScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or ambient parameters scoped to the current call</summary>
Public NotInheritable Class OperationState
  Inherits ContextualStateContainer

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As OperationState
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of OperationState)(OperationScope.Current)
    End Get
  End Property

#End Region

End Class


''' <summary> stores all contextual or ambient parameters scoped to the current call</summary>
Public NotInheritable Class AmbientParameters
  Inherits ContextualStateContainer

  Public Shared ReadOnly Property Current As AmbientParameters
    Get

      ' kein singleton   asynclovcal + tread


    End Get
  End Property

End Class
