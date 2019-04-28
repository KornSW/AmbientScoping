Imports System
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Runtime.CompilerServices

Public Module AmbientScopingExtensions

  Private _Instances As New Dictionary(Of Type, Object)

  <Extension()>
  Public Sub Activate(Of T)(ByRef target As T, Optional scope As Scope = Nothing)

    If (scope Is Nothing) Then
      scope = Scopes.Default
    End If

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

    'TODO: konstrucktor injections

  End Sub


  '<Extension()>
  'Public Function Profile(extendee As Scopes) As Scope
  '  Return extendee("Profile")
  'End Function

  '<Extension()>
  'Public Function AppDomain(extendee As Scopes) As Scope
  '  Return extendee("AppDomain")
  'End Function

End Module

'Public NotInheritable Class Scope

'  Friend Sub New(name As String)

'  End Sub

'End Class

'Public NotInheritable Class Scopes

'  Private Shared _ScopeRegistry As Scopes = Nothing

'  Public Shared ReadOnly Property [Default]() As Scope
'    Get
'      Return Ambient.AppDomain()
'    End Get
'  End Property

'  Public Shared ReadOnly Property Ambient As Scopes
'    Get
'      If (_ScopeRegistry Is Nothing) Then
'        _ScopeRegistry = New Scopes
'      End If
'      Return _ScopeRegistry
'    End Get
'  End Property
'  Private Sub New()
'  End Sub

'  Private _ScopeInstances As New Dictionary(Of String, Scope)

'  Default Public ReadOnly Property ByName(scopeName As String) As Scope
'    Get
'      SyncLock _ScopeInstances
'        Dim lowerScopeName = scopeName.ToLower()
'        If (_ScopeInstances.ContainsKey(lowerScopeName)) Then
'          Return _ScopeInstances(lowerScopeName)

'        Else
'          Dim newInst As New Scope(scopeName)
'          _ScopeInstances.Add(lowerScopeName, newInst)
'          Return newInst
'        End If


'      End SyncLock
'    End Get
'  End Property

'End Class

Public NotInheritable Class Activation

  Public Shared Function ForType(Of T)() As TypeActivationConfiguration(Of T)

  End Function

End Class

Public Class TypeActivationConfiguration

  Public Sub CreateInstancePerCall()

  End Sub

  Public Sub ShareOneInstancePerContextKey(contextKeyEvaluator As Action(Of String))

  End Sub

  Public Sub UseFactory(factory As Func(Of Object))

  End Sub

End Class

Public Class TypeActivationConfiguration(Of T)
  Inherits TypeActivationConfiguration

  Public Overloads Sub UseFactory(factory As Func(Of T))

  End Sub

End Class

Public Module TypeActivationConfigurationExtensions

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Sub ShareOneInstancePerThread(extendee As TypeActivationConfiguration)
    extendee.ShareOneInstancePerContextKey(Function() "Thread:" + Threading.Thread.CurrentThread.ManagedThreadId.ToString())
  End Sub

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Sub ShareOneInstancePerAppDomain(extendee As TypeActivationConfiguration)
    extendee.ShareOneInstancePerContextKey(Function() "AppDomain")
  End Sub

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Sub ShareOneInstancePerProfile(extendee As TypeActivationConfiguration)
    extendee.ShareOneInstancePerContextKey(Function() "Profile:" + ThreadMetaData.GetInstance().ProfileId)
  End Sub

End Module

Public NotInheritable Class ThreadMetaData
  Inherits Dictionary(Of String, Object)

#Region " Scoped Singleton "

  Shared Sub New()
    Activation.ForType(Of ThreadMetaData).ShareOneInstancePerThread()
    Activation.ForType(Of ThreadMetaData).UseFactory(Function() New ThreadMetaData)
  End Sub

  Public Shared Function GetInstance() As ThreadMetaData
    Dim instance As ThreadMetaData = Nothing
    instance.Activate()
    Return instance
  End Function

#End Region

  Private Sub New()
  End Sub

End Class

Public Module ThreadMetaDataExtensions

  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function ProfileId(extendee As ThreadMetaData) As String
    Return extendee(NameOf(ProfileId))?.ToString()
  End Function

End Module