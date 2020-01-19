Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public NotInheritable Class ProfileScopeProvider
  Inherits ScopeProvider

#Region " Classic Singleton "

  '(scope discrimination will be done by the instance-methods) 

  Private Shared _Current As ProfileScopeProvider = Nothing

  Public Shared ReadOnly Property GetInstance() As ProfileScopeProvider
    Get
      If (_Current Is Nothing) Then
        _Current = New ProfileScopeProvider
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    ProfileBindingContext.SubscribeForBindingUnregistered(AddressOf OnBindingUnregistered)
  End Sub

  Protected Overrides Sub ShutdownScopeFor(discriminator As Object)
    ProfileBindingContext.UnsubscribeFromBindingUnregistered(AddressOf OnBindingUnregistered)
    MyBase.ShutdownScopeFor(discriminator)
  End Sub

  Private Sub OnBindingUnregistered(leavedConsumer As ProfileBindingContext, wasLastOne As Boolean)
    If (wasLastOne) Then
      Me.OnLastBindingUnregistered(leavedConsumer.ProfileIdentifier)
    End If
  End Sub

  Protected Sub OnLastBindingUnregistered(bindingIdentifier As String)
    Me.ShutdownSingletonContainerFor(bindingIdentifier)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return ProfileBindingContext.Current.ProfileIdentifier
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
