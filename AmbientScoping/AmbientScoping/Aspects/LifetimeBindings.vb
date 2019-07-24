Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

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
      Return ScopedSingleton.GetOrCreateInstance(Of ProfileBinding)(OperationScope.Current, Function() New ProfileBinding)
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

Public Class TenantBinding
  Implements IDisposable

  Public Shared Sub InvokeUnder(tenantIdentifier As String, target As Action)
    Dim ambientInstance = Current
    Dim oldTenantIdentifier = ambientInstance.tenantIdentifier
    ambientInstance.tenantIdentifier = tenantIdentifier
    Try
      target.Invoke()
    Finally
      ambientInstance.tenantIdentifier = oldTenantIdentifier
    End Try
  End Sub

  Public Shared ReadOnly Property Current As TenantBinding
    Get
      Return ScopedSingleton.GetOrCreateInstance(Of TenantBinding)(OperationScope.Current, Function() New TenantBinding)
    End Get
  End Property

  Private Sub New()

  End Sub

  Public Property TenantIdentifier As String
    Get



    End Get
    Set(value As String)

      'hier leave und join (nur falls änderung) 
      'notinh oder leerstring ist nur leave!!

    End Set
  End Property

  Public Shared ReadOnly Property ConsumersOn(tenantIdentifier As String) As TenantBinding()
    Get

    End Get
  End Property

  Public Shared Function HasConsumersOn(tenantIdentifier As String) As Boolean

  End Function

  Public Delegate Sub ConsumerJoinedSubsciber(tenantIdentifier As String, joinedConsumer As TenantBinding, isFirstOne As Boolean)
  Public Shared Sub SubscribeForConsumerJoinedTo(subscriber As ConsumerJoinedSubsciber)

  End Sub

  Public Delegate Sub ConsumerLeavedSubsciber(tenantIdentifier As String, leavedConsumer As TenantBinding, wasLastOne As Boolean)
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
        Me.TenantIdentifier = Nothing 'WICHTIG!!!!
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
