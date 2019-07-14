Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Friend Module Test

  Sub Tester()



    ProfileScope.Current.SwitchProfile("Profil1")



    'UMDREHEN  Singleton VS MetadataDict-> immer ein dict mit KEY as string und darin ein dict nabens RuntimeContainer("Singletons")

    '2 CONTAINER  
    ' RuntimeContainer +PersistentContainer



    ' ProfileScope.Current.GetOrCreateSingleton(Of T) '<<<<< extension

    ' ProfileScope.Current.ComponentDiscoveryClearances  '<<<<< extension die dann ins haupt-dict geht


    'ProfileScope.Current.
    'ScopedSingleton.GetOrCreateInstance(Of MyTestservice)(ProfileScope.Current)





    'Container.ShutdownSingletons()
    'Container.ExportPersistentContainer

    'Container.ImportPersistentContainer()
    'Container.RecoverSingletons()
    '+ event imported -> dann werden die singltons neu hochgefharen
    'SINGELTONS MÜSSEN DEN CONTAINER NURTZEN


    'ProfileScope.Current.AutoShudownIfUnsend = True
    ' ProfileScope.OnAfterProfileInitialized(profileName, Container)   '<< globae hooks zum persistieren oder recoven(auch application und usescope)
    'ProfileScope.OnBeforeProfileShutown(profileName, Container)

    'AmbientContext.

    ' CallScope.Current.

  End Sub

End Module
Public Class MyTestservice

End Class

Public Interface ISupportsStateSnapshot

  Sub CreateStateSnapshot(snapshotValueWriter As Action(Of String, Object))
  Sub RecoverStateSnapshot(snapshotValueReader As Func(Of String, Object))

End Interface

#Region " Scope Levels "

Friend Interface ISingletonContainer
  ReadOnly Property Singletons As IDictionary(Of Type, Object)

End Interface

Public MustInherit Class Scope
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
    ScopedSingleton.TerminateSingletonInstances(Me)

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

  Protected Sub ShutdownScope(discriminator As Object)
    Me.ShutdownSingletonContainer(discriminator)
    Me.ShutdownScopeContainer(discriminator)
  End Sub

  Protected Sub ShutdownSingletonContainer(discriminator As Object)
    Dim singletonContainer As IDictionary(Of Type, Object) = Nothing
    If (_SingletonContainersPerDiscriminator.TryRemove(discriminator, singletonContainer)) Then
      OnSingletonContainerShutdown(Me, discriminator, singletonContainer)
    End If
  End Sub

  Protected Sub ShutdownScopeContainer(discriminator As Object)
    Dim stateContainer As IDictionary(Of String, Object) = Nothing
    If (_StateContainersPerDiscriminator.TryRemove(discriminator, stateContainer)) Then
      OnStateContainerShutdown(Me, discriminator, stateContainer)
    End If
  End Sub

#Region " Init & Shutdown Subsciption (STATIC) "

  Private Shared Sub OnNewStateContainerCreated(scope As Scope, discriminator As Object, stateContainer As IDictionary(Of String, Object))

    'TODO jmd dranhängern der dann RestoreStateFrom aufruft

  End Sub

  Private Shared Sub OnStateContainerShutdown(scope As Scope, discriminator As Object, stateContainer As IDictionary(Of String, Object))

    'TODO jmd dranhängern der dann SnapshotStateTo aufruft

  End Sub

  Public Shared _NewSingletonContainerInitializedSubscribers As New List(Of NewSingletonContainerInitializedSubscriberMethod)
  Private Shared Sub OnNewSingletonContainerInitialized(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    If (_NewSingletonContainerInitializedSubscribers.Any()) Then
      For Each s In _NewSingletonContainerInitializedSubscribers.ToArray()
        s.Invoke(scope, discriminator, singletonContainer)
      Next
    End If
  End Sub

  Public Delegate Sub NewSingletonContainerInitializedSubscriberMethod(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
  Public Shared Sub SubscribeForNewSingletonContainerInitialized(subscriber As NewSingletonContainerInitializedSubscriberMethod)
    _NewSingletonContainerInitializedSubscribers.Add(subscriber)
  End Sub

  Public Shared _SingletonContainerShutdownSubscribers As New List(Of SingletonContainerShutdownSubscriberMethod)
  Private Shared Sub OnSingletonContainerShutdown(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    If (_SingletonContainerShutdownSubscribers.Any()) Then
      For Each s In _SingletonContainerShutdownSubscribers.ToArray()
        s.Invoke(scope, discriminator, singletonContainer)
      Next
    End If
  End Sub
  Public Delegate Sub SingletonContainerShutdownSubscriberMethod(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
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
          Me.ShutdownScope(discriminator)
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

#End Region

End Class

Public NotInheritable Class ApplicationScope
  Inherits Scope

#Region " Classic Singleton "

  Private Shared _Current As ApplicationScope = Nothing

  Public Shared ReadOnly Property Current As ApplicationScope
    Get
      If (_Current Is Nothing) Then
        _Current = New ApplicationScope
      End If
      Return _Current
    End Get
  End Property

#End Region

  Protected Overrides Function GetDiscriminator() As Object
    Return AppDomain.CurrentDomain.Id
  End Function

End Class

'Public NotInheritable Class TenantScope
'  Inherits Scope

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
'    TenantConsumer.SubscribeForConsumerLeavedFrom(
'      Sub(profileHandle As String, leavedConsumer As TenantConsumer, wasLastOne As Boolean)
'        If (wasLastOne) Then
'          Me.OnLastTenantConsumerLeaved(profileHandle)
'        End If
'      End Sub
'    )
'  End Sub

'  Protected Overrides Function GetDiscriminator() As Object
'    Return AmbientContext.Current.BoundTenantIdentifier
'  End Function

'  Protected Sub OnLastTenantConsumerLeaved(tenantIdentifier As String)
'    Me.ShutdownSingletonContainer(tenantIdentifier)
'  End Sub

'End Class

Public NotInheritable Class ProfileScope
  Inherits Scope

#Region " Classic Singleton "

  Private Shared _Current As ProfileScope = Nothing

  Public Shared ReadOnly Property Current As ProfileScope
    Get
      If (String.IsNullOrWhiteSpace(ProfileBinding.Current.ProfileName)) Then
        Return Nothing
      End If
      If (_Current Is Nothing) Then
        _Current = New ProfileScope
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    ProfileBinding.SubscribeForConsumerLeavedFrom(
      Sub(profileName As String, leavedConsumer As ProfileBinding, wasLastOne As Boolean)
        If (wasLastOne) Then
          Me.OnLastProfileConsumerLeaved(profileName)
        End If
      End Sub
    )
  End Sub

  Protected Sub OnLastProfileConsumerLeaved(profileName As String)
    Me.ShutdownSingletonContainer(profileName)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return ProfileBinding.Current
  End Function

  Public Sub SwitchProfile(profileName As String)
    ProfileBinding.Current.ProfileName = profileName
  End Sub

  Public Shared Sub ActivateProfile(profileName As String)
    ProfileBinding.Current.ProfileName = profileName
  End Sub

  Public ReadOnly Property ProfileName As String
    Get
      Return ProfileBinding.Current.ProfileName
    End Get
  End Property

End Class

Public NotInheritable Class CallScope
  Inherits Scope

#Region " Classic Singleton "

  Private Shared _Current As CallScope = Nothing

  Public Shared ReadOnly Property Current As CallScope
    Get
      If (_Current Is Nothing) Then
        _Current = New CallScope
      End If
      Return _Current
    End Get
  End Property

#End Region

  Protected Overrides Function GetDiscriminator() As Object
    'Return 'TODO ASYNCLOCAL
  End Function

End Class

Public Module ScopeExtensions

  ''Quick Navigation from Scope to Context

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function Context(scope As ApplicationScope) As ApplicationContext
  '  'in real this is a convenience call for ApplicationScope.Current.Container(Gettype(ApplicationContext))
  '  Return ApplicationContext.Current
  'End Function

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function Context(scope As TenantScope) As TenantContext
  '  'in real this is a convenience call for TenantScope.Current.Container(Gettype(TenantContext))
  '  Return TenantContext.Current
  'End Function

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function Context(scope As ProfileScope) As ProfileContext
  '  'in real this is a convenience call for ProfileScope.Current.Container(Gettype(ProfileContext))
  '  Return ProfileContext.Current
  'End Function

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function Context(scope As CallScope) As AmbientContext
  '  'in real this is a convenience call for CallScope.Current.Container(Gettype(AmbientContext))
  '  Return AmbientContext.Current
  'End Function


  <Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  Public Function ComponentDiscoveryClearances(profileScope As ProfileScope) As String()
    Return ScopedSingleton.GetOrCreateInstance(Of ProfileContext)(ProfileScope.Current) _
     .GetValue(Of String())(NameOf(ComponentDiscoveryClearances), {})
  End Function

End Module

#End Region

#Region " Singlton "

Public Class ScopedSingleton

  'TODO: in activationhooks registrieren

  Shared Sub New()
    Scope.SubscribeForNewSingletonContainerInitialized(AddressOf OnContainerInitialize)
    Scope.SubscribeForSingletonContainerShutdown(AddressOf OnContainerShutdown)
  End Sub

  Private Shared Sub OnContainerInitialize(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
  End Sub

  'TODO autodispose via subscription on Scope.sysbcribeforshutdown

  Public Shared Function GetOrCreateInstance(Of T As New)(scope As Scope) As T
    Return GetOrCreateInstance(Of T)(scope, Function() New T)
  End Function

  Public Shared Function GetOrCreateInstance(Of T)(scope As Scope, factory As Func(Of T)) As T

    Dim singletonContainer = DirectCast(scope, ISingletonContainer).Singletons

    If (singletonContainer.ContainsKey(GetType(T))) Then
      Return DirectCast(singletonContainer(GetType(T)), T)
    End If

    Dim newInstance As T = factory.Invoke()
    singletonContainer.Add(GetType(T), newInstance)

    If (TypeOf (newInstance) Is ISupportsStateSnapshot) Then
      DirectCast(newInstance, ISupportsStateSnapshot).RecoverStateSnapshot(Function(key As String) scope.StateContainer(key))
    End If

    Return newInstance
  End Function

  Private Shared Sub OnContainerShutdown(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    TerminateSingletonInstances(scope, singletonContainer)
  End Sub

  Public Shared Sub TerminateSingletonInstances(scope As Scope)
    Dim singletonContainer = DirectCast(scope, ISingletonContainer).Singletons
    TerminateSingletonInstances(scope, singletonContainer)
  End Sub

  Private Shared Sub TerminateSingletonInstances(scope As Scope, singletonContainer As IDictionary(Of Type, Object))
    For Each key In singletonContainer.Keys.ToArray()
      Dim singletonInstance = singletonContainer(key)
      singletonContainer.Remove(key)

      If (TypeOf (singletonInstance) Is ISupportsStateSnapshot) Then
        DirectCast(singletonInstance, ISupportsStateSnapshot).CreateStateSnapshot(Sub(k, v) scope.StateContainer(k) = v)
      End If

      If (TypeOf (singletonInstance) Is IDisposable) Then
        DirectCast(singletonInstance, IDisposable).Dispose()
      End If

    Next
  End Sub

End Class

#End Region

#Region " LifetimeHandle "

Public Class ProfileBinding
  Implements IDisposable

  Public Shared Sub InvokeUnder(profileName As String, target As Action)
    Dim ambientInstance = Current
    Dim oldProfileName = ambientInstance.ProfileName
    ambientInstance.ProfileName = profileName
    Try
      target.Invoke()
    Finally
      ambientInstance.ProfileName = oldProfileName
    End Try
  End Sub

  Public Shared ReadOnly Property Current As ProfileBinding
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of ProfileBinding)(CallScope.Current, Function() New ProfileBinding)
    End Get
  End Property

  Private Sub New()

  End Sub

  Public Property ProfileName As String
    Get



    End Get
    Set(value As String)

      'hier leave und join (nur falls änderung) 
      'notinh oder leerstring ist nur leave!!

    End Set
  End Property

  Public Shared ReadOnly Property ConsumersOn(profileName As String) As ProfileBinding()
    Get

    End Get
  End Property

  Public Shared Function HasConsumersOn(profileName As String) As Boolean

  End Function

  Public Delegate Sub ConsumerJoinedSubsciber(profileName As String, joinedConsumer As ProfileBinding, isFirstOne As Boolean)
  Public Shared Sub SubscribeForConsumerJoinedTo(subscriber As ConsumerJoinedSubsciber)

  End Sub

  Public Delegate Sub ConsumerLeavedSubsciber(profileName As String, leavedConsumer As ProfileBinding, wasLastOne As Boolean)
  Public Shared Sub SubscribeForConsumerLeavedFrom(subscriber As ConsumerLeavedSubsciber)
    'nötig um den kompletten context der sharedsingletons u disposen
  End Sub

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then
        Me.ProfileName = Nothing 'WICHTIG!!!!
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

#End Region

End Class

#End Region





#Region " Contexts (Metadata-Dictionaries) "
Public Module ContextExtensions

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function BoundProfileHandle(context As AmbientContext) As String
  '  Return context.GetValue(NameOf(BoundProfileHandle), String.Empty)
  'End Function

  '<Extension(), EditorBrowsable(EditorBrowsableState.Always)>
  'Public Function BoundTenantIdentifier(context As AmbientContext) As String
  '  Return context.GetValue(NameOf(BoundTenantIdentifier), String.Empty)
  'End Function

End Module

Public MustInherit Class ContextBase
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
Public NotInheritable Class ApplicationContext
  Inherits ContextBase

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As ApplicationContext
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of ApplicationContext)(ApplicationScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or environment-related metadata scoped to the current profile</summary>
Public NotInheritable Class ProfileContext
  Inherits ContextBase

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As ProfileContext
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of ProfileContext)(ProfileScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or environment-related metadata scoped to the current tenant</summary>
Public NotInheritable Class TenantContext
  Inherits ContextBase

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As TenantContext
    Get
      'Return ScopedSingleton.GetOrCreateInstance(Of TenantContext)(TenantScope.Current)
    End Get
  End Property

#End Region

End Class

''' <summary> stores all contextual or ambient parameters scoped to the current call</summary>
Public NotInheritable Class AmbientContext
  Inherits ContextBase

#Region " Scoped Singleton "

  Public Shared ReadOnly Property Current As AmbientContext
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of AmbientContext)(CallScope.Current)
    End Get
  End Property

#End Region

End Class

#End Region
