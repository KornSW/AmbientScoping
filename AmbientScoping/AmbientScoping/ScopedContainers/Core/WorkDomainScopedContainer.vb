'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

''' <summary>
''' This represents the WorkDomain as primary level of any BL relevant scoping. The states inside are scoped by the
''' currently bound 'WorkDomainIdentifier' which is hold ambient for the current call/task (see WorkDomainBinding).
''' </summary>
<DebuggerDisplay("WorkDomainScopedContainer (WorkDomain: {WorkDomainIdentifier})")>
Public NotInheritable Class WorkDomainScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an classical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As WorkDomainScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As WorkDomainScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New WorkDomainScopedContainer
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    AddHandler WorkDomainBinding.WorkDomainSuspending, AddressOf Me.Suspend
  End Sub

  Protected Overrides Sub Suspend(discriminator As Object)
    RemoveHandler WorkDomainBinding.WorkDomainSuspending, AddressOf Me.Suspend
    MyBase.Suspend(discriminator)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return WorkDomainBinding.Current.WorkDomainIdentifier
  End Function

  Public ReadOnly Property WorkDomainIdentifier As String
    Get
      Return WorkDomainBinding.Current.WorkDomainIdentifier
    End Get
  End Property

  'just convenience...
  Public Sub SwitchWorkDomain(workDomainIdentifier As String)
    WorkDomainBinding.BindCurrentCall(workDomainIdentifier)
  End Sub

End Class
