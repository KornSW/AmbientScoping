'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports AmbientScoping.Singletons

Namespace DataFlowing

  'TODO: events für OnInit & OnTerminate um hier eine persistierungslogik anzuhängen

  Public NotInheritable Class FlowableDataBuffer

    <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
    Private _RawData As New List(Of FlowableDataItem)

#Region " Constructor / Factory / Default Instance "

    Public Shared ReadOnly Property UowScopedInstance As FlowableDataBuffer
      Get

        'DANGER: we are also a singleton, but we must not touch the state flowing functionality here!
        'we are providing this functionality, so any use of this by our own would cause a stack overflow!

        Return SingletonEngine.GetOrCreateInstance(Of FlowableDataBuffer)(
          UowScopedContainer.GetInstance(), 'the flowable states have to be centralized in the lowerst level / smallest scope ("Unit of Work") 
          Function() New FlowableDataBuffer(False)
        )

      End Get
    End Property

    Public Shared Function CreateIndependentInstance() As FlowableDataBuffer
      Return New FlowableDataBuffer(True)
    End Function

    ''' <summary>
    ''' This is private because we want this class to be incompatible with serialisation to prevent from missuse!
    ''' The value of 'RawData' property should be flowed and/or serialized instead of the whole store.
    ''' </summary>
    Private Sub New(isIndependentInstance As Boolean)
      Me.IsIndependentInstance = isIndependentInstance
    End Sub

    Friend ReadOnly Property IsIndependentInstance As Boolean

#End Region

#Region " Events "

    ''' <summary>
    ''' This event will be raised while CAPTURING all flowable data
    ''' (for example when persisting the application-state during shutdown or 
    ''' before ambient data will be transmitted to a webservice)
    ''' </summary>
    Public Event RequestingUpdate(instance As FlowableDataBuffer)

    Private Sub OnRequestingUpdate()
      If (RequestingUpdateEvent IsNot Nothing) Then
        RaiseEvent RequestingUpdate(Me)
      End If
    End Sub

    ''' <summary>
    ''' This event will be raised while RESTORING all flowable data
    ''' (for example when the application is continuing its work using a persisted state or 
    ''' inside a webservice when a request was received including ambient data)
    ''' </summary>
    Public Event RequestingDistribution(instance As FlowableDataBuffer)

    Private Sub OnRequestingDistribution()
      If (RequestingDistributionEvent IsNot Nothing) Then
        RaiseEvent RequestingDistribution(Me)
      End If
    End Sub

#End Region

    Public Sub SetItem(fullyQualifiedName As String, flowableData As Object)
      SyncLock _RawData

        Dim oldItem As FlowableDataItem = Nothing

        For Each item In _RawData
          If (String.Equals(item.FullyQualifiedName, fullyQualifiedName, StringComparison.InvariantCultureIgnoreCase)) Then
            oldItem = item
            Exit For
          End If
        Next

        If (oldItem IsNot Nothing) Then
          _RawData.Remove(oldItem)
        End If

        _RawData.Add(New FlowableDataItem(fullyQualifiedName, flowableData))

      End SyncLock
    End Sub

    Public Function TryGetItemByName(fullyQualifiedName As String, ByRef foundItem As FlowableDataItem) As Boolean
      SyncLock _RawData

        For Each item In _RawData

          If (String.Equals(item.FullyQualifiedName, fullyQualifiedName, StringComparison.InvariantCultureIgnoreCase)) Then
            foundItem = item
            Return True
          End If

        Next

        Return False
      End SyncLock
    End Function

    Public Function TryRemoveItem(fullyQualifiedName As String) As Boolean
      SyncLock _RawData

        Dim oldItem As FlowableDataItem = Nothing

        For Each item In _RawData
          If (String.Equals(item.FullyQualifiedName, fullyQualifiedName, StringComparison.InvariantCultureIgnoreCase)) Then
            oldItem = item
            Exit For
          End If
        Next

        If (oldItem Is Nothing) Then
          Return False
        Else
          _RawData.Remove(oldItem)
          Return True
        End If

      End SyncLock
    End Function

    Public Sub EnsureCachedDataIsUpToDate()
      Me.OnRequestingUpdate()
    End Sub

#Region " direct access to item.FlowableData (Convenience) "

    Public Function TryGetDataByName(fullyQualifiedName As String, ByRef flowableData As Object) As Boolean
      Dim item As FlowableDataItem = Nothing

      If (Me.TryGetItemByName(fullyQualifiedName, item)) Then
        flowableData = item.FlowableData
        Return True
      End If

      Return False
    End Function

    Public Function TryGetDataByName(Of TFlowableData)(fullyQualifiedName As String, ByRef flowableData As TFlowableData) As Boolean
      Dim item As FlowableDataItem = Nothing

      If (Me.TryGetItemByName(fullyQualifiedName, item)) Then
        flowableData = DirectCast(item.FlowableData, TFlowableData)
        Return True
      End If

      Return False
    End Function

    Public Function GetDataByNameOrDefault(fullyQualifiedName As String, Optional [default] As Object = Nothing) As Object
      Dim item As FlowableDataItem = Nothing

      If (Me.TryGetItemByName(fullyQualifiedName, item)) Then
        Return item.FlowableData
      End If

      Return [default]
    End Function

    Public Function GetDataByNameOrDefault(Of TFlowableData)(fullyQualifiedName As String, [default] As TFlowableData) As TFlowableData
      Dim item As FlowableDataItem = Nothing

      If (Me.TryGetItemByName(fullyQualifiedName, item)) Then
        Return DirectCast(item.FlowableData, TFlowableData)
      End If

      Return [default]
    End Function

#End Region

#Region " extract/apply & export/import "

    Public Function ExtractRawData() As FlowableDataItem()

      Me.EnsureCachedDataIsUpToDate()

      SyncLock _RawData
        Return _RawData.ToArray()
      End SyncLock

    End Function

    Public Sub ApplyRawData(rawData As FlowableDataItem())

      SyncLock _RawData
        _RawData = rawData.ToList()
      End SyncLock

      Me.OnRequestingDistribution()

    End Sub

    ''' <summary>
    ''' copies the items into a dictionary without clearing it before
    ''' </summary>
    Public Sub ExportRawDataIntoDictionary(target As IDictionary(Of String, Object))

      Me.EnsureCachedDataIsUpToDate()

      SyncLock _RawData
        For Each item In _RawData
          target(item.FullyQualifiedName) = item.FlowableData
        Next
      End SyncLock

    End Sub

    Public Sub ImportRawDataFromDictionary(source As IDictionary(Of String, Object))

      SyncLock _RawData
        _RawData.Clear()
        Dim flowableData As Object = Nothing
        For Each fullyQualifiedName In source.Keys.ToArray()
          If (source.TryGetValue(fullyQualifiedName, flowableData)) Then
            _RawData.Add(New FlowableDataItem(fullyQualifiedName, flowableData))
          End If
        Next
      End SyncLock

      Me.OnRequestingDistribution()

    End Sub

#End Region

  End Class

End Namespace
