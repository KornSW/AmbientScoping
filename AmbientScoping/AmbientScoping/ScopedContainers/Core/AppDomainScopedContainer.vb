﻿'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

<DebuggerDisplay("AppDomainScopedContainer ({AppDomainId})")>
Public NotInheritable Class AppDomainScopedContainer
  Inherits ScopedContainer

#Region " Classic Singleton "

  'this will ever be an classical 'unscoped' singleton, because any scope discrimination will be done by the instance-methods

  Private Shared _Current As AppDomainScopedContainer = Nothing

  Public Shared ReadOnly Property GetInstance() As AppDomainScopedContainer
    Get
      If (_Current Is Nothing) Then
        _Current = New AppDomainScopedContainer
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
    Me.Suspend(_AppDomainId)
  End Sub

  Protected Overrides Function GetDiscriminator() As Object
    Return _AppDomainId
  End Function

  Public ReadOnly Property AppDomainId As Integer
    Get
      Return _AppDomainId
    End Get
  End Property

End Class
