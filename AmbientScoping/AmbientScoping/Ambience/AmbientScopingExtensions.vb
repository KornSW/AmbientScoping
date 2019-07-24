'Imports System
'Imports System.Collections.Generic
'Imports System.ComponentModel
'Imports System.Runtime.CompilerServices

'Public Module AmbientActions

'  ''' <summary>
'  ''' supplies an object for ambient access only from inside of the given closure 
'  ''' </summary>
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub DropAmbientInto(Of T)(objectToSupply As T, closure As Action)

'    Using AmbientScope.OpenClosureContext(Of T)(objectToSupply)

'      'Sub() objectToSupply.SupplyAmbient(AmbientScope.ClosureStack),
'      'Sub() objectToSupply.UnsupplyAmbient(AmbientScope.ClosureStack)

'      closure.Invoke()

'    End Using

'  End Sub

'  ''' <summary>
'  ''' supplies an object for ambient access only from inside of the given closure 
'  ''' </summary>
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub DropAmbientInto(itemKey As Object, objectToSupply As Object, closure As Action)

'    Using AmbientScope.OpenClosureContext(Of T)(objectToSupply)

'      'Sub() objectToSupply.SupplyAmbient(AmbientScope.ClosureStack),
'      'Sub() objectToSupply.UnsupplyAmbient(AmbientScope.ClosureStack)

'      closure.Invoke()

'    End Using

'  End Sub

'  ''' <summary>
'  ''' supplies an object for ambient access
'  ''' </summary>
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub SupplyGlobal(Of T)(objectToSupply As T, scope As Object) 'AmbientScope)

'    'nothing ist immer appdomain!!!

'    'GRasp

'    narrowdown


'  End Sub

'  ''' <summary>
'  ''' supplies an object for ambient access
'  ''' </summary>
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub SupplyGlobal(itemKey As Object, objectToSupply As Object, scope As AmbientScope)

'    'nothing ist immer appdomain!!!

'    'GRasp

'    narrowdown


'  End Sub

'  ''' <summary>
'  ''' supplies an object for ambient access
'  ''' </summary>
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub SupplyGlobal(Of T)(onDemandFactory As Func(Of T), scope As AmbientScope)

'    'nothing ist immer appdomain!!!

'    'GRasp

'    narrowdown


'  End Sub

'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub UnsupplyGlobal(Of T)(scope As AmbientScope)


'  End Sub

'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Sub UnsupplyGlobal(itemKey As Object, scope As AmbientScope)


'  End Sub







'  'GRASP
'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Function RetrieveAmbient(Of T)(ByRef objectToSupply As T) As Boolean

'    If (RetrieveAmbient(Of T)(objectToSupply, AmbientScope.ClosureStack)) Then
'      Return True
'    End If


'    'HIER STATTDESSTEN LISTE DER SCOPES DURCHLAUFEN NACH OBEN

'    Return RetrieveAmbient(Of T)(objectToSupply, AmbientScope.DefaultSingleton)

'  End Function

'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Function RetrieveAmbient(Of T)(ByRef objectToSupply As T, scope As AmbientScope) As Boolean
'    Return scope.TryRetrieve(Of T)(objectToSupply)
'  End Function

'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Function RetrieveAmbient(Of T)(ByRef objectToSupply As T, scope As AmbientScope, onDemandFactory As Func(Of T), registerAsSingleton As Boolean) As Boolean
'    Return scope.TryRetrieve(Of T)(objectToSupply, onDemandFactory, registerAsSingleton)
'  End Function

'End Module

'Public Class AmbientScope

''  Public Shared Function GetClosureContext(pushMethod As Action, popMethod As Action()) As IDisposable







''  End Function


''  Private Shared _DefaultSingleton As New AmbientScope(NameOf(System.AppDomain), Function() "GLOBAL")

''  Public Shared ReadOnly Property DefaultSingleton As AmbientScope
''    Get
''      Return _DefaultSingleton
''    End Get
''  End Property

''  Private Shared _ClosureStack As New AmbientScope(NameOf(System.AppDomain), Function() "TODO Closute.current!!!")

''  Public Shared ReadOnly Property ClosureStack As AmbientScope
''    Get
''      Return _ClosureStack
''    End Get
''  End Property




''  Public Sub New(name As String, discriminationKeyEvaluator As Func(Of Object))



''  End Sub

''  Public Property DiscriminationKeyEvaluator As Func(Of Object)

''  Public Sub Cleanup()

''  End Sub

''  Public Sub Cleanup(discriminationKey As Object)

''  End Sub

'End Class















''Public NotInheritable Class Scope

''  Friend Sub New(name As String)

''  End Sub

''End Class

''Public NotInheritable Class Scopes

''  Private Shared _ScopeRegistry As Scopes = Nothing

''  Public Shared ReadOnly Property [Default]() As Scope
''    Get
''      Return Ambient.AppDomain()
''    End Get
''  End Property

''  Public Shared ReadOnly Property Ambient As Scopes
''    Get
''      If (_ScopeRegistry Is Nothing) Then
''        _ScopeRegistry = New Scopes
''      End If
''      Return _ScopeRegistry
''    End Get
''  End Property
''  Private Sub New()
''  End Sub

''  Private _ScopeInstances As New Dictionary(Of String, Scope)

''  Default Public ReadOnly Property ByName(scopeName As String) As Scope
''    Get
''      SyncLock _ScopeInstances
''        Dim lowerScopeName = scopeName.ToLower()
''        If (_ScopeInstances.ContainsKey(lowerScopeName)) Then
''          Return _ScopeInstances(lowerScopeName)

''        Else
''          Dim newInst As New Scope(scopeName)
''          _ScopeInstances.Add(lowerScopeName, newInst)
''          Return newInst
''        End If


''      End SyncLock
''    End Get
''  End Property

''End Class

''Public NotInheritable Class Activation

''  Public Shared Function ForType(Of T)() As TypeActivationConfiguration(Of T)

''  End Function

''End Class

''Public Class TypeActivationConfiguration

''  Public Sub CreateInstancePerCall()

''  End Sub

''  Public Sub ShareOneInstancePerContextKey(contextKeyEvaluator As Action(Of String))

''  End Sub

''  Public Sub UseFactory(factory As Func(Of Object))

''  End Sub

''End Class

''Public Class TypeActivationConfiguration(Of T)
''  Inherits TypeActivationConfiguration

''  Public Overloads Sub UseFactory(factory As Func(Of T))

''  End Sub

''End Class

''Public Module TypeActivationConfigurationExtensions

''  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
''  Public Sub ShareOneInstancePerThread(extendee As TypeActivationConfiguration)
''    extendee.ShareOneInstancePerContextKey(Function() "Thread:" + Threading.Thread.CurrentThread.ManagedThreadId.ToString())
''  End Sub

''  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
''  Public Sub ShareOneInstancePerAppDomain(extendee As TypeActivationConfiguration)
''    extendee.ShareOneInstancePerContextKey(Function() "AppDomain")
''  End Sub

''  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
''  Public Sub ShareOneInstancePerProfile(extendee As TypeActivationConfiguration)
''    extendee.ShareOneInstancePerContextKey(Function() "Profile:" + ThreadMetaData.GetInstance().ProfileId)
''  End Sub

''End Module

''Public NotInheritable Class ThreadMetaData
''  Inherits Dictionary(Of String, Object)

''#Region " Scoped Singleton "

''  Shared Sub New()
''    Activation.ForType(Of ThreadMetaData).ShareOneInstancePerThread()
''    Activation.ForType(Of ThreadMetaData).UseFactory(Function() New ThreadMetaData)
''  End Sub

''  Public Shared Function GetInstance() As ThreadMetaData
''    Dim instance As ThreadMetaData = Nothing
''    instance.Activate()
''    Return instance
''  End Function

''#End Region

''  Private Sub New()
''  End Sub

''End Class

''Public Module ThreadMetaDataExtensions

''  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
''  Public Function ProfileId(extendee As ThreadMetaData) As String
''    Return extendee(NameOf(ProfileId))?.ToString()
''  End Function

''End Module