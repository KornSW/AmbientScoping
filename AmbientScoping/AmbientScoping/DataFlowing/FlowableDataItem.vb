'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Diagnostics

Namespace DataFlowing

  ''' <summary> (immutable) </summary>
  <DebuggerDisplay("FlowableDataItem ({FullyQualifiedName})")>
  Public Class FlowableDataItem

    Public Sub New(fullyQualifiedName As String, flowableData As Object)
      Me.FullyQualifiedName = fullyQualifiedName
      Me.FlowableData = flowableData
    End Sub

    Public ReadOnly Property FullyQualifiedName As String

    Public ReadOnly Property FlowableData As Object

    Public Overrides Function ToString() As String
      Return Me.FullyQualifiedName
    End Function

  End Class

End Namespace
