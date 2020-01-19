'Imports System
'Imports System.Collections.Concurrent
'Imports System.Collections.Generic
'Imports System.ComponentModel
'Imports System.Diagnostics
'Imports System.Linq
'Imports System.Runtime.CompilerServices
'Imports System.Threading

'Public NotInheritable Class CallBindingContext
'  Implements IDisposable

'  Private Shared ReadOnly _FirstCallId As String = "(shared)" 'first touch
'  Private Shared _CurrentExplCallId As New AsyncLocal(Of String)

'  Shared Sub New()
'    AddHandler AppDomain.CurrentDomain.DomainUnload, AddressOf OnAppDomainShutdown
'  End Sub

'  Private Shared Sub OnAppDomainShutdown(sender As Object, e As EventArgs)


'  End Sub

'#Region " Classic Singleton "

'  'no problem to have a shared dummy instance because we habe no persistent state (were just a facade)

'  Private Shared _Current As CallBindingContext = Nothing

'  Public Shared ReadOnly Property Current As CallBindingContext
'    Get
'      If (_Current Is Nothing) Then
'        _Current = New CallBindingContext
'      End If
'      Return _Current
'    End Get
'  End Property

'#End Region


'  Public Shared Sub Invoka()

'  End Sub

'#Region " Content (BindingIdentifier) "

'  Private Sub New()
'  End Sub

'  'ExecutionContext.Capture().RestoreFlow()
'  'ExecutionContext.Capture().SuppressFlow()
'  'ExecutionContext.Run()

'  Public Property LogicalCallId As String
'    Get
'      'bei erstverwendung ist fixe identität nötig, on demand joinen wr einen neuen logischen call
'      If (String.IsNullOrWhiteSpace(_CurrentExplCallId.Value)) Then
'        Me.LogicalCallId = Guid.NewGuid().ToString()
'      End If

'      ' EnsureCurrentThreadIsRegisteredAsCusomer


'      'wenn wir uns mekern ob der aktuelle thread gerande aserhalb oder innabherlb iner lamba wist,
'      'wissen wir ob wir ein unterthreadsind

'      ' Thread.CurrentThread.ExecutionContext.




'      'AKTUELLER AUFRUF MUSS AUCH BEKANNT SEIN WEIL ER IM UNTERTHREAD LÄNGERLAUFEN KANN ALS LAMBDA IM OBERTHRREAD
'      'HIER MUSS ALSO WIEDER DER AKTUELLE THREAD MARKIERT WEDEN
'      'BindCurrentThred (der arbeitet gerade mit)

'      Return _CurrentExplCallId.Value
'    End Get
'    Set(value As String)

'      EnsureCurrentThread

'      If (Equals(value, _CurrentExplCallId.Value)) Then
'        Exit Property
'      End If

'      If (Not String.IsNullOrWhiteSpace(_CurrentExplCallId.Value)) Then
'        UnregisterBinding(_CurrentExplCallId.Value)
'      End If

'      _CurrentExplCallId.Value = value

'      Try
'        If (Not String.IsNullOrWhiteSpace(_CurrentExplCallId.Value)) Then
'          RegisterBinding(_CurrentExplCallId.Value)
'        End If
'      Catch
'        CurrentExplCallId.Value = Nothing
'        Throw
'      End Try
'    End Set
'  End Property

'#End Region

'#Region " Consumer-related Lifetime- and Cleanup-Handling "

'  Private Shared _Bindings As New Dictionary(Of String, List(Of CallBindingContext))

'  Protected Shared Sub UnregisterBinding(instance As CallBindingContext)

'    Dim success As Boolean = False
'    Dim wasLast As Boolean = False

'    SyncLock _Bindings
'      If (_Bindings.ContainsKey(instance.ProfileIdentifier)) Then
'        Dim lst = _Bindings(instance.ProfileIdentifier)
'        If (lst.Contains(instance)) Then
'          lst.Remove(instance)
'          success = True
'          If (lst.Count = 0) Then
'            _Bindings.Remove(instance.ProfileIdentifier)
'            wasLast = True
'          End If
'        End If
'      End If
'    End SyncLock

'    If (success) Then
'      NotifyBindingUnregistered(instance, wasLast)
'    End If

'  End Sub

'  Protected Shared Sub RegisterBinding(instance As CallBindingContext)

'    Dim success As Boolean = False
'    Dim isFirst As Boolean = False

'    SyncLock _Bindings

'      Dim lst As List(Of CallBindingContext)
'      If (_Bindings.ContainsKey(instance.ProfileIdentifier)) Then
'        lst = _Bindings(instance.ProfileIdentifier)
'      Else
'        lst = New List(Of CallBindingContext)
'        _Bindings.Add(instance.ProfileIdentifier, lst)
'      End If

'      If (Not lst.Contains(instance)) Then
'        lst.Add(instance)
'        success = True
'        isFirst = (lst.Count = 1)
'      End If

'    End SyncLock

'    If (success) Then
'      NotifyBindingRegistered(instance, isFirst)
'    End If

'  End Sub

'  Public Shared Function GetAllKnownIdentifiers() As String()
'    SyncLock _Bindings
'      Return _Bindings.Keys.ToArray()
'    End SyncLock
'  End Function

'  Public Shared Function GetInstancesByIdentifier(profileIdentifier As String) As CallBindingContext()
'    SyncLock _Bindings
'      If (_Bindings.ContainsKey(profileIdentifier)) Then
'        Return _Bindings(profileIdentifier).ToArray()
'      End If
'      Return {}
'    End SyncLock
'  End Function

'  Public Shared Function HasInstancesForIdentifier(profileIdentifier As String) As Boolean
'    SyncLock _Bindings
'      If (_Bindings.ContainsKey(profileIdentifier)) Then
'        Return _Bindings(profileIdentifier).Count > 0
'      End If
'      Return False
'    End SyncLock
'  End Function

'#Region " Subsciption "

'  Public Delegate Sub BindingRegisteredSubscriber(newBinding As CallBindingContext, isFirstOne As Boolean)
'  Public Delegate Sub BindingUnregisteredLeavedSubscriber(leavedBinding As CallBindingContext, wasLastOne As Boolean)

'  Private Shared _BindingRegisteredSubscibers As New List(Of BindingRegisteredSubscriber)
'  Private Shared _BindingUnregisteredSubscibers As New List(Of BindingUnregisteredLeavedSubscriber)

'  Public Shared Sub SubscribeForBindingRegistered(subscriber As BindingRegisteredSubscriber, Optional invokeRetroactivley As Boolean = False)
'    SyncLock _BindingRegisteredSubscibers
'      If (Not _BindingRegisteredSubscibers.Contains(subscriber)) Then
'        _BindingRegisteredSubscibers.Add(subscriber)
'      End If
'    End SyncLock
'    If (invokeRetroactivley) Then
'      SyncLock _Bindings
'        For Each identifier In _Bindings.Keys
'          For Each binding In _Bindings(identifier)
'            subscriber.Invoke(binding, False)
'          Next
'        Next
'      End SyncLock
'    End If
'  End Sub

'  Public Shared Sub UnsubscribeFromBindingRegistered(subscriber As BindingRegisteredSubscriber)
'    SyncLock _BindingRegisteredSubscibers
'      If (_BindingRegisteredSubscibers.Contains(subscriber)) Then
'        _BindingRegisteredSubscibers.Remove(subscriber)
'      End If
'    End SyncLock
'  End Sub

'  Public Shared Sub SubscribeForBindingUnregistered(subscriber As BindingUnregisteredLeavedSubscriber)
'    SyncLock _BindingUnregisteredSubscibers
'      If (Not _BindingUnregisteredSubscibers.Contains(subscriber)) Then
'        _BindingUnregisteredSubscibers.Add(subscriber)
'      End If
'    End SyncLock
'  End Sub

'  Public Shared Sub UnsubscribeFromBindingUnregistered(subscriber As BindingUnregisteredLeavedSubscriber)
'    SyncLock _BindingUnregisteredSubscibers
'      If (_BindingUnregisteredSubscibers.Contains(subscriber)) Then
'        _BindingUnregisteredSubscibers.Remove(subscriber)
'      End If
'    End SyncLock
'  End Sub

'  Private Shared Sub NotifyBindingRegistered(newBinding As CallBindingContext, isFirstOne As Boolean)
'    SyncLock _BindingRegisteredSubscibers
'      For Each subscriber In _BindingRegisteredSubscibers
'        subscriber.Invoke(newBinding, isFirstOne)
'      Next
'    End SyncLock
'  End Sub

'  Private Shared Sub NotifyBindingUnregistered(leavedBinding As CallBindingContext, wasLastOne As Boolean)
'    SyncLock _BindingUnregisteredSubscibers
'      For Each subscriber In _BindingUnregisteredSubscibers
'        subscriber.Invoke(leavedBinding, wasLastOne)
'      Next
'    End SyncLock
'  End Sub

'#End Region

'#End Region

'#Region " IDisposable "

'  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
'  Private _AlreadyDisposed As Boolean = False

'  ''' <summary>
'  ''' Dispose the current object instance
'  ''' </summary>
'  Protected Overridable Sub Dispose(disposing As Boolean)
'    If (Not _AlreadyDisposed) Then
'      If (disposing) Then
'        Me.ProfileIdentifier = Nothing 'WICHTIG!!!!
'      End If
'      _AlreadyDisposed = True
'    End If
'  End Sub

'  ''' <summary>
'  ''' Dispose the current object instance and suppress the finalizer
'  ''' </summary>
'  Public Sub Dispose() Implements IDisposable.Dispose
'    Me.Dispose(True)
'    GC.SuppressFinalize(Me)
'  End Sub

'#End Region

'End Class
