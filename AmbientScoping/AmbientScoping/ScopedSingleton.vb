Imports System
Imports System.Collections
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public Class ScopedSingleton

  'TODO: in activationhooks registrieren

  Shared Sub New()
    ScopeProvider.SubscribeForNewSingletonContainerInitialized(AddressOf OnContainerInitialize)
    ScopeProvider.SubscribeForSingletonContainerShutdown(AddressOf OnContainerShutdown)
  End Sub

  Private Shared Sub OnContainerInitialize(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))

    'registrieen als knowncontainer

  End Sub

  Public Function GetAllFlowableStates() As Object

    'ruf update bei den laufenden services auf 

  End Function

  Public Function ImportAllFlowableStates(resetSingltons As Boolean) As Object



  End Function

#Region " GetOrCreateInstance-Methods "

  ''' <summary>
  ''' Returns the current singleton Instance for the given scope or create a new one
  ''' </summary>
  ''' <typeparam name="TSingleton"></typeparam>
  ''' <param name="scopedSingletonContainer">target ambience-scope</param>
  ''' <param name="singletonInstancefactory">factory to invoke when a new instance is been created</param>
  ''' <returns></returns>
  Public Shared Function GetOrCreateInstance(Of TSingleton)(scopedSingletonContainer As ISingletonContainer, singletonInstancefactory As Func(Of TSingleton)) As TSingleton
    Dim instance As TSingleton = Nothing

    If (scopedSingletonContainer.TryGetInstance(Of TSingleton)(instance)) Then
      Return instance
    End If

    scopedSingletonContainer.SetFactory(Of TSingleton)(singletonInstancefactory)

    Return scopedSingletonContainer.InitializeNewInstance(Of TSingleton)()
  End Function

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
    stateResetMethod As ISingletonContainer.StateResetMethod(Of TSingleton),
    invokeStateResetMethodAfterInstanceCreation As Boolean
  ) As TSingleton

    Dim instance As TSingleton = Nothing

    If (scopedSingletonContainer.TryGetInstance(Of TSingleton)(instance)) Then
      Return instance
    End If

    scopedSingletonContainer.SetFactory(Of TSingleton)(singletonInstancefactory)
    scopedSingletonContainer.SetResetMethod(Of TSingleton)(stateResetMethod)

    instance = scopedSingletonContainer.InitializeNewInstance(Of TSingleton)()
    If (invokeStateResetMethodAfterInstanceCreation) Then
      scopedSingletonContainer.ResetInstance(Of TSingleton)()
    End If

    Return instance
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
    stateResetMethod As ISingletonContainer.StateResetMethod(Of TSingleton),
    invokeStateResetMethodAfterInstanceCreation As Boolean
  ) As TSingleton

    Return GetOrCreateInstance(Of TSingleton)(scopedSingletonContainer, Function() New TSingleton, stateResetMethod, invokeStateResetMethodAfterInstanceCreation)
  End Function

  Public Shared Function GetOrCreateInstance(Of TSingleton, TFlowableState)(
    scopedSingletonContainer As ISingletonContainer,
    singletonInstancefactory As Func(Of TSingleton),
    flowableStateExtractorMethod As ISingletonContainer.FlowableStateExtractorMethod(Of TSingleton, TFlowableState),
    flowableStateRecoveryMethod As ISingletonContainer.FlowableStateRecoveryMethod(Of TSingleton, TFlowableState),
    initialStateFactory As Func(Of TFlowableState),
    invokeFlowableStateRecoveryMethodAfterInstanceCreation As Boolean
  ) As TSingleton

    Dim instance As TSingleton = Nothing

    If (scopedSingletonContainer.TryGetInstance(Of TSingleton)(instance)) Then
      Return instance
    End If

    scopedSingletonContainer.SetFactory(Of TSingleton)(singletonInstancefactory)

    scopedSingletonContainer.SetFlowableStateExtractorMethod(Of TSingleton, TFlowableState)(flowableStateExtractorMethod)
    scopedSingletonContainer.SetFlowableStateRecoveryMethod(Of TSingleton, TFlowableState)(flowableStateRecoveryMethod)

    instance = scopedSingletonContainer.InitializeNewInstance(Of TSingleton)()

    If (invokeFlowableStateRecoveryMethodAfterInstanceCreation) Then
      Dim snapshotWithInitialState As New Dictionary(Of String, Object)
      snapshotWithInitialState.Add(GetType(TSingleton).FullName, initialStateFactory.Invoke())
      scopedSingletonContainer.RecoverFlowableStateForInstance(Of TSingleton)(snapshotWithInitialState)
    End If

    Return instance
  End Function

  Public Shared Function GetOrCreateInstance(Of TSingleton As New, TFlowableState)(
    scopedSingletonContainer As ISingletonContainer,
    flowableStateExtractorMethod As FlowableStateExtractorMethod(Of TSingleton, TFlowableState),
    flowableStateRecoveryMethod As FlowableStateRecoveryMethod(Of TSingleton, TFlowableState),
    initialStateFactory As Func(Of TFlowableState),
    invokeFlowableStateRecoveryMethodAfterInstanceCreation As Boolean
  ) As TSingleton

    Return GetOrCreateInstance(Of TSingleton)(
      scopedSingletonContainer,
      Function() New TSingleton,
      flowableStateExtractorMethod,
      flowableStateRecoveryMethod,
      initialStateFactory,
      invokeFlowableStateRecoveryMethodAfterInstanceCreation
    )

  End Function

#End Region

  Private Shared Sub OnContainerShutdown(scope As ScopeProvider, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))


    'PRESERVE FLOWABLESTATE (bkleibt liegen fall sinfltotns wider hochfahren)


    TerminateSingletonInstances(scope, singletonContainer)
  End Sub

  Public Shared Sub TerminateSingletonInstances(scope As ScopeProvider)
    DirectCast(scope, ISingletonContainer).TerminateSingletons(True)
  End Sub

End Class
