'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic
Imports System.Diagnostics
Imports System.Linq
Imports System.Threading

''' <summary>
''' This context is the anchor-point of any scoping because it holds an ambient available binding of the current
''' call/task to a Uow-Scope ('Unit of Work') addressed by an 'jobIdentifier'
''' </summary>
Public NotInheritable Class UowBindingContext

#Region " Default Behaviour "

  Shared Sub New()

    'TODO: sinnvolle defaults, damit das framework direkte "ready2use" ist:

    'If(webwelt)
    '  WorkingContext.FallbackJobIdentifierSupplier = Function() id aus httpcontext ziehen wenn nicht da dann guid packen oder instaceid vom request
    '  WorkingContext.FallbackMode = BindCurrentCallToFallbackJobIdentifier
    '  WorkingContext.OnCurrentCallUnboundHook =
    '  WorkingContext.OnCurrentCallBoundHook =

    'If(winforms)
    '  WorkingContext.FallbackJobIdentifierSupplier = Function() "(global)"
    '  WorkingContext.FallbackMode = UseFallbackJobIdentifierOnDemand


    AddHandler AppDomain.CurrentDomain.DomainUnload, AddressOf AppDomain_Unload
  End Sub

  Private Shared Sub AppDomain_Unload(sender As Object, e As EventArgs)
    For Each instanceToSuspend In _InstancesPerJobIdentifier.Values.ToArray()
      instanceToSuspend.SuspendContext()
    Next
  End Sub

#End Region

#Region " Current "

  Private Shared _InstancesPerJobIdentifier As New Dictionary(Of String, UowBindingContext)

  ''' <summary>
  ''' returns the WorkingContext-Instance fur the current JobIdentifier (if the current call was bound to one).
  ''' otherwise the behaviour will be as configured in 'BahaviourOnUnboundAccess' (exception, null or fallback)
  ''' </summary>
  ''' <returns></returns>
  Public Shared ReadOnly Property Current As UowBindingContext
    Get
      Return GetCurrent()
    End Get
  End Property

  Private Shared Function GetCurrent(Optional suppressExceptions As Boolean = False) As UowBindingContext

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
        If (CallBoundToUowEvent IsNot Nothing) Then
          RaiseEvent CallBoundToUow(_CurrentlyBoundWorkingJobIdentifier.Value)
        End If
      End If
    Else
      If (String.IsNullOrWhiteSpace(_CurrentlyBoundWorkingJobIdentifier.Value)) Then
        'unbind
        If (CallUnboundFromUowEvent IsNot Nothing) Then
          RaiseEvent CallUnboundFromUow(oldJobIdentifier)
        End If
      ElseIf (Not _CurrentlyBoundWorkingJobIdentifier.Value = oldJobIdentifier) Then
        'change
        If (CallUnboundFromUowEvent IsNot Nothing) Then
          RaiseEvent CallUnboundFromUow(oldJobIdentifier)
        End If
        If (CallBoundToUowEvent IsNot Nothing) Then
          RaiseEvent CallBoundToUow(_CurrentlyBoundWorkingJobIdentifier.Value)
        End If
      End If
    End If

    If (String.IsNullOrWhiteSpace(jobIdentifier)) Then
      Return Nothing
    End If

    Dim createdNew As Boolean = False
    Dim foundContextInstance As UowBindingContext
    SyncLock _InstancesPerJobIdentifier
      If (_InstancesPerJobIdentifier.ContainsKey(jobIdentifier)) Then
        foundContextInstance = _InstancesPerJobIdentifier(jobIdentifier)
      Else
        foundContextInstance = New UowBindingContext(jobIdentifier)
        _InstancesPerJobIdentifier.Add(jobIdentifier, foundContextInstance)
        createdNew = True
      End If
    End SyncLock

    If (createdNew AndAlso UowScopeCreatedEvent IsNot Nothing) Then
      RaiseEvent UowScopeCreated(jobIdentifier)
    End If

    Return foundContextInstance
  End Function

#End Region

#Region " Ambient Call Binding (AsyncLocal) "

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
      If (CallBoundToUowEvent IsNot Nothing) Then
        RaiseEvent CallBoundToUow(jobIdentifier)
      End If

    ElseIf (Not _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier) Then
      Dim oldValue = _CurrentlyBoundWorkingJobIdentifier.Value
      _CurrentlyBoundWorkingJobIdentifier.Value = jobIdentifier
      If (CallUnboundFromUowEvent IsNot Nothing) Then
        RaiseEvent CallUnboundFromUow(oldValue)
      End If
      If (CallBoundToUowEvent IsNot Nothing) Then
        RaiseEvent CallBoundToUow(jobIdentifier)
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

    If (CallUnboundFromUowEvent IsNot Nothing) Then
      RaiseEvent CallUnboundFromUow(oldValue)
    End If

    Return True
  End Function

  ''' <summary>
  ''' returns false when the current thread was not bound before
  ''' </summary>
  ''' <returns></returns>
  Public Shared Function UnbindCurrentCallAndSuspendContext() As Boolean

    If (Not CurrentCallIsBound) Then
      Return False
    End If

    Dim oldValue = _CurrentlyBoundWorkingJobIdentifier.Value
    _CurrentlyBoundWorkingJobIdentifier.Value = String.Empty

    If (CallUnboundFromUowEvent IsNot Nothing) Then
      RaiseEvent CallUnboundFromUow(oldValue)
    End If

    SyncLock _InstancesPerJobIdentifier
      If (_InstancesPerJobIdentifier.ContainsKey(oldValue)) Then
        Dim instanceToSuspend As UowBindingContext = _InstancesPerJobIdentifier(oldValue)
        instanceToSuspend.SuspendContext()
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

  Public Sub SuspendContext()

    'this triggers the shutdown of singletons...
    If (UowScopeSuspendingEvent IsNot Nothing) Then
      RaiseEvent UowScopeSuspending(Me.JobIdentifier)
    End If

  End Sub

#End Region

#Region " Events "

  Public Shared Event UowScopeCreated(jobIdentifer As String)

  Public Shared Event UowScopeSuspending(jobIdentifer As String)

  Public Shared Event CallBoundToUow(jobIdentifer As String)

  Public Shared Event CallUnboundFromUow(jobIdentifer As String)

#End Region

#Region " Hooks (customizing) "

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
