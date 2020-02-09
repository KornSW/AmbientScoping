'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports AmbientScoping.Singletons

Public Class TenantBindingContext
  Implements IDisposable

  ''' <summary> This decides about the scope of the binding. By default, the 'UowScopedContainer' is used.
  ''' This means, that the decition about the 'current Tenant' is stored for each 'Unit of Work' separately.</summary>
  Public Shared Property ScopedContainerSelector As Func(Of ISingletonContainer) = Function() UowScopedContainer.GetInstance()

  Public Shared ReadOnly Property Current As TenantBindingContext
    Get

      Return SingletonEngine.GetOrCreateInstance(Of TenantBindingContext, FlowableState)(
        ScopedContainerSelector.Invoke(),
        Function() New TenantBindingContext,
        AddressOf ExtractFlowableState,
        AddressOf RecoverFlowableState,
        AddressOf DefaultFlowableStateFactory,
        invokeFlowableStateRecoveryMethodAfterInstanceCreation:=True
      )

    End Get
  End Property

#Region " State-Flowing "

  Protected Class FlowableState
    Public Property BoundTenantIdentifier As String
  End Class

  Protected Shared Function DefaultFlowableStateFactory() As FlowableState
    Return New FlowableState With {
      .BoundTenantIdentifier = Nothing
    }
  End Function

  Protected Shared Sub RecoverFlowableState(ByRef owner As TenantBindingContext, snapshot As FlowableState)
    owner.TenantIdentifier = snapshot.BoundTenantIdentifier
  End Sub

  Protected Shared Sub ExtractFlowableState(owner As TenantBindingContext, ByRef snapshot As FlowableState)
    snapshot.BoundTenantIdentifier = owner.TenantIdentifier
  End Sub

#End Region

#Region " Content (BindingIdentifier) "

  Private Sub New()
  End Sub

  Private _TenantIdentifier As String = Nothing

  Public Property TenantIdentifier As String
    Get
      Return _TenantIdentifier
    End Get
    Set(value As String)

      If (Equals(value, _TenantIdentifier)) Then
        Exit Property
      End If

      If (Not Me.TenantIdentifier Is Nothing) Then
        UnregisterBinding(Me)
      End If

      _TenantIdentifier = value

      Try
        If (Not Me.TenantIdentifier Is Nothing) Then
          RegisterBinding(Me)
        End If
      Catch
        _TenantIdentifier = Nothing
        Throw
      End Try
    End Set
  End Property

#End Region

#Region " Consumer based Lifetime- and Cleanup-Handling "

  Public Shared Event TenantScopeCreated(profileIdentifer As String)
  Public Shared Event TenantScopeSuspending(profileIdentifer As String)
  Public Shared Event ConsumerBoundToTenant(profileIdentifer As String, isFirst As Boolean)
  Public Shared Event ConsumerUnboundFromTenant(profileIdentifer As String, wasLast As Boolean)

  Private Shared _Bindings As New Dictionary(Of String, List(Of TenantBindingContext))

  Protected Shared Sub RegisterBinding(instance As TenantBindingContext)

    Dim success As Boolean = False
    Dim isFirst As Boolean = False

    SyncLock _Bindings

      Dim lst As List(Of TenantBindingContext)
      If (_Bindings.ContainsKey(instance.TenantIdentifier)) Then
        lst = _Bindings(instance.TenantIdentifier)
      Else
        lst = New List(Of TenantBindingContext)
        _Bindings.Add(instance.TenantIdentifier, lst)
      End If

      If (Not lst.Contains(instance)) Then
        lst.Add(instance)
        success = True
        isFirst = (lst.Count = 1)
      End If

    End SyncLock

    If (success) Then

      If (isFirst AndAlso TenantScopeCreatedEvent IsNot Nothing) Then
        RaiseEvent TenantScopeCreated(instance.TenantIdentifier)
      End If

      If (ConsumerBoundToTenantEvent IsNot Nothing) Then
        RaiseEvent ConsumerBoundToTenant(instance.TenantIdentifier, isFirst)
      End If

    End If

  End Sub

  Protected Shared Sub UnregisterBinding(instance As TenantBindingContext)

    Dim success As Boolean = False
    Dim wasLast As Boolean = False

    SyncLock _Bindings
      If (_Bindings.ContainsKey(instance.TenantIdentifier)) Then
        Dim lst = _Bindings(instance.TenantIdentifier)
        If (lst.Contains(instance)) Then
          lst.Remove(instance)
          success = True
          If (lst.Count = 0) Then
            _Bindings.Remove(instance.TenantIdentifier)
            wasLast = True
          End If
        End If
      End If
    End SyncLock

    If (success) Then

      If (ConsumerUnboundFromTenantEvent IsNot Nothing) Then
        RaiseEvent ConsumerUnboundFromTenant(instance.TenantIdentifier, wasLast)
      End If

      If (wasLast AndAlso TenantScopeSuspendingEvent IsNot Nothing) Then
        RaiseEvent TenantScopeSuspending(instance.TenantIdentifier)
      End If

    End If

  End Sub

  Public Shared Function GetAllKnownIdentifiers() As String()
    SyncLock _Bindings
      Return _Bindings.Keys.ToArray()
    End SyncLock
  End Function

  Public Shared Function GetInstancesByIdentifier(tenantIdentifier As String) As TenantBindingContext()
    SyncLock _Bindings
      If (_Bindings.ContainsKey(tenantIdentifier)) Then
        Return _Bindings(tenantIdentifier).ToArray()
      End If
      Return {}
    End SyncLock
  End Function

  Public Shared Function HasInstancesForIdentifier(tenantIdentifier As String) As Boolean
    SyncLock _Bindings
      If (_Bindings.ContainsKey(tenantIdentifier)) Then
        Return _Bindings(tenantIdentifier).Count > 0
      End If
      Return False
    End SyncLock
  End Function


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
        'IMPORTANT: this triggers a unbind which will start a cascaded cleanup
        Me.TenantIdentifier = Nothing
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
