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
''' This binding is the anchor-point of any scoping because it holds an ambient available binding of the current
''' call/task to a WorkDomain-Scope (like a 'Workspace' or 'Unit of Work') addressed by an 'workDomainIdentifier'
''' </summary>
Public NotInheritable Class WorkDomainBinding

#Region " Default Behaviour "

  Shared Sub New()

    'TODO: sinnvolle defaults, damit das framework direkte "ready2use" ist:

    'If(webwelt)
    '  WorkingContext.FallbackWorkDomainIdentifierSupplier = Function() id aus httpcontext ziehen wenn nicht da dann guid packen oder instaceid vom request
    '  WorkingContext.FallbackMode = BindCurrentCallToFallbackWorkDomainIdentifier
    '  WorkingContext.OnCurrentCallUnboundHook =
    '  WorkingContext.OnCurrentCallBoundHook =

    'If(winforms)
    '  WorkingContext.FallbackWorkDomainIdentifierSupplier = Function() "(global)"
    '  WorkingContext.FallbackMode = UseFallbackWorkDomainIdentifierOnDemand

    AddHandler AppDomain.CurrentDomain.DomainUnload, AddressOf AppDomain_Unload
  End Sub

  Private Shared Sub AppDomain_Unload(sender As Object, e As EventArgs)
    For Each instanceToSuspend In _InstancesPerWorkDomainIdentifier.Values.ToArray()
      instanceToSuspend.SuspendDomain()
    Next
  End Sub

#End Region

#Region " Current "

  Private Shared _InstancesPerWorkDomainIdentifier As New Dictionary(Of String, WorkDomainBinding)

  ''' <summary>
  ''' returns the Instance fur the current WorkDomainIdentifier (if the current call was bound to one).
  ''' otherwise the behaviour will be as configured in 'BahaviourOnUnboundAccess' (exception, null or fallback)
  ''' </summary>
  ''' <returns></returns>
  Public Shared ReadOnly Property Current As WorkDomainBinding
    Get
      Return GetCurrent()
    End Get
  End Property

  Private Shared Function GetCurrent(Optional suppressExceptions As Boolean = False) As WorkDomainBinding

    Dim workDomainIdentifier As String = _CurrentlyBoundWorkDomainIdentifier.Value
    Dim oldWorkDomainIdentifier As String = workDomainIdentifier

    If (PreferCustomBindingEvaluator AndAlso CustomBindingEvaluator IsNot Nothing) Then
      workDomainIdentifier = CustomBindingEvaluator.Invoke(_CurrentlyBoundWorkDomainIdentifier.Value)
    End If

    If (String.IsNullOrWhiteSpace(workDomainIdentifier)) Then

      Select Case BehaviourOnUnboundAccess

        Case UnboundAccessBehaviour.ReturnNull
          Return Nothing

        Case UnboundAccessBehaviour.ThrowException
          If (suppressExceptions) Then
            Return Nothing
          Else
            Throw New Exception($"The current call was not bound to any 'WorkDomainIdentifier' call '.BindCurrentCall(...)' before requesting the '.Current'-Property!")
          End If

        Case Else '(UnboundAccessBahaviour.UseFallbackAction)

          Select Case FallbackAction
            Case UnboundCallFallbackAction.BindCallToAGlobalSharedDomain
              workDomainIdentifier = "(global)"
              _CurrentlyBoundWorkDomainIdentifier.Value = workDomainIdentifier 'bind!

            Case UnboundCallFallbackAction.UseAGlobalSharedDomainOnDemand
              workDomainIdentifier = "(global)"

            Case UnboundCallFallbackAction.BindCallToANewDedicatedDomain
              workDomainIdentifier = Guid.NewGuid().ToString()
              _CurrentlyBoundWorkDomainIdentifier.Value = workDomainIdentifier 'bind!

            Case UnboundCallFallbackAction.UseWorkDomainIdentifierFromCustomBindingEvaluator
              If (Not PreferCustomBindingEvaluator AndAlso CustomBindingEvaluator IsNot Nothing) Then
                Dim current As String = _CurrentlyBoundWorkDomainIdentifier.Value
                workDomainIdentifier = CustomBindingEvaluator.Invoke(_CurrentlyBoundWorkDomainIdentifier.Value)
              End If

          End Select

      End Select

    End If

    If (String.IsNullOrWhiteSpace(oldWorkDomainIdentifier)) Then
      If (Not String.IsNullOrWhiteSpace(_CurrentlyBoundWorkDomainIdentifier.Value)) Then
        'bind (initial)
        If (CallBoundToWorkDomainEvent IsNot Nothing) Then
          RaiseEvent CallBoundToWorkDomain(_CurrentlyBoundWorkDomainIdentifier.Value)
        End If
      End If
    Else
      If (String.IsNullOrWhiteSpace(_CurrentlyBoundWorkDomainIdentifier.Value)) Then
        'unbind
        If (CallUnboundFromWorkDomainEvent IsNot Nothing) Then
          RaiseEvent CallUnboundFromWorkDomain(oldWorkDomainIdentifier)
        End If
      ElseIf (Not _CurrentlyBoundWorkDomainIdentifier.Value = oldWorkDomainIdentifier) Then
        'change
        If (CallUnboundFromWorkDomainEvent IsNot Nothing) Then
          RaiseEvent CallUnboundFromWorkDomain(oldWorkDomainIdentifier)
        End If
        If (CallBoundToWorkDomainEvent IsNot Nothing) Then
          RaiseEvent CallBoundToWorkDomain(_CurrentlyBoundWorkDomainIdentifier.Value)
        End If
      End If
    End If

    If (String.IsNullOrWhiteSpace(workDomainIdentifier)) Then
      Return Nothing
    End If

    Dim createdNew As Boolean = False
    Dim foundBindingInstance As WorkDomainBinding
    SyncLock _InstancesPerWorkDomainIdentifier
      If (_InstancesPerWorkDomainIdentifier.ContainsKey(workDomainIdentifier)) Then
        foundBindingInstance = _InstancesPerWorkDomainIdentifier(workDomainIdentifier)
      Else
        foundBindingInstance = New WorkDomainBinding(workDomainIdentifier)
        _InstancesPerWorkDomainIdentifier.Add(workDomainIdentifier, foundBindingInstance)
        createdNew = True
      End If
    End SyncLock

    If (createdNew AndAlso WorkDomainCreatedEvent IsNot Nothing) Then
      RaiseEvent WorkDomainCreated(workDomainIdentifier)
    End If

    Return foundBindingInstance
  End Function

#End Region

#Region " Ambient Call Binding (AsyncLocal) "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private Shared _CurrentlyBoundWorkDomainIdentifier As New AsyncLocal(Of String)

  Public Shared ReadOnly Property CurrentCallIsBound As Boolean
    Get
      Return (Not String.IsNullOrWhiteSpace(_CurrentlyBoundWorkDomainIdentifier.Value))
    End Get
  End Property

  ''' <summary>
  ''' attaches an exisiting (also if ist was suspend before was called) or create a new domain 
  ''' </summary>
  ''' <param name="workDomainIdentifier"></param>
  Public Shared Sub BindCurrentCall(workDomainIdentifier As String)
    Dim dummy As Boolean = False
    BindCurrentCall(workDomainIdentifier, dummy)
  End Sub

  ''' <summary>
  ''' ataches an exisiting (also if ist was suspend before was called) or rcreae a new domain 
  ''' </summary>
  ''' <param name="workDomainIdentifier"></param>
  Public Shared Sub BindCurrentCall(workDomainIdentifier As String, ByRef newWorkDomainWasCreated As Boolean)

    If (String.IsNullOrWhiteSpace(workDomainIdentifier)) Then
      Throw New Exception("Cannot create or bind a WorkDomain using an empty 'workDomainIdentifier'!")
    End If

    If (String.IsNullOrWhiteSpace(_CurrentlyBoundWorkDomainIdentifier.Value)) Then
      _CurrentlyBoundWorkDomainIdentifier.Value = workDomainIdentifier
      If (CallBoundToWorkDomainEvent IsNot Nothing) Then
        RaiseEvent CallBoundToWorkDomain(workDomainIdentifier)
      End If

    ElseIf (Not _CurrentlyBoundWorkDomainIdentifier.Value = workDomainIdentifier) Then
      Dim oldValue = _CurrentlyBoundWorkDomainIdentifier.Value
      _CurrentlyBoundWorkDomainIdentifier.Value = workDomainIdentifier
      If (CallUnboundFromWorkDomainEvent IsNot Nothing) Then
        RaiseEvent CallUnboundFromWorkDomain(oldValue)
      End If
      If (CallBoundToWorkDomainEvent IsNot Nothing) Then
        RaiseEvent CallBoundToWorkDomain(workDomainIdentifier)
      End If
    End If

  End Sub

  ''' <summary>
  ''' alos possiblen when not found  boolean (false wenn nicht gebundn war)   constext (and itssingltons will be kept running!!!
  ''' retuens false when the current call-tree was not bound before
  ''' </summary>
  ''' <returns></returns>
  Public Shared Function UnbindCurrentCall() As Boolean

    If (Not CurrentCallIsBound) Then
      Return False
    End If

    Dim oldValue = _CurrentlyBoundWorkDomainIdentifier.Value
    _CurrentlyBoundWorkDomainIdentifier.Value = String.Empty

    If (CallUnboundFromWorkDomainEvent IsNot Nothing) Then
      RaiseEvent CallUnboundFromWorkDomain(oldValue)
    End If

    Return True
  End Function

  ''' <summary>
  ''' returns false when the current thread was not bound before
  ''' </summary>
  ''' <returns></returns>
  Public Shared Function UnbindCurrentCallAndSuspendDomain() As Boolean

    If (Not CurrentCallIsBound) Then
      Return False
    End If

    Dim oldValue = _CurrentlyBoundWorkDomainIdentifier.Value
    _CurrentlyBoundWorkDomainIdentifier.Value = String.Empty

    If (CallUnboundFromWorkDomainEvent IsNot Nothing) Then
      RaiseEvent CallUnboundFromWorkDomain(oldValue)
    End If

    SyncLock _InstancesPerWorkDomainIdentifier
      If (_InstancesPerWorkDomainIdentifier.ContainsKey(oldValue)) Then
        Dim instanceToSuspend As WorkDomainBinding = _InstancesPerWorkDomainIdentifier(oldValue)
        instanceToSuspend.SuspendDomain()
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
    ReturnNull = 0

    ''' <summary> execute the action specified under 'FallbackAction' (Property)  </summary>
    UseFallbackAction = 1

  End Enum

  ''' <summary>
  ''' specifies, which action should be executed when the 'Current'-Property is requested by an unbound call
  ''' AND 'BahaviourOnUnboundAccess' is set to 'UseFallbackAction'
  ''' </summary>
  Public Shared Property FallbackAction As UnboundCallFallbackAction = UnboundCallFallbackAction.BindCallToAGlobalSharedDomain

  Public Enum UnboundCallFallbackAction As Integer

    ''' <summary> uses the magic-value '(global)' as WorkDomainIdentifier, binds the current call to it and creates/returns the domain for it</summary>
    BindCallToAGlobalSharedDomain = 0

    ''' <summary> uses the magic-value '(global)' as WorkDomainIdentifier and creates/returns the domain for it without binding the current call to it </summary>
    UseAGlobalSharedDomainOnDemand = 1

    ''' <summary> Generates an dedicated WorkDomainIdentifier (GUID) and creates a new domain for it </summary>
    BindCallToANewDedicatedDomain = 2

    ''' <summary> Invoke the given method of 'CustomBindingEvaluator' </summary>
    UseWorkDomainIdentifierFromCustomBindingEvaluator = 3

  End Enum

#End Region

#Region " Instance Member "

  Private Sub New(workDomainIdentifier As String)
    Me.WorkDomainIdentifier = workDomainIdentifier
  End Sub

  Public ReadOnly Property WorkDomainIdentifier As String

  Public Sub SuspendDomain()

    'this triggers the shutdown of singletons...
    If (WorkDomainSuspendingEvent IsNot Nothing) Then
      RaiseEvent WorkDomainSuspending(Me.WorkDomainIdentifier)
    End If

  End Sub

#End Region

#Region " Events "

  Public Shared Event WorkDomainCreated(workDomainIdentifier As String)

  Public Shared Event WorkDomainSuspending(workDomainIdentifier As String)

  Public Shared Event CallBoundToWorkDomain(workDomainIdentifier As String)

  Public Shared Event CallUnboundFromWorkDomain(workDomainIdentifier As String)

#End Region

#Region " Hooks (customizing) "

  ''' <summary>
  ''' Customizing-Hook: an optional delegate for the evaluation of the WorkDomainIdentifier, which can be used to pick the correct
  ''' Instance when the 'Current'-Property is requested.
  ''' If 'PreferCustomBindingEvaluator' is set to 'true', the given method will be invoked immediately when from the getter of the 'Current'-Property,
  ''' otherwise the method can be used as fallback (only for unbound calls) by setting 'BahaviourOnUnboundAccess' to 'UseFallbackAction' AND
  ''' 'FallbackAction' to 'UseWorkDomainIdentifierFromCustomBindingEvaluator'
  ''' </summary>
  Public Shared Property CustomBindingEvaluator As CustomBindingEvalutionMethod

  ''' <summary>
  ''' This delegate is made to apply a customized behaviour for the evaluation of the current call's WorkDomainIdentifier. An implementation needs 
  ''' return a WorkDomainIdentifier (for example from any ambient source like a 'HttpContext', 'RemotingContext' or 'SessionContext'). The argument
  ''' 'currentlyBoundWorkDomainIdentifier' (byRef) allows to read and/or set the WorkDomainIdentifier for the current call (stored in a AsyncLocal-Field) but this is optional,
  ''' a on-demand evaluated WorkDomainIdentifier could also be returned without setting the 'currentlyBoundWorkDomainIdentifier'
  ''' </summary>
  ''' <param name="currentlyBoundWorkDomainIdentifier">optinal access the binding value, stored inside of a AsyncLocal-Field</param>
  ''' <returns></returns>
  Public Delegate Function CustomBindingEvalutionMethod(ByRef currentlyBoundWorkDomainIdentifier As String) As String

  ''' <summary>
  ''' if true, the given 'CustomBindingEvaluator' method (if not null) will be invoked each time,
  ''' when the bound 'WorkDomainIdentifier' needs to be evlauted for the current call.
  ''' if false, the  'CustomBindingEvaluator' will only be available for use as 'FallbackAction'
  ''' </summary>
  Public Shared Property PreferCustomBindingEvaluator As Boolean = True

#End Region

End Class
