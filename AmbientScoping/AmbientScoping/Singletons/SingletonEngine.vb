'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports AmbientScoping.DataFlowing

Namespace Singletons

  'TODO: container shutdown event um container aus der engine zu entfernen

  Public NotInheritable Class SingletonEngine

#Region "(delegates)"

    ''' <summary> a method which will invoked during an ambient-state reset. </summary>
    ''' <param name="singletonInstance">
    ''' the instance which should be reset.
    ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    Public Delegate Sub StateResetMethod(Of TSingleton)(ByRef singletonInstance As TSingleton)

    ''' <summary> a method which will invoked during an ambient-state reset. </summary>
    ''' <param name="singletonInstance">
    ''' the instance which should be reset.
    ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    Public Delegate Sub FlowableStateRecoveryMethod(Of TSingleton, TFlowableState)(ByRef singletonInstance As TSingleton, dto As TFlowableState)

    Public Delegate Sub FlowableStateExtractorMethod(Of TSingleton, TFlowableState)(singletonInstance As TSingleton, ByRef dto As TFlowableState)

    Private Sub New()
    End Sub

#End Region

#Region " DataFlowing "

    'hier werden die flowable states aller scopes gemeinsam gehalten!!!!
    'wir wissen ja nicht in welchem scope der singleton hochfahren wird...

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Private Shared WithEvents _FlowableDataBuffer As FlowableDataBuffer = FlowableDataBuffer.UowScopedInstance

    Public Shared Property FlowableDataBuffer As FlowableDataBuffer
      Get
        Return _FlowableDataBuffer
      End Get
      Set(value As FlowableDataBuffer)

        If (ReferenceEquals(_FlowableDataBuffer, value)) Then
          Exit Property
        End If

        _FlowableDataBuffer = value

        SyncLock _KnownSingletons
          For Each knownSingleton In _KnownSingletons
            'ensure that the wired-up instance of FlowableDataBuffer is up to date
            knownSingleton.FlowableDataBuffer = value
          Next
        End SyncLock

      End Set
    End Property

    Private Shared Sub PreserveAllFlowableStates(target As FlowableDataBuffer) Handles _FlowableDataBuffer.RequestingUpdate
      'target can be ignored because the event could only be raised by our bound FlowableDataBuffer
      PreserveAllFlowableStates()
    End Sub

    Public Shared Sub PreserveAllFlowableStates()
      SyncLock _KnownSingletons
        For Each knownSingleton In _KnownSingletons
          'will only affect already running singletons
          knownSingleton.PreserveFlowableState()
        Next
      End SyncLock
    End Sub

    Private Shared Sub ReDistributeAllFlowableStates(source As FlowableDataBuffer) Handles _FlowableDataBuffer.RequestingDistribution
      'source can be ignored because the event could only be raised by our bound FlowableDataBuffer
      ReDistributeAllFlowableStates()
    End Sub

    Public Shared Sub ReDistributeAllFlowableStates()
      SyncLock _KnownSingletons
        For Each knownSingleton In _KnownSingletons
          'will only affect already running singletons
          knownSingleton.RecoverFlowableState()
        Next
      End SyncLock
    End Sub

#End Region

#Region " Controllers "

    Private Shared _KnownSingletons As New List(Of SingletonController)

    Private Shared Function GetOrCreateController(Of TSingleton)(container As ISingletonContainer, ByRef isNew As Boolean) As SingletonController
      SyncLock _KnownSingletons
        Dim registrationType = GetType(TSingleton)

        For Each controller In _KnownSingletons
          If (ReferenceEquals(controller.Container, container)) Then
            If (controller.RegistrationType = registrationType) Then
              isNew = False
              Return controller
            End If
          End If
        Next

        Dim newController As New SingletonController(container, registrationType, _FlowableDataBuffer)
        _KnownSingletons.Add(newController)
        isNew = True
        Return newController

      End SyncLock
    End Function

#End Region

#Region " GetOrCreateInstance-Methods "

    ''' <summary>
    ''' Returns the current singleton Instance for the given scope or create a new one
    ''' </summary>
    ''' <typeparam name="TSingleton"></typeparam>
    ''' <param name="scopedSingletonContainer">target ambience-scope</param>
    ''' <returns></returns>
    Public Shared Function GetOrCreateInstance(Of TSingleton As New)(scopedSingletonContainer As ISingletonContainer) As TSingleton
      Return GetOrCreateInstance(Of TSingleton)(scopedSingletonContainer, Function() New TSingleton)
    End Function

    ''' <summary>
    ''' Returns the current singleton Instance for the given scope or create a new one
    ''' </summary>
    ''' <typeparam name="TSingleton"></typeparam>
    ''' <param name="scopedSingletonContainer">target ambience-scope</param>
    ''' <param name="singletonInstancefactory">factory to invoke when a new instance is been created</param>
    ''' <returns></returns>
    Public Shared Function GetOrCreateInstance(Of TSingleton)(
      scopedSingletonContainer As ISingletonContainer,
      singletonInstancefactory As Func(Of TSingleton)
    ) As TSingleton

      Dim isNewController As Boolean
      Dim controller = GetOrCreateController(Of TSingleton)(scopedSingletonContainer, isNewController)

      If (isNewController) Then
        controller.DefineSingletonFactory(singletonInstancefactory)
      End If

      Return DirectCast(controller.GetOrCreateInstance(), TSingleton)
    End Function

    ''' <summary>
    ''' Returns the current singleton Instance for the given scope or create a new one
    ''' </summary>
    ''' <typeparam name="TSingleton"></typeparam>
    ''' <param name="scopedSingletonContainer">target ambience-scope</param>
    ''' <param name="stateResetMethod">
    ''' A method which will invoked during an ambient-state reset.
    ''' Its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    ''' <param name="invokeStateResetMethodAfterInstanceCreation">
    ''' specifies, that the 'stateResetMethod' shall also be invoked immediately after a new instance has been created
    ''' (this requires the given 'stateResetMethod' not to set the instance to null, which would cause a endless loop)</param>
    ''' <returns></returns>
    Public Shared Function GetOrCreateInstance(Of TSingleton As New)(
      scopedSingletonContainer As ISingletonContainer,
      stateResetMethod As StateResetMethod(Of TSingleton),
      invokeStateResetMethodAfterInstanceCreation As Boolean
    ) As TSingleton

      Return GetOrCreateInstance(Of TSingleton)(scopedSingletonContainer, Function() New TSingleton, stateResetMethod, invokeStateResetMethodAfterInstanceCreation)
    End Function

    ''' <summary>
    ''' Returns the current singleton Instance for the given scope or create a new one
    ''' </summary>
    ''' <typeparam name="TSingleton"></typeparam>
    ''' <param name="scopedSingletonContainer">target ambience-scope</param>
    ''' <param name="singletonInstancefactory">factory to invoke when a new instance is been created</param>
    ''' <param name="stateResetMethod">
    ''' A method which will invoked during an ambient-state reset.
    ''' Its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
    ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
    ''' </param>
    ''' <param name="invokeStateResetMethodAfterInstanceCreation">
    ''' specifies, that the 'stateResetMethod' shall also be invoked immediately after a new instance has been created
    ''' (this requires the given 'stateResetMethod' not to set the instance to null, which would cause a endless loop)</param>
    ''' <returns></returns>
    Public Shared Function GetOrCreateInstance(Of TSingleton)(
      scopedSingletonContainer As ISingletonContainer,
      singletonInstancefactory As Func(Of TSingleton),
      stateResetMethod As StateResetMethod(Of TSingleton),
      invokeStateResetMethodAfterInstanceCreation As Boolean
    ) As TSingleton

      Dim isNewController As Boolean
      Dim controller = GetOrCreateController(Of TSingleton)(scopedSingletonContainer, isNewController)

      If (isNewController) Then
        controller.DefineSingletonFactory(singletonInstancefactory)
        controller.DefineSingletonResetMethod(
        Sub(ByRef instance As Object)
          'just translation between the genric-delegates (convenience) and the untyped-delegates (internal)
          Dim typedInstance = DirectCast(instance, TSingleton)
          stateResetMethod.Invoke(typedInstance)
          instance = typedInstance 'pushback byref
        End Sub
      )
        controller.InvokeStateResetMethodAfterInstanceCreation = invokeStateResetMethodAfterInstanceCreation
      End If

      Return DirectCast(controller.GetOrCreateInstance(), TSingleton)
    End Function

    Public Shared Function GetOrCreateInstance(Of TSingleton, TFlowableState)(
      scopedSingletonContainer As ISingletonContainer,
      singletonInstancefactory As Func(Of TSingleton),
      flowableStateExtractorMethod As FlowableStateExtractorMethod(Of TSingleton, TFlowableState),
      flowableStateRecoveryMethod As FlowableStateRecoveryMethod(Of TSingleton, TFlowableState),
      initialStateFactory As Func(Of TFlowableState),
      invokeFlowableStateRecoveryMethodAfterInstanceCreation As Boolean
    ) As TSingleton

      Dim isNewController As Boolean
      Dim controller = GetOrCreateController(Of TSingleton)(scopedSingletonContainer, isNewController)

      If (isNewController) Then
        controller.DefineSingletonFactory(singletonInstancefactory)
        controller.DefineFlowableStateExtractorMethod(
        GetType(TFlowableState),
        Sub(instance As Object, ByRef dto As Object)
          'just translation between the genric-delegates (convenience) and the untyped-delegates (internal)
          Dim typedInstance = DirectCast(instance, TSingleton)
          Dim typedDto = DirectCast(dto, TFlowableState)
          flowableStateExtractorMethod.Invoke(typedInstance, typedDto)
          dto = typedDto 'pushback byref
        End Sub
      )
        controller.DefineFlowableStateRecoveryMethod(
        GetType(TFlowableState),
        Sub(ByRef instance As Object, dto As Object)
          'just translation between the genric-delegates (convenience) and the untyped-delegates (internal)
          Dim typedInstance = DirectCast(instance, TSingleton)
          Dim typedDto = DirectCast(dto, TFlowableState)
          flowableStateRecoveryMethod.Invoke(typedInstance, typedDto)
          instance = typedInstance 'pushback byref
        End Sub
      )
        controller.DefineInitialStateFactory(initialStateFactory)
        controller.InvokeFlowableStateRecoveryMethodAfterInstanceCreation = invokeFlowableStateRecoveryMethodAfterInstanceCreation
      End If

      Return DirectCast(controller.GetOrCreateInstance(), TSingleton)
    End Function

    Public Shared Function GetOrCreateInstance(Of TSingleton As New, TFlowableState)(
      scopedSingletonContainer As ISingletonContainer,
      flowableStateExtractorMethod As FlowableStateExtractorMethod(Of TSingleton, TFlowableState),
      flowableStateRecoveryMethod As FlowableStateRecoveryMethod(Of TSingleton, TFlowableState),
      initialStateFactory As Func(Of TFlowableState),
      invokeFlowableStateRecoveryMethodAfterInstanceCreation As Boolean
    ) As TSingleton

      Return GetOrCreateInstance(Of TSingleton, TFlowableState)(
      scopedSingletonContainer,
      Function() New TSingleton,
      flowableStateExtractorMethod,
      flowableStateRecoveryMethod,
      initialStateFactory,
      invokeFlowableStateRecoveryMethodAfterInstanceCreation
    )

    End Function

#End Region

  End Class

End Namespace
