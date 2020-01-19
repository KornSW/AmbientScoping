Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

'evtl noch weitere Gemschacsirchtungen für Scopes auf dem Level eines "LogicalCalls": "ThreadScopeProvider" "WorkflowScopeProvider" "HttpContextScopeProvider"
Public NotInheritable Class CallScopeProvider
  Inherits ScopeProvider

#Region " Classic Singleton "

  '(scope discrimination will be done by the instance-methods) 

  Private Shared _Current As CallScopeProvider = Nothing

  Public Shared ReadOnly Property GetInstance() As CallScopeProvider
    Get
      If (_Current Is Nothing) Then
        _Current = New CallScopeProvider
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    CallBindingContext.SubscribeForBindingUnregistered(AddressOf OnBindingUnregistered)
  End Sub

  Protected Overrides Sub ShutdownScopeFor(discriminator As Object)
    CallBindingContext.UnsubscribeFromBindingUnregistered(AddressOf OnBindingUnregistered)
    MyBase.ShutdownScopeFor(discriminator)
  End Sub

  Private Sub OnBindingUnregistered(leavedConsumer As CallBindingContext, wasLastOne As Boolean)
    If (wasLastOne) Then
      Me.OnLastBindingUnregistered(leavedConsumer.LogicalCallId)
    End If
  End Sub

  Protected Sub OnLastBindingUnregistered(bindingIdentifier As String)
    Me.ShutdownSingletonContainerFor(bindingIdentifier)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return CallBindingContext.Current.LogicalCallId
  End Function

#Region " Convenience "

  Public Sub BindToProfile(profileIdentifier As String)
    ProfileBindingContext.Current.ProfileIdentifier = profileIdentifier
  End Sub

  Public ReadOnly Property ProfileIdentifier As String
    Get
      Return ProfileBindingContext.Current.ProfileIdentifier
    End Get
  End Property

#End Region

End Class
