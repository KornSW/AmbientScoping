'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

''' <summary>
''' This represents the 'Unit of Work' as smallest level of any scoping. The states inside are scoped by the
''' currently bound 'JobIdentifier' which is hold ambient for the current call/task (see TaskBindingContext).
''' </summary>
<DebuggerDisplay("UowScopedContainer (Job: {JobIdentifier})")>
Public NotInheritable Class UowScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an clissical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As UowScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As UowScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New UowScopedContainer
      End If
      Return _Current
    End Get
  End Property

#End Region

  Private Sub New()
    AddHandler UowBindingContext.UowScopeSuspending, AddressOf Me.Suspend
  End Sub

  Protected Overrides Sub Suspend(discriminator As Object)
    RemoveHandler UowBindingContext.UowScopeSuspending, AddressOf Me.Suspend
    MyBase.Suspend(discriminator)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return UowBindingContext.Current.JobIdentifier
  End Function

  Public ReadOnly Property JobIdentifier As String
    Get
      Return UowBindingContext.Current.JobIdentifier
    End Get
  End Property

  'just convenience...
  Public Sub SwitchJob(jobIdentifier As String)
    UowBindingContext.BindCurrentCall(jobIdentifier)
  End Sub

End Class
