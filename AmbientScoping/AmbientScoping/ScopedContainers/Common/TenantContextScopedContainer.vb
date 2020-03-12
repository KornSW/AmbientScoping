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
<DebuggerDisplay("TenantContextScopedContainer ({TenantIdentifier})")>
Public NotInheritable Class TenantContextScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an classical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As TenantContextScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As TenantContextScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New TenantContextScopedContainer
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    AddHandler TenantContextBinding.TenantScopeSuspending, AddressOf Me.Suspend
  End Sub

  Protected Overrides Sub Suspend(discriminator As Object)
    RemoveHandler TenantContextBinding.TenantScopeSuspending, AddressOf Me.Suspend
    MyBase.Suspend(discriminator)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return Me.TenantIdentifier
  End Function

  Public ReadOnly Property TenantIdentifier As String
    Get
      Return TenantContextBinding.Current.TenantIdentifier
    End Get
  End Property

  'just convenience...
  Public Sub SwitchTenant(tenantIdentifier As String)
    TenantContextBinding.Current.TenantIdentifier = tenantIdentifier
  End Sub

End Class
