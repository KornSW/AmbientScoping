Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices
Imports AmbientScoping

Public MustInherit Class ScopeProvider
  Implements ISingletonContainer
  Implements IDisposable

  Protected Sub New()
  End Sub

  Private _StateContainersPerDiscriminator As New ConcurrentDictionary(Of Object, IDictionary(Of String, Object))
  Private _SingletonContainersPerDiscriminator As New ConcurrentDictionary(Of Object, IDictionary(Of Type, Object))

  Protected MustOverride Function GetDiscriminator() As Object

  Protected Overridable Function SingletonContainerFactory(discriminator As Object) As IDictionary(Of Type, Object)
    Return New ConcurrentDictionary(Of Type, Object)
  End Function

  Protected Overridable Function StateContainerFactory(discriminator As Object) As IDictionary(Of String, Object)
    Return New ConcurrentDictionary(Of String, Object)
  End Function

  Public ReadOnly Property StateContainer As IDictionary(Of String, Object)
    Get
      Dim currentDiscriminator = Me.GetDiscriminator()
      Dim currentStateContainer As IDictionary(Of String, Object) = Nothing
      Do
        If (_StateContainersPerDiscriminator.TryGetValue(currentDiscriminator, currentStateContainer)) Then
          Return currentStateContainer
        End If
        currentStateContainer = Me.StateContainerFactory(currentDiscriminator)
        If (_StateContainersPerDiscriminator.TryAdd(currentDiscriminator, currentStateContainer)) Then
          OnNewStateContainerCreated(Me, currentDiscriminator, currentStateContainer)
        End If
      Loop
    End Get
  End Property

  Public Sub RestoreStateFrom(snapshotContainer As IDictionary(Of String, Object))

    'shutdown first because this will trigger that the states will be written to the statecontainer
    Me.TerminateSingletonInstances(Me)

    'no we can clear the state container
    Me.StateContainer.Clear()

    For Each k In snapshotContainer.Keys
      Me.StateContainer(k) = snapshotContainer(k)
    Next

  End Sub

  Public Sub SnapshotStateTo(snapshotContainer As IDictionary(Of String, Object))

    'force a sync (like on shutdown)
    For Each key In Me.Singletons.Keys.ToArray()
      Dim singletonInstance = Me.Singletons(key)
      If (TypeOf (singletonInstance) Is ISupportsStateSnapshot) Then
        DirectCast(singletonInstance, ISupportsStateSnapshot).CreateStateSnapshot(Sub(k, v) Me.StateContainer(k) = v)
      End If
    Next

    For Each k In Me.StateContainer.Keys
      snapshotContainer(k) = Me.StateContainer(k)
    Next

  End Sub

  Private ReadOnly Property Singletons As IDictionary(Of Type, Object) Implements ISingletonContainer.Singletons
    Get
      Dim currentDiscriminator = Me.GetDiscriminator()
      Dim currentSingletonContainer As IDictionary(Of Type, Object) = Nothing
      Do
        If (_SingletonContainersPerDiscriminator.TryGetValue(currentDiscriminator, currentSingletonContainer)) Then
          Return currentSingletonContainer
        End If
        currentSingletonContainer = Me.SingletonContainerFactory(currentDiscriminator)
        If (_SingletonContainersPerDiscriminator.TryAdd(currentDiscriminator, currentSingletonContainer)) Then
          OnNewSingletonContainerInitialized(Me, currentDiscriminator, currentSingletonContainer)
        End If
      Loop
    End Get
  End Property

  Public ReadOnly Property Instances As Object() Implements ISingletonContainer.Instances
    Get
      Throw New NotImplementedException()














    End Get
  End Property

  Public ReadOnly Property InstanceTypes As Type() Implements ISingletonContainer.InstanceTypes
    Get
      Throw New NotImplementedException()














    End Get
  End Property

  Protected Overridable Sub ShutdownScopeFor(discriminator As Object)
    Me.ShutdownSingletonContainerFor(discriminator)
    Me.ShutdownStateContainerFor(discriminator)
  End Sub

  Protected Sub ShutdownSingletonContainerFor(discriminator As Object)
    Dim singletonContainer As IDictionary(Of Type, Object) = Nothing
    If (_SingletonContainersPerDiscriminator.TryRemove(discriminator, singletonContainer)) Then
      OnSingletonContainerShutdown(Me, discriminator, singletonContainer)
    End If
  End Sub

  Protected Sub ShutdownStateContainerFor(discriminator As Object)
    Dim stateContainer As IDictionary(Of String, Object) = Nothing
    If (_StateContainersPerDiscriminator.TryRemove(discriminator, stateContainer)) Then
      OnStateContainerShutdown(Me, discriminator, stateContainer)
    End If
  End Sub

#Region " Init & Shutdown Subsciption (STATIC) "

  Private Shared Sub OnNewStateContainerCreated(scope As ScopeProvider, discriminator As Object, stateContainer As IDictionary(Of String, Object))







    'TODO jmd dranhängern der dann RestoreStateFrom aufruft









  End Sub

  Private Shared Sub OnStateContainerShutdown(scope As ScopeProvider, discriminator As Object, stateContainer As IDictionary(Of String, Object))






    'TODO    jmd dranhängern der dann SnapshotStateTo aufruft


    persisiterung möglich




  End Sub

  Public Shared _NewSingletonContainerInitializedSubscribers As New List(Of NewSingletonContainerInitializedSubscriberMethod)

  Private Shared Sub OnNewSingletonContainerInitialized(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    If (_NewSingletonContainerInitializedSubscribers.Any()) Then
      For Each s In _NewSingletonContainerInitializedSubscribers.ToArray()
        s.Invoke(scope, discriminator, singletonContainer)
      Next
    End If
  End Sub

  Public Delegate Sub NewSingletonContainerInitializedSubscriberMethod(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))

  Public Shared Sub SubscribeForNewSingletonContainerInitialized(subscriber As NewSingletonContainerInitializedSubscriberMethod)
    _NewSingletonContainerInitializedSubscribers.Add(subscriber)
  End Sub

  Public Shared _SingletonContainerShutdownSubscribers As New List(Of SingletonContainerShutdownSubscriberMethod)

  Private Shared Sub OnSingletonContainerShutdown(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    If (_SingletonContainerShutdownSubscribers.Any()) Then
      For Each s In _SingletonContainerShutdownSubscribers.ToArray()
        s.Invoke(scope, discriminator, singletonContainer)
      Next
    End If
  End Sub

  Public Delegate Sub SingletonContainerShutdownSubscriberMethod(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))

  Public Shared Sub SubscribeForSingletonContainerShutdown(subscriber As SingletonContainerShutdownSubscriberMethod)
    _SingletonContainerShutdownSubscribers.Add(subscriber)
  End Sub

#End Region

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then
        For Each discriminator In _SingletonContainersPerDiscriminator.Keys.ToArray()
          Me.ShutdownScopeFor(discriminator)
        Next
      End If
      _AlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

  Public Function TryGetInstance(Of TSingleton)(ByRef instance As TSingleton) As Boolean Implements ISingletonContainer.TryGetInstance
    Throw New NotImplementedException()
  End Function

  Public Sub SetFactory(Of TSingleton)(factory As Func(Of TSingleton)) Implements ISingletonContainer.SetFactory
    Throw New NotImplementedException()
  End Sub

  Public Function InitializeInstance(Of TSingleton)() As TSingleton Implements ISingletonContainer.InitializeInstance
    Throw New NotImplementedException()
  End Function

  Public Sub ResetSingleton(Of TSingleton)() Implements ISingletonContainer.ResetSingleton
    Throw New NotImplementedException()
  End Sub

  Public Sub SetResetMethod(Of TSingleton)(resetMethod As ISingletonContainer.StateResetMethod(Of TSingleton)) Implements ISingletonContainer.SetResetMethod
    Throw New NotImplementedException()
  End Sub

  Public Sub SetFlowableStateRecoveryMethod(Of TSingleton, TFlowableState)(resetMethod As ISingletonContainer.FlowableStateRecoveryMethod(Of TSingleton, TFlowableState)) Implements ISingletonContainer.SetFlowableStateRecoveryMethod
    Throw New NotImplementedException()
  End Sub

  Public Sub SetFlowableStateExtractorMethod(Of TSingleton, TFlowableState)(resetMethod As ISingletonContainer.FlowableStateExtractorMethod(Of TSingleton, TFlowableState)) Implements ISingletonContainer.SetFlowableStateExtractorMethod
    Throw New NotImplementedException()
  End Sub

  Public Sub ResetInstance(Of TSingleton)() Implements ISingletonContainer.ResetInstance
    Throw New NotImplementedException()
  End Sub

  Public Sub RecoverFlowableStateForInstance(Of TSingleton)(sourceSnapshotContainer As IDictionary(Of String, Object)) Implements ISingletonContainer.RecoverFlowableStateForInstance
    Throw New NotImplementedException()
  End Sub

  Public Sub ExtractorFlowableStateForInstance(Of TSingleton)(targetSnapshotContainer As IDictionary(Of String, Object)) Implements ISingletonContainer.ExtractorFlowableStateForInstance
    Throw New NotImplementedException()
  End Sub

  Public Sub TerminateSingletons(preserveFlowableStates As Boolean) Implements ISingletonContainer.TerminateSingletons
    'SyncLock singletonContainer
    '  For Each key In s.Keys.ToArray()
    '    Dim singletonInstance = singletonContainer(key)
    '    singletonContainer.Remove(key)

    '    If (TypeOf (singletonInstance) Is IDisposable) Then
    '      DirectCast(singletonInstance, IDisposable).Dispose() <<<<<<<<<<<<<<<<<<<<<<<<<<<<
    '    End If
    '  Next
    'End SyncLock
  End Sub

  Public Function InitializeNewInstance(Of TSingleton)() As TSingleton Implements ISingletonContainer.InitializeNewInstance
    Throw New NotImplementedException()
  End Function

#End Region

End Class

'Public NotInheritable Class ApplicationStateAcessor
'  Inherits ScopedStateAccessor

'#Region " Classic Singleton "

'  Private Shared _Current As ApplicationStateAcessor = Nothing

'  Public Shared ReadOnly Property Current As ApplicationStateAcessor
'    Get
'      If (_Current Is Nothing) Then
'        _Current = New ApplicationStateAcessor
'      End If
'      Return _Current
'    End Get
'  End Property

'#End Region

'  Protected Overrides Function GetDiscriminator() As Object
'    Return AppDomain.CurrentDomain.Id
'  End Function

'End Class

'Public NotInheritable Class TenantScope
'  Inherits ScopedStateAccessor

'#Region " Classic Singleton "

'  Private Shared _Current As TenantScope = Nothing

'  Public Shared ReadOnly Property Current As TenantScope
'    Get
'      If (_Current Is Nothing) Then
'        _Current = New TenantScope
'      End If
'      Return _Current
'    End Get
'  End Property

'#End Region
'  Private Sub New()
'    TenantBinding.SubscribeForConsumerLeavedFrom(
'      Sub(profileHandle As String, leavedConsumer As TenantBinding, wasLastOne As Boolean)
'        If (wasLastOne) Then
'          Me.OnLastTenantConsumerLeaved(profileHandle)
'        End If
'      End Sub
'    )
'  End Sub

'  Public Sub SwitchTenent(tenantIdentifier As String)
'    TenantBinding.Current.TenantIdentifier = tenantIdentifier
'  End Sub

'  Public Shared Sub ActivateTenant(tenantIdentifier As String)
'    TenantBinding.Current.TenantIdentifier = tenantIdentifier
'  End Sub

'  Protected Overrides Function GetDiscriminator() As Object
'    Return TenantBinding.Current
'  End Function

'  Protected Sub OnLastTenantConsumerLeaved(tenantIdentifier As String)
'    Me.ShutdownSingletonContainer(tenantIdentifier)
'  End Sub

'End Class

'Public NotInheritable Class OperationStateAccessor
'  Inherits ScopedStateAccessor

'#Region " Classic Singleton "

'  Private Shared _Current As OperationStateAccessor = Nothing

'  Public Shared ReadOnly Property GetInstance As OperationStateAccessor
'    Get
'      If (_Current Is Nothing) Then
'        _Current = New OperationStateAccessor
'      End If
'      Return _Current
'    End Get
'  End Property

'#End Region

'  Protected Overrides Function GetDiscriminator() As Object





'  End Function

'End Class

'Public Module ScopeExtensions

'  ''Quick Navigation from Scope to Context

'  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  'Public Function Context(scope As ApplicationScope) As ApplicationContext
'  '  'in real this is a convenience call for ApplicationScope.Current.Container(Gettype(ApplicationContext))
'  '  Return ApplicationContext.Current
'  'End Function

'  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  'Public Function Context(scope As TenantScope) As TenantContext
'  '  'in real this is a convenience call for TenantScope.Current.Container(Gettype(TenantContext))
'  '  Return TenantContext.Current
'  'End Function

'  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  'Public Function Context(scope As ProfileScope) As ProfileContext
'  '  'in real this is a convenience call for ProfileScope.Current.Container(Gettype(ProfileContext))
'  '  Return ProfileContext.Current
'  'End Function

'  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  'Public Function Context(scope As CallScope) As AmbientContext
'  '  'in real this is a convenience call for CallScope.Current.Container(Gettype(AmbientContext))
'  '  Return AmbientContext.Current
'  'End Function


'  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
'  Public Function ComponentDiscoveryClearances(profileScope As ProfileStateAccessor) As String()
'    Return ScopedSingleton.GetOrCreateInstance(Of ProfileState)(ProfileStateAccessor.GetInstance) _
'     .GetValue(Of String())(NameOf(ComponentDiscoveryClearances), {})
'  End Function

'End Module
