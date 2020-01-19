Imports System
Imports System.Collections
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Public Interface ISingletonContainer

  ReadOnly Property Instances As Object()

  ReadOnly Property InstanceTypes As Type()

  Function TryGetInstance(Of TSingleton)(ByRef instance As TSingleton) As Boolean

  Sub SetFactory(Of TSingleton)(factory As Func(Of TSingleton))

  Sub ResetSingleton(Of TSingleton)()

  Sub SetResetMethod(Of TSingleton)(resetMethod As StateResetMethod(Of TSingleton))
  Sub SetFlowableStateRecoveryMethod(Of TSingleton, TFlowableState)(resetMethod As FlowableStateRecoveryMethod(Of TSingleton, TFlowableState))
  Sub SetFlowableStateExtractorMethod(Of TSingleton, TFlowableState)(resetMethod As FlowableStateExtractorMethod(Of TSingleton, TFlowableState))

  Sub ResetInstance(Of TSingleton)()

  Sub RecoverFlowableStateForInstance(Of TSingleton)(sourceSnapshotContainer As IDictionary(Of String, Object))
  Sub ExtractorFlowableStateForInstance(Of TSingleton)(targetSnapshotContainer As IDictionary(Of String, Object))

  ''' <summary>
  ''' </summary>
  ''' <param name="preserveFlowableStates">should the flowable states of all services be collected and preserved before termination of the service instances?</param>
  Sub TerminateSingletons(preserveFlowableStates As Boolean)

  ''' <summary> requires a factory </summary>
  Function InitializeNewInstance(Of TSingleton)() As TSingleton

  ''' <summary> a method which will invoked during an ambient-state reset. </summary>
  ''' <param name="instance">
  ''' the instance which should be reset.
  ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
  ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
  ''' </param>
  Delegate Sub StateResetMethod(Of TSingleton)(ByRef instance As TSingleton)

  ''' <summary> a method which will invoked during an ambient-state reset. </summary>
  ''' <param name="instance">
  ''' the instance which should be reset.
  ''' its allowed to replace the instance or to set it to null, which will cause the invokation of the factory
  ''' (in the last case the 'invokeStateResetMethodAfterInstanceCreation' have to be false to avoid a endless loop)
  ''' </param>
  Delegate Sub FlowableStateRecoveryMethod(Of TSingleton, TFlowableState)(ByRef instance As TSingleton, stateSnapshot As TFlowableState)

  Delegate Sub FlowableStateExtractorMethod(Of TSingleton, TFlowableState)(instance As TSingleton, ByRef stateSnapshot As TFlowableState)

End Interface
