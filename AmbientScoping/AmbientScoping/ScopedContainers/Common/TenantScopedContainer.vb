'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

''' <summary>
''' This provides a per 'Tenant' discrimanted container. The states inside are scoped by the
''' currently bound 'TenantIdentifier' which is hold ambient (see TenantBindingContext).
''' </summary>
<DebuggerDisplay("TenantScopedContainer ({TenantIdentifier})")>
Public NotInheritable Class TenantScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an clissical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As TenantScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As TenantScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New TenantScopedContainer
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    AddHandler TenantBindingContext.TenantScopeSuspending, AddressOf Me.Suspend
  End Sub

  Protected Overrides Sub Suspend(discriminator As Object)
    RemoveHandler TenantBindingContext.TenantScopeSuspending, AddressOf Me.Suspend
    MyBase.Suspend(discriminator)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return Me.TenantIdentifier
  End Function

  Public ReadOnly Property TenantIdentifier As String
    Get
      Return TenantBindingContext.Current.TenantIdentifier
    End Get
  End Property

  'just convenience...
  Public Sub SwitchTenant(tenantIdentifier As String)
    TenantBindingContext.Current.TenantIdentifier = tenantIdentifier
  End Sub

End Class
