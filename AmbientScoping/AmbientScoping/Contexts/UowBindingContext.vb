﻿'Imports System
'Imports System.Collections.Concurrent
'Imports System.Collections.Generic
'Imports System.ComponentModel
'Imports System.Diagnostics
'Imports System.Linq
'Imports System.Runtime.CompilerServices

'Public Class UowBindingContext
'  Implements IDisposable

'  ''' <summary> This decides about the scope of the binding and should be on the level of a "logical call" (default: "AsyncLocalScopeProvider") </summary>
'  Public Shared Property ScopedContainerSelector As Func(Of ISingletonContainer) = Function() CallScopeProvider.GetInstance()
'  'on special runtime environments this could be changed to a "WorkflowScopeProvider" or "HttpContextScopeProvider"

'  Public Shared ReadOnly Property Current As UowBindingContext
'    Get

'      Return ScopedSingleton.GetOrCreateInstance(Of UowBindingContext, FlowableState)(
'        ScopedContainerSelector.Invoke(),
'        Function() New UowBindingContext,
'        AddressOf ExtractFlowableState,
'        AddressOf RecoverFlowableState,
'        AddressOf DefaultFlowableStateFactory,
'        invokeFlowableStateRecoveryMethodAfterInstanceCreation:=True
'      )

'    End Get
'  End Property

'#Region " State-Flowing "

'  Protected Class FlowableState
'    Public Property BoundUowIdentifier As String
'  End Class

'  Protected Shared Function DefaultFlowableStateFactory() As FlowableState
'    Return New FlowableState With {
'      .BoundUowIdentifier = Nothing
'    }
'  End Function

'  Protected Shared Sub RecoverFlowableState(ByRef owner As UowBindingContext, snapshot As FlowableState)
'    owner.UowIdentifier = snapshot.BoundUowIdentifier
'  End Sub

'  Protected Shared Sub ExtractFlowableState(owner As UowBindingContext, ByRef snapshot As FlowableState)
'    snapshot.BoundUowIdentifier = owner.UowIdentifier
'  End Sub

'#End Region

'#Region " Content (BindingIdentifier) "

'  Private Sub New()
'  End Sub

'  Private _UowIdentifier As String = Nothing

'  Public Property UowIdentifier As String
'    Get
'      Return _UowIdentifier
'    End Get
'    Set(value As String)

'      If (Equals(value, _UowIdentifier)) Then
'        Exit Property
'      End If

'      If (Not Me.UowIdentifier Is Nothing) Then
'        UnregisterBinding(Me)
'      End If

'      _UowIdentifier = value

'      Try
'        If (Not Me.UowIdentifier Is Nothing) Then
'          RegisterBinding(Me)
'        End If
'      Catch
'        _UowIdentifier = Nothing
'        Throw
'      End Try
'    End Set
'  End Property

'#End Region

'#Region " Consumer-related Lifetime- and Cleanup-Handling "

'  Private Shared _Bindings As New Dictionary(Of String, List(Of UowBindingContext))

'  Protected Shared Sub UnregisterBinding(instance As UowBindingContext)

'    Dim success As Boolean = False
'    Dim wasLast As Boolean = False

'    SyncLock _Bindings
'      If (_Bindings.ContainsKey(instance.UowIdentifier)) Then
'        Dim lst = _Bindings(instance.UowIdentifier)
'        If (lst.Contains(instance)) Then
'          lst.Remove(instance)
'          success = True
'          If (lst.Count = 0) Then
'            _Bindings.Remove(instance.UowIdentifier)
'            wasLast = True
'          End If
'        End If
'      End If
'    End SyncLock

'    If (success) Then
'      NotifyBindingUnregistered(instance, wasLast)
'    End If

'  End Sub

'  Protected Shared Sub RegisterBinding(instance As UowBindingContext)

'    Dim success As Boolean = False
'    Dim isFirst As Boolean = False

'    SyncLock _Bindings

'      Dim lst As List(Of UowBindingContext)
'      If (_Bindings.ContainsKey(instance.UowIdentifier)) Then
'        lst = _Bindings(instance.UowIdentifier)
'      Else
'        lst = New List(Of UowBindingContext)
'        _Bindings.Add(instance.UowIdentifier, lst)
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

'  Public Shared Function GetInstancesByIdentifier(profileIdentifier As String) As UowBindingContext()
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

'  Public Delegate Sub BindingRegisteredSubscriber(newBinding As UowBindingContext, isFirstOne As Boolean)
'  Public Delegate Sub BindingUnregisteredLeavedSubscriber(leavedBinding As UowBindingContext, wasLastOne As Boolean)

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

'  Private Shared Sub NotifyBindingRegistered(newBinding As UowBindingContext, isFirstOne As Boolean)
'    SyncLock _BindingRegisteredSubscibers
'      For Each subscriber In _BindingRegisteredSubscibers
'        subscriber.Invoke(newBinding, isFirstOne)
'      Next
'    End SyncLock
'  End Sub

'  Private Shared Sub NotifyBindingUnregistered(leavedBinding As UowBindingContext, wasLastOne As Boolean)
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
'        Me.UowIdentifier = Nothing 'WICHTIG!!!!
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