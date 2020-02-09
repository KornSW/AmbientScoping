'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports AmbientScoping.DataFlowing

Namespace Singletons

  'TODO: in activationhooks registrieren
  'TODO: Auto-Shutdown on Idle

  ''' <summary>
  ''' Controls intialisation, lifetime and state-preservation/-flowing for one singleton
  ''' </summary>
  Friend Class SingletonController

#Region " Delegates "

    ''' <summary> a method which will invoked during an ambient-state reset. </summary>
    ''' <param name="singletonInstance">
    ''' the instance which should be reset.
    ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    Public Delegate Sub UntypedStateResetMethod(ByRef singletonInstance As Object)

    ''' <summary> a method which will invoked during an ambient-state reset. </summary>
    ''' <param name="singletonInstance">
    ''' the instance which should be reset.
    ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    Delegate Sub UntypedFlowableStateRecoveryMethod(ByRef singletonInstance As Object, dto As Object)

    Delegate Sub UntypedFlowableStateExtractorMethod(singletonInstance As Object, ByRef dto As Object)

#End Region

#Region " Constructor & wire-up "

    Private WithEvents _Container As ISingletonContainer

    Public Sub New(container As ISingletonContainer, registrationType As Type, flowableDataBuffer As FlowableDataBuffer)
      _Container = container
      Me.RegistrationType = registrationType
      Me.FlowableDataBuffer = flowableDataBuffer
    End Sub

    Public Property FlowableDataBuffer As FlowableDataBuffer

    Public ReadOnly Property Container As ISingletonContainer
      Get
        Return _Container
      End Get
    End Property

#End Region

#Region " Configuration "

    Private _Factory As Func(Of Object) = Function() Activator.CreateInstance(Me.RegistrationType)
    Private _ResetMethod As UntypedStateResetMethod = Nothing 'nothing: destroy singlton per default!!!
    Private _InitialStateFactory As Func(Of Object) = Function() Activator.CreateInstance(Me.StateSnapshotType)
    Private _StateExtractorMethod As UntypedFlowableStateExtractorMethod = Nothing
    Private _DtoTypeForExtrator As Type = Nothing
    Private _StateRecoveryMethod As UntypedFlowableStateRecoveryMethod = Nothing
    Private _DtoTypeForRecovery As Type = Nothing

    Public Property InvokeStateResetMethodAfterInstanceCreation As Boolean = False
    Public Property InvokeFlowableStateRecoveryMethodAfterInstanceCreation As Boolean = False

    Public ReadOnly Property RegistrationType As Type

    Public ReadOnly Property StateSnapshotType As Type
      Get
        '_StateSnapshotTypeForRecovery & _StateSnapshotTypeForExtrator should always be equal! (they are just hold separate for validation)
        Return _DtoTypeForRecovery
      End Get
    End Property

    Public Sub DefineSingletonFactory(factory As Func(Of Object))
      _Factory = factory
    End Sub

    Public Sub DefineSingletonResetMethod(resetMethod As UntypedStateResetMethod)
      _ResetMethod = resetMethod
    End Sub

    Public Sub DefineFlowableStateRecoveryMethod(dtoType As Type, stateRecoveryMethod As UntypedFlowableStateRecoveryMethod)
      If (dtoType Is Nothing) Then
        Throw New Exception("Please provide a 'dtoType'")
      End If
      If (_DtoTypeForExtrator IsNot Nothing AndAlso Not _DtoTypeForExtrator = dtoType) Then
        Throw New Exception("The 'dtoType' for StateRecoveryMethod and StateExtractorMethod have to be equal!")
      End If
      _DtoTypeForRecovery = dtoType
      _StateRecoveryMethod = stateRecoveryMethod
    End Sub

    Public Sub DefineFlowableStateExtractorMethod(dtoType As Type, stateExtractorMethod As UntypedFlowableStateExtractorMethod)
      If (dtoType Is Nothing) Then
        Throw New Exception("Please provide a 'dtoType'")
      End If
      If (_DtoTypeForRecovery IsNot Nothing AndAlso Not _DtoTypeForRecovery = dtoType) Then
        Throw New Exception("The 'dtoType' for StateRecoveryMethod and StateExtractorMethod have to be equal!")
      End If
      _DtoTypeForExtrator = dtoType
      _StateExtractorMethod = stateExtractorMethod
    End Sub

    Public Sub DefineInitialStateFactory(initialStateFactory As Func(Of Object))
      _InitialStateFactory = initialStateFactory
    End Sub

#End Region

#Region " Usage "

    Public Function TryGetInstance(ByRef instance As Object) As Boolean
      SyncLock Me
        Return Me.Container.TryGetSingletonInstance(Me.RegistrationType, instance)
      End SyncLock
    End Function

    Public Function GetOrCreateInstance() As Object
      SyncLock Me
        Dim instance As Object = Nothing
        If (Me.TryGetInstance(instance)) Then
          Return instance
        Else
          Me.CreateInstance(instance)
          Me.Container.SetSingletonInstance(Me.RegistrationType, instance)
        End If
        Return instance
      End SyncLock
    End Function

    ''' <summary></summary>
    ''' <param name="forceTermination"> terminate the singleton also, if it is not neccesarry
    ''' (ignores a specified 'ResetMethod', which would run a soft-reset preserving the instance)</param>
    Public Sub ResetSingletonInstance(forceTermination As Boolean, ensureIsCreated As Boolean)
      Dim instance As Object = Nothing

      If (Me.TryGetInstance(instance)) Then

        If (forceTermination) Then
          _TerminatingSingletonDuringReset = True
          Try
            Me.Container.TerminateSingletonInstance(Me.RegistrationType)
          Finally
            _TerminatingSingletonDuringReset = False
          End Try
        Else
          Me.ResetInstance(instance)

          'a new singleton could be created during recovery - ensure, that the container contains it...
          Me.Container.SetSingletonInstance(Me.RegistrationType, instance)

        End If
      End If

      If (ensureIsCreated) Then
        Me.GetOrCreateInstance()
      End If

    End Sub

    ''' <summary>
    ''' extracts the flowable state from the singleton instance into the assigned FlowableDataBuffer
    ''' </summary>
    Public Sub PreserveFlowableState()
      Dim instance As Object = Nothing
      Dim dto As Object = Nothing

      If (_StateExtractorMethod Is Nothing OrElse Me.StateSnapshotType Is Nothing OrElse Me.TryGetInstance(instance) = False) Then
        Exit Sub
      End If

      If (Not Me.FlowableDataBuffer.TryGetDataByName(Me.StateSnapshotType.FullName, dto) OrElse dto Is Nothing) Then
        dto = _InitialStateFactory.Invoke()
      End If

      _StateExtractorMethod.Invoke(instance, dto)

      If (dto Is Nothing) Then
        dto = _InitialStateFactory.Invoke()
      End If

      Me.FlowableDataBuffer.SetItem(Me.StateSnapshotType.FullName, dto)

    End Sub

    ''' <summary>
    ''' recovers the flowable state from the assigned FlowableDataBuffer into the singleton instance
    ''' </summary>
    Public Sub RecoverFlowableState()
      Dim instance As Object = Nothing
      If (Me.TryGetInstance(instance)) Then

        Me.RecoverFlowableState(instance)

        'a new singleton could be created during recovery - ensure, that the container contains it...
        Me.Container.SetSingletonInstance(Me.RegistrationType, instance)

      End If
    End Sub

#End Region

#Region " Internals "

    'semaphores (loop-protection)
    Private _CurrentlyResettingInstance As Boolean = False
    Private _CurrentlyRecoveringState As Boolean = False
    Private _TerminatingSingletonDuringReset As Boolean = False

    Private Sub CreateInstance(ByRef singletonInstance As Object)

      singletonInstance = _Factory.Invoke()

      If (Me.InvokeStateResetMethodAfterInstanceCreation AndAlso Not _CurrentlyResettingInstance) Then
        Me.ResetInstance(singletonInstance)
      End If

      If (Me.InvokeFlowableStateRecoveryMethodAfterInstanceCreation AndAlso Not _CurrentlyRecoveringState) Then
        Me.RecoverFlowableState(singletonInstance)
      End If

    End Sub

    Private Sub ResetInstance(ByRef singletonInstance As Object)

      If (singletonInstance Is Nothing) Then
        Exit Sub
      End If

      _CurrentlyResettingInstance = True
      Try

        If (_ResetMethod Is Nothing) Then

        Else
          _ResetMethod.Invoke(singletonInstance)
        End If

        If (singletonInstance Is Nothing) Then
          Me.CreateInstance(singletonInstance)
        End If

      Finally
        _CurrentlyResettingInstance = False
      End Try
    End Sub

    Private Sub RecoverFlowableState(ByRef singletonInstance As Object)

      If (_StateRecoveryMethod Is Nothing OrElse Me.StateSnapshotType Is Nothing OrElse singletonInstance IsNot Nothing) Then
        Exit Sub
      End If

      _CurrentlyRecoveringState = True
      Try

        Dim dto As Object = Nothing
        If (Not Me.FlowableDataBuffer.TryGetDataByName(Me.StateSnapshotType.FullName, dto) OrElse dto Is Nothing) Then
          dto = _InitialStateFactory.Invoke()
        End If

        _StateRecoveryMethod.Invoke(singletonInstance, dto)

        If (singletonInstance Is Nothing) Then
          Me.CreateInstance(singletonInstance)
        End If

      Finally
        _CurrentlyRecoveringState = False
      End Try
    End Sub

    Private Sub OnInstanceTerminating(sender As ISingletonContainer, declaredType As Type, instance As Object) Handles _Container.SingletonInstanceTerminating
      If (declaredType = Me.RegistrationType AndAlso _TerminatingSingletonDuringReset = False) Then
        Me.PreserveFlowableState()
      End If
    End Sub

#End Region

  End Class

End Namespace
