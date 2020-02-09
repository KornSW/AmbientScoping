'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

''' <summary>
''' This provides a per 'Profile' discrimanted container. A 'Profile' is a runtime profile which defines the portfolio 
''' of functionality fot a composite application. The states inside are scoped by the currently bound 'ProfileIdentifier' 
''' which is hold ambient (see ProfileBindingContext).
''' </summary>
<DebuggerDisplay("ProfileScopedContainer ({ProfileIdentifier})")>
Public NotInheritable Class ProfileScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an clissical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As ProfileScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As ProfileScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New ProfileScopedContainer
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    AddHandler ProfileBindingContext.ProfileScopeSuspending, AddressOf Me.Suspend
  End Sub

  Protected Overrides Sub Suspend(discriminator As Object)
    RemoveHandler ProfileBindingContext.ProfileScopeSuspending, AddressOf Me.Suspend
    MyBase.Suspend(discriminator)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return Me.ProfileIdentifier
  End Function

  Public ReadOnly Property ProfileIdentifier As String
    Get
      Return ProfileBindingContext.Current.ProfileIdentifier
    End Get
  End Property

  'just convenience...
  Public Sub SwitchProfile(profileIdentifier As String)
    ProfileBindingContext.Current.ProfileIdentifier = profileIdentifier
  End Sub

End Class
