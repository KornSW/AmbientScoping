Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Threading

''' <summary>
''' Represents the anchor of any ambience because every scope needs an 'BindingIdentifier'.
''' The lowest level is the binding of a call to a WorkingContext, so that any other 'BindingIdentifiers' (as Tenant-Bindings, Session-Bindings, etc.)
''' Can be placed inside of the WorkingContext.
''' </summary>
Public NotInheritable Class WorkingContext

#Region " Default Behaviour "

  Shared Sub New()

    '    If (webwelt) Then
    '      WorkingContext.FallbackJobIdentifierSupplier = Function() id aus httpcontext ziehen wenn nicht da dann guid packen oder instaceid vom request
    '               WorkingContext.FallbackMode = BindCurrentCallToFallbackJobIdentifier
    '      WorkingContext.OnCurrentCallUnboundHook =
    '      WorkingContext.OnCurrentCallBoundHook =
    'If(winforms)
    '      WorkingContext.FallbackJobIdentifierSupplier = Function() "(global)"
    '      WorkingContext.FallbackMode = UseFallbackJobIdentifierOnDemand


  End Sub

#End Region

  'todo: unbind und suspend on appdoomain ende!!!!

#Region " Current "

  Private Shared _InstancesPerJobIdentifier As New Dictionary(Of String, WorkingContext)

  ''' <summary>
  ''' returns the WorkingContext-Instance fur the current JobIdentifier (if the current call was bound to one).
  ''' otherwise the behaviour will be as configured in 'BahaviourOnUnboundAccess' (exception, null or fallback)
  ''' </summary>
  ''' <returns></returns>
  Public Shared ReadOnly Property Current As WorkingContext
    Get
      Return GetCurrent()
    End Get
  End Property

  Private Shared Function GetCurrent(Optional suppressExceptions As Boolean = False) As WorkingContext

    Dim jobIdentifier As String = _CurrentlyBoundWorkingJobIdentifier.Value
    Dim oldJobIdentifier As String = jobIdentifier

    If (PreferCustomBindingEvaluator AndAlso CustomBindingEvaluator IsNot Nothing) Then
      jobIdentifier = CustomBindingEvaluator.Invoke(_CurrentlyBoundWorkingJobIdentifier.Value)
    End If

    If (String.IsNullOrWhiteSpace(jobIdentifier)) Then

      Select Case BehaviourOnUnboundAccess

        Case UnboundAccessBehaviour.ReturnNothing
          Return Nothing

        Case UnboundAccessBehaviour.ThrowException
          If (suppressExceptions) Then
            Return Nothing
          Else
            Throw New Exception($"The current call was not bound to any 'JobIdentifier' call 'WorkingContext.BindCurrentCall(jobId)' before requesting the 'WorkingContext.Current'-Property!")
          End If

        Case Else '(UnboundAccessBahaviour.UseFallbackAction)

          Select Case FallbackAction
            Case UnboundCallFallbackAction.BindCallToAGlobalSharedContext
              jobIdentifier = "(global)"
              _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier 'bind!

            Case UnboundCallFallbackAction.UseAGlobalSharedContextOnDemand
              jobIdentifier = "(global)"

            Case UnboundCallFallbackAction.BindCallToANewDedicatedContext
              jobIdentifier = Guid.NewGuid().ToString()
              _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier 'bind!

            Case UnboundCallFallbackAction.UseJobIdentifierFromCustomBindingEvaluator
              If (Not PreferCustomBindingEvaluator AndAlso CustomBindingEvaluator IsNot Nothing) Then
                Dim current As String = _CurrentlyBoundWorkingJobIdentifier.Value
                jobIdentifier = CustomBindingEvaluator.Invoke(_CurrentlyBoundWorkingJobIdentifier.Value)
              End If

          End Select

      End Select

    End If

    If (String.IsNullOrWhiteSpace(oldJobIdentifier)) Then
      If (Not String.IsNullOrWhiteSpace(_CurrentlyBoundWorkingJobIdentifier.Value)) Then
        'bind (initial)
        If (OnCurrentCallBoundAction IsNot Nothing) Then
          OnCurrentCallBoundAction.Invoke(_CurrentlyBoundWorkingJobIdentifier.Value)
        End If
      End If
    Else
      If (String.IsNullOrWhiteSpace(_CurrentlyBoundWorkingJobIdentifier.Value)) Then
        'unbind
        If (OnCurrentCallUnboundAction IsNot Nothing) Then
          OnCurrentCallUnboundAction.Invoke(oldJobIdentifier)
        End If
      ElseIf (Not _CurrentlyBoundWorkingJobIdentifier.Value = oldJobIdentifier) Then
        'change
        If (OnCurrentCallUnboundAction IsNot Nothing) Then
          OnCurrentCallUnboundAction.Invoke(oldJobIdentifier)
        End If
        If (OnCurrentCallBoundAction IsNot Nothing) Then
          OnCurrentCallBoundAction.Invoke(_CurrentlyBoundWorkingJobIdentifier.Value)
        End If
      End If
    End If

    If (String.IsNullOrWhiteSpace(jobIdentifier)) Then
      Return Nothing
    End If

    Dim createdNew As Boolean = False
    Dim foundContextInstance As WorkingContext
    SyncLock _InstancesPerJobIdentifier
      If (_InstancesPerJobIdentifier.ContainsKey(jobIdentifier)) Then
        foundContextInstance = _InstancesPerJobIdentifier(jobIdentifier)
      Else
        foundContextInstance = New WorkingContext(jobIdentifier)
        _InstancesPerJobIdentifier.Add(jobIdentifier, foundContextInstance)
        createdNew = True
      End If
    End SyncLock

    If (createdNew AndAlso OnContextCreatedAction IsNot Nothing) Then
      OnContextCreatedAction.Invoke(jobIdentifier)
    End If

    Return foundContextInstance
  End Function

#End Region

#Region " Call Binding (AsyncLocal) "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private Shared _CurrentlyBoundWorkingJobIdentifier As New AsyncLocal(Of String)

  Public Shared ReadOnly Property CurrentCallIsBound As Boolean
    Get
      Return (Not String.IsNullOrWhiteSpace(_CurrentlyBoundWorkingJobIdentifier.Value))
    End Get
  End Property

  ''' <summary>
  ''' ataches an exisiting (also if ist was suspend before was called) or rcreae a new context 
  ''' </summary>
  ''' <param name="jobIdentifier"></param>
  Public Shared Sub BindCurrentCall(jobIdentifier As String)
    Dim dummy As Boolean = False
    BindCurrentCall(jobIdentifier, dummy)
  End Sub

  ''' <summary>
  ''' ataches an exisiting (also if ist was suspend before was called) or rcreae a new context 
  ''' </summary>
  ''' <param name="jobIdentifier"></param>
  Public Shared Sub BindCurrentCall(jobIdentifier As String, ByRef newWorkingContextWasCreated As Boolean)

    If (String.IsNullOrWhiteSpace(jobIdentifier)) Then
      Throw New Exception("Cannot create or bind a WorkingContext using an empty 'JobIdentifier'!")
    End If

    If (String.IsNullOrWhiteSpace(_CurrentlyBoundWorkingJobIdentifier.Value)) Then
      _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier
      If (OnCurrentCallBoundAction IsNot Nothing) Then
        OnCurrentCallBoundAction.Invoke(jobIdentifier)
      End If

    ElseIf (Not _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier) Then
      Dim oldValue = _CurrentlyBoundWorkingJobIdentifier.Value
      _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier
      If (OnCurrentCallUnboundAction IsNot Nothing) Then
        OnCurrentCallUnboundAction.Invoke(oldValue)
      End If
      If (OnCurrentCallBoundAction IsNot Nothing) Then
        OnCurrentCallBoundAction.Invoke(jobIdentifier)
      End If
    End If

  End Sub

  ''' <summary>
  ''' alos possiblen when not found  boolean (false wenn nicht gebundn war)   constext (and itssingltons will be kept running!!!
  ''' retuens false when the current thread was not bound before
  ''' </summary>
  ''' <returns></returns>
  Public Shared Function UnbindCurrentCall() As Boolean

    If (Not CurrentCallIsBound) Then
      Return False
    End If

    Dim oldValue = _CurrentlyBoundWorkingJobIdentifier.Value
    _CurrentlyBoundWorkingJobIdentifier.Value = String.Empty

    If (OnCurrentCallUnboundAction IsNot Nothing) Then
      OnCurrentCallUnboundAction.Invoke(oldValue)
    End If

    Return True
  End Function

  ''' <summary>
  '''   ' retuens false when the current thread was not bound before
  ''' </summary>
  ''' <param name="preserveStates">Preserve all flowable states of singletons</param>
  ''' <returns></returns>
  Public Shared Function UnbindCurrentCallAndSuspendContext(preserveStates As Boolean) As Boolean

    If (Not CurrentCallIsBound) Then
      Return False
    End If

    Dim oldValue = _CurrentlyBoundWorkingJobIdentifier.Value
    _CurrentlyBoundWorkingJobIdentifier.Value = String.Empty

    If (OnCurrentCallUnboundAction IsNot Nothing) Then
      OnCurrentCallUnboundAction.Invoke(oldValue)
    End If

    SyncLock _InstancesPerJobIdentifier
      If (_InstancesPerJobIdentifier.ContainsKey(oldValue)) Then
        Dim instanceToSuspend As WorkingContext = _InstancesPerJobIdentifier(oldValue)
        instanceToSuspend.SuspendContext(preserveStates)
      End If
    End SyncLock

    Return True
  End Function

#End Region

#Region " fallback logic "

  ''' <summary>
  '''  specifies the behaviour when the 'Current'-Property is requested by an unbound call
  ''' </summary>
  Public Shared Property BehaviourOnUnboundAccess As UnboundAccessBehaviour = UnboundAccessBehaviour.UseFallbackAction

  Public Enum UnboundAccessBehaviour As Integer

    ''' <summary> the getter of the 'Current'-Property will throw a exception </summary>
    ThrowException = -1

    ''' <summary> the getter of the 'Current'-Property will return null </summary>
    ReturnNothing = 0

    ''' <summary> execute the action specified under 'FallbackAction' (Property)  </summary>
    UseFallbackAction = 1

  End Enum

  ''' <summary>
  ''' specifies, which action should be executed when the 'Current'-Property is requested by an unbound call
  ''' AND 'BahaviourOnUnboundAccess' is set to 'UseFallbackAction'
  ''' </summary>
  Public Shared Property FallbackAction As UnboundCallFallbackAction = UnboundCallFallbackAction.BindCallToAGlobalSharedContext

  Public Enum UnboundCallFallbackAction As Integer

    ''' <summary> uses the magic-value '(global)' as JobIdentifier, binds the current call to it and creates/returns the context for it</summary>
    BindCallToAGlobalSharedContext = 0

    ''' <summary> uses the magic-value '(global)' as JobIdentifier and creates/returns the context for it without binding the current call to it </summary>
    UseAGlobalSharedContextOnDemand = 1

    ''' <summary> Generates an dedicated JobIdentifier (GUID) and creates a new context for it </summary>
    BindCallToANewDedicatedContext = 2

    ''' <summary> Invoke the given method of 'CustomBindingEvaluator' </summary>
    UseJobIdentifierFromCustomBindingEvaluator = 3

  End Enum

#End Region

#Region " Instance Member "

  Private Sub New(jobIdentifier As String)
    Me.JobIdentifier = jobIdentifier
  End Sub

  Public ReadOnly Property JobIdentifier As String

  Public ReadOnly Property HasPreservedStates As Boolean
    Get

    End Get
  End Property

  Public ReadOnly Property FlowableStates As ConcurrentDictionary(Of String, Object)

  'TODO: invoke state stores requestUpdate(me.FlowableStates)
  '  aus das setzen hier (wiederherstellen) muss über die anderen bindings eine autmaitshc cascarde starten (mit singlton resets...)




  'TODO: evtl setter um presevedstates beim hochfahren wieder einzuspeilen

  ''' <summary>
  ''' 
  ''' </summary>
  ''' <param name="preserveStates">Preserve all flowable states of singletons</param>
  Public Sub SuspendContext(preserveStates As Boolean)



    'hier singletons herunterfahren








    If (OnContextSuspendedAction IsNot Nothing) Then
      OnContextSuspendedAction.Invoke(Me.JobIdentifier) 'preserveStates
    End If

  End Sub

  Public Sub ClearPresevedStatesAndResetSingletons()







  End Sub

#End Region

#Region " Hooks (customizing) "

  ''' <summary>
  ''' Customizing-Hook: an optional delegate, which will be invoked immediately after a new WorkingContext has been created.
  ''' As argument the JobIdentifier of the new WorkingContext will be passed into the delegate.
  ''' </summary>
  Public Shared Property OnContextCreatedAction As Action(Of String) = Nothing

  ''' <summary>
  ''' Customizing-Hook: an optional delegate, which will be invoked immediately after a WorkingContext has been suspended.
  ''' As argument the JobIdentifier of the suspended WorkingContext will be passed into the delegate.
  ''' </summary>
  Public Shared Property OnContextSuspendedAction As Action(Of String) = Nothing

  ''' <summary>
  ''' Customizing-Hook: an optional delegate, which will be invoked immediately after the current call (AsyncLocal) has been
  ''' bound to a WorkingContext by setting the JobIdentifier. As argument the bound JobIdentifier will be passed into the delegate.
  ''' </summary>
  Public Shared Property OnCurrentCallBoundAction As Action(Of String) = Nothing '-> kann man dann In httpcontext reinwerfen

  ''' <summary>
  ''' Customizing-Hook: an optional delegate, which will be invoked immediately after the current call (AsyncLocal) has been
  ''' unbound from a WorkingContext by removing the JobIdentifier. As argument the removed JobIdentifier will be passed into the delegate.
  ''' </summary>
  Public Shared Property OnCurrentCallUnboundAction As Action(Of String) = Nothing

  ''' <summary>
  ''' Customizing-Hook: an optional delegate for the evaluation of the JobIdentifier, which can be used to pick the correct
  ''' WorkingContext-Instance when the 'Current'-Property is requested.
  ''' If 'PreferCustomBindingEvaluator' is set to 'true', the given method will be invoked immediately when from the getter of the 'Current'-Property,
  ''' otherwise the method can be used as fallback (only for unbound calls) by setting 'BahaviourOnUnboundAccess' to 'UseFallbackAction' AND
  ''' 'FallbackAction' to 'UseJobIdentifierFromCustomBindingEvaluator'
  ''' </summary>
  Public Shared Property CustomBindingEvaluator As CustomBindingEvalutionMethod

  ''' <summary>
  ''' This delegate is made to apply a customized behaviour for the evaluation of the current call's JobIdentifier. An implementation needs 
  ''' return a JobIdentifier (for example from any ambient source like a 'HttpContext', 'RemotingContext' or 'SessionContext'). The argument
  ''' 'currentlyBoundJobIdentifer' (byRef) allows to read and/or set the JobIdentifier for the current call (stored in a AsyncLocal-Field) but this is optional,
  ''' a on-demand evaluated JobIdentifier could also be returned without setting the 'currentlyBoundJobIdentifer'
  ''' </summary>
  ''' <param name="currentlyBoundJobIdentifer">optinal access the binding value, stored inside of a AsyncLocal-Field</param>
  ''' <returns></returns>
  Public Delegate Function CustomBindingEvalutionMethod(ByRef currentlyBoundJobIdentifer As String) As String

  ''' <summary>
  ''' if true, the given 'CustomBindingEvaluator' method (if not null) will be invoked each time,
  ''' when the bound 'JobIdentifier' needs to be evlauted for the current call.
  ''' if false, the  'CustomBindingEvaluator' will only be available for use as 'FallbackAction'
  ''' </summary>
  Public Shared Property PreferCustomBindingEvaluator As Boolean = True

#End Region

End Class
