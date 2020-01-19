Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public NotInheritable Class TenantScopeProvider
  Inherits ScopeProvider

#Region " Classic Singleton "

  '(scope discrimination will be done by the instance-methods) 

  Private Shared _Current As TenantScopeProvider = Nothing

  Public Shared ReadOnly Property GetInstance() As TenantScopeProvider
    Get
      If (_Current Is Nothing) Then
        _Current = New TenantScopeProvider
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    TenantBindingContext.SubscribeForBindingUnregistered(AddressOf OnBindingUnregistered)
  End Sub

  Protected Overrides Sub ShutdownScopeFor(discriminator As Object)
    TenantBindingContext.UnsubscribeFromBindingUnregistered(AddressOf OnBindingUnregistered)
    MyBase.ShutdownScopeFor(discriminator)
  End Sub

  Private Sub OnBindingUnregistered(leavedConsumer As TenantBindingContext, wasLastOne As Boolean)
    If (wasLastOne) Then
      Me.OnLastBindingUnregistered(leavedConsumer.TenantIdentifier)
    End If
  End Sub

  Protected Sub OnLastBindingUnregistered(bindingIdentifier As String)
    Me.ShutdownSingletonContainerFor(bindingIdentifier)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return TenantBindingContext.Current.TenantIdentifier
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
