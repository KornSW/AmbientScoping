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

Public Class ProfileBindingContext
  Implements IDisposable

  ''' <summary> This decides about the scope of the binding. By default, the 'UowScopedContainer' is used.
  ''' This means, that the decition about the 'current Profile' is stored for each 'Unit of Work' separately.</summary>
  Public Shared Property ScopedContainerSelector As Func(Of ISingletonContainer) = Function() UowScopedContainer.GetInstance()

  Public Shared ReadOnly Property Current As ProfileBindingContext
    Get

      Return SingletonEngine.GetOrCreateInstance(Of ProfileBindingContext, FlowableState)(
        ScopedContainerSelector.Invoke(),
        Function() New ProfileBindingContext,
        AddressOf ExtractFlowableState,
        AddressOf RecoverFlowableState,
        AddressOf DefaultFlowableStateFactory,
        invokeFlowableStateRecoveryMethodAfterInstanceCreation:=True
      )

    End Get
  End Property

#Region " State-Flowing "

  Protected Class FlowableState
    Public Property BoundProfileIdentifier As String
  End Class

  Protected Shared Function DefaultFlowableStateFactory() As FlowableState
    Return New FlowableState With {
      .BoundProfileIdentifier = Nothing
    }
  End Function

  Protected Shared Sub RecoverFlowableState(ByRef owner As ProfileBindingContext, snapshot As FlowableState)
    owner.ProfileIdentifier = snapshot.BoundProfileIdentifier
  End Sub

  Protected Shared Sub ExtractFlowableState(owner As ProfileBindingContext, ByRef snapshot As FlowableState)
    snapshot.BoundProfileIdentifier = owner.ProfileIdentifier
  End Sub

#End Region

#Region " Content (BindingIdentifier) "

  Private Sub New()
  End Sub

  Private _ProfileIdentifier As String = Nothing

  Public Property ProfileIdentifier As String
    Get
      Return _ProfileIdentifier
    End Get
    Set(value As String)

      If (Equals(value, _ProfileIdentifier)) Then
        Exit Property
      End If

      If (Not Me.ProfileIdentifier Is Nothing) Then
        UnregisterBinding(Me)
      End If

      _ProfileIdentifier = value

      Try
        If (Not Me.ProfileIdentifier Is Nothing) Then
          RegisterBinding(Me)
        End If
      Catch
        _ProfileIdentifier = Nothing
        Throw
      End Try
    End Set
  End Property

#End Region

#Region " Consumer based Lifetime- and Cleanup-Handling "

  Public Shared Event ProfileScopeCreated(profileIdentifer As String)
  Public Shared Event ProfileScopeSuspending(profileIdentifer As String)
  Public Shared Event ConsumerBoundToProfile(profileIdentifer As String, isFirst As Boolean)
  Public Shared Event ConsumerUnboundFromProfile(profileIdentifer As String, wasLast As Boolean)

  Private Shared _Bindings As New Dictionary(Of String, List(Of ProfileBindingContext))

  Protected Shared Sub RegisterBinding(instance As ProfileBindingContext)

    Dim success As Boolean = False
    Dim isFirst As Boolean = False

    SyncLock _Bindings

      Dim lst As List(Of ProfileBindingContext)
      If (_Bindings.ContainsKey(instance.ProfileIdentifier)) Then
        lst = _Bindings(instance.ProfileIdentifier)
      Else
        lst = New List(Of ProfileBindingContext)
        _Bindings.Add(instance.ProfileIdentifier, lst)
      End If

      If (Not lst.Contains(instance)) Then
        lst.Add(instance)
        success = True
        isFirst = (lst.Count = 1)
      End If

    End SyncLock

    If (success) Then

      If (isFirst AndAlso ProfileScopeCreatedEvent IsNot Nothing) Then
        RaiseEvent ProfileScopeCreated(instance.ProfileIdentifier)
      End If

      If (ConsumerBoundToProfileEvent IsNot Nothing) Then
        RaiseEvent ConsumerBoundToProfile(instance.ProfileIdentifier, isFirst)
      End If

    End If

  End Sub

  Protected Shared Sub UnregisterBinding(instance As ProfileBindingContext)

    Dim success As Boolean = False
    Dim wasLast As Boolean = False

    SyncLock _Bindings
      If (_Bindings.ContainsKey(instance.ProfileIdentifier)) Then
        Dim lst = _Bindings(instance.ProfileIdentifier)
        If (lst.Contains(instance)) Then
          lst.Remove(instance)
          success = True
          If (lst.Count = 0) Then
            _Bindings.Remove(instance.ProfileIdentifier)
            wasLast = True
          End If
        End If
      End If
    End SyncLock

    If (success) Then

      If (ConsumerUnboundFromProfileEvent IsNot Nothing) Then
        RaiseEvent ConsumerUnboundFromProfile(instance.ProfileIdentifier, wasLast)
      End If

      If (wasLast AndAlso ProfileScopeSuspendingEvent IsNot Nothing) Then
        RaiseEvent ProfileScopeSuspending(instance.ProfileIdentifier)
      End If

    End If

  End Sub

  Public Shared Function GetAllKnownIdentifiers() As String()
    SyncLock _Bindings
      Return _Bindings.Keys.ToArray()
    End SyncLock
  End Function

  Public Shared Function GetInstancesByIdentifier(profileIdentifier As String) As ProfileBindingContext()
    SyncLock _Bindings
      If (_Bindings.ContainsKey(profileIdentifier)) Then
        Return _Bindings(profileIdentifier).ToArray()
      End If
      Return {}
    End SyncLock
  End Function

  Public Shared Function HasInstancesForIdentifier(profileIdentifier As String) As Boolean
    SyncLock _Bindings
      If (_Bindings.ContainsKey(profileIdentifier)) Then
        Return _Bindings(profileIdentifier).Count > 0
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
  Protected Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then
        'IMPORTANT: this triggers a unbind which will start a cascaded cleanup
        Me.ProfileIdentifier = Nothing
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
