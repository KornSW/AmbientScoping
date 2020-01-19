Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public NotInheritable Class ApplicationScopeProvider
  Inherits ScopeProvider

#Region " Classic Singleton "

  '(scope discrimination will be done by the instance-methods) 

  Private Shared _Current As ApplicationScopeProvider = Nothing

  Public Shared ReadOnly Property GetInstance() As ApplicationScopeProvider
    Get
      If (_Current Is Nothing) Then
        _Current = New ApplicationScopeProvider
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private _AppDomainId As Integer = AppDomain.CurrentDomain.Id

  Private Sub New()
    AddHandler AppDomain.CurrentDomain.DomainUnload, AddressOf OnAppDomainShutdown
  End Sub

  Private Sub OnAppDomainShutdown(sender As Object, e As EventArgs)
    Me.ShutdownSingletonContainerFor(ProfileIdentifier)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return _AppDomainId
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
