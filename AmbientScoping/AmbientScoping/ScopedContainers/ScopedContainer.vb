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

Public MustInherit Class ScopedContainer
  Implements ISingletonContainer
  Implements IDisposable

  Protected Sub New()
  End Sub

#Region " internal Container Handling "

  Private _StateContainersPerDiscriminator As New Dictionary(Of Object, IDictionary(Of String, Object))

  Protected MustOverride Function GetDiscriminator() As Object

  Protected Overridable Function StateContainerFactory(discriminator As Object) As IDictionary(Of String, Object)
    Return New Dictionary(Of String, Object)
  End Function

  Public ReadOnly Property RawContainer As IDictionary(Of String, Object)
    Get
      Dim currentDiscriminator = Me.GetDiscriminator()
      Dim currentStateContainer As IDictionary(Of String, Object) = Nothing

      SyncLock _StateContainersPerDiscriminator
        Do
          If (_StateContainersPerDiscriminator.TryGetValue(currentDiscriminator, currentStateContainer)) Then
            Return currentStateContainer
          End If
          currentStateContainer = Me.StateContainerFactory(currentDiscriminator)
          _StateContainersPerDiscriminator.Add(currentDiscriminator, currentStateContainer)
        Loop
      End SyncLock

    End Get
  End Property

  Protected Overridable Sub Suspend(discriminator As Object)

    'this also tiggers preservation of flowable states...
    Me.TerminateAllSingletonInstances()

    SyncLock _StateContainersPerDiscriminator
      _StateContainersPerDiscriminator.Remove(discriminator)
    End SyncLock

  End Sub

#End Region

#Region " ISingletonContainer "

  Private Event SingletonInstanceTerminating(sender As ISingletonContainer, declaredType As Type, instance As Object) Implements ISingletonContainer.SingletonInstanceTerminating

  <DebuggerBrowsable(DebuggerBrowsableState.RootHidden)>
  Private ReadOnly Property SingletonInstances As Object()
    Get
      Return Me.GetAllSingletonInstances().ToArray()
    End Get
  End Property

  Private Iterator Function GetAllSingletonInstances() As IEnumerable(Of Object) Implements ISingletonContainer.GetAllSingletonInstances
    Dim kvps As KeyValuePair(Of String, Object)()
    SyncLock Me.RawContainer
      kvps = Me.RawContainer.ToArray()
    End SyncLock
    For Each kvp In kvps
      If (kvp.Key.StartsWith("singleton:")) Then
        Yield kvp.Value
      End If
    Next
  End Function

  Private Iterator Function GetAllSingletonTypes() As IEnumerable(Of Type) Implements ISingletonContainer.GetAllSingletonTypes
    Dim keys As String()
    SyncLock Me.RawContainer
      keys = Me.RawContainer.Keys.ToArray()
    End SyncLock
    For Each key In keys
      If (key.StartsWith("singleton:")) Then
        Yield Type.GetType(key.Substring(10))
      End If
    Next
  End Function

  Private Function TryGetSingletonInstance(Of TSingleton)(ByRef instance As TSingleton) As Boolean Implements ISingletonContainer.TryGetSingletonInstance
    Dim untypedInstance As Object = Nothing

    If (Me.TryGetSingletonInstance(GetType(TSingleton), untypedInstance)) Then
      instance = DirectCast(untypedInstance, TSingleton)
      Return True
    End If

    Return False
  End Function

  Private Function TryGetSingletonInstance(registeredType As Type, ByRef instance As Object) As Boolean Implements ISingletonContainer.TryGetSingletonInstance
    Dim registationType As String = "singleton:" + registeredType.FullName
    Dim untypedInstance As Object = Nothing

    SyncLock Me.RawContainer
      If (Me.RawContainer.TryGetValue(registationType, instance)) Then
        Return True
      End If
    End SyncLock

    Return False
  End Function

  Private Sub SetSingletonInstance(registeredType As Type, instance As Object) Implements ISingletonContainer.SetSingletonInstance
    Dim registationType As String = "singleton:" + registeredType.FullName

    SyncLock Me.RawContainer
      If (Me.RawContainer.ContainsKey(registationType)) Then
        Me.RawContainer(registationType) = instance
      Else
        Me.RawContainer.Add(registationType, instance)
      End If
    End SyncLock

  End Sub

  Private Sub SetSingletonInstance(Of TSingleton)(instance As TSingleton) Implements ISingletonContainer.SetSingletonInstance
    Me.SetSingletonInstance(GetType(TSingleton), instance)
  End Sub

  Private Sub TerminateSingletonInstance(registeredType As Type) Implements ISingletonContainer.TerminateSingletonInstance
    Dim instance As Object = Nothing
    If (Me.TryGetSingletonInstance(registeredType, instance)) Then
      If (instance IsNot Nothing) Then

        If (SingletonInstanceTerminatingEvent IsNot Nothing) Then
          RaiseEvent SingletonInstanceTerminating(Me, registeredType, instance)
        End If

        SyncLock Me.RawContainer

          Me.RawContainer.Remove("singleton:" + registeredType.FullName)

          If (TypeOf instance Is IDisposable) Then
            DirectCast(instance, IDisposable).Dispose()
          End If

        End SyncLock

      End If
    End If
  End Sub

  Private Sub TerminateSingletonInstance(Of TSingleton)() Implements ISingletonContainer.TerminateSingletonInstance
    Me.TerminateSingletonInstance(GetType(TSingleton))
  End Sub

  Private Sub TerminateAllSingletonInstances() Implements ISingletonContainer.TerminateAllSingletonInstances
    For Each registerredType In Me.GetAllSingletonTypes
      Me.TerminateSingletonInstance(registerredType)
    Next
  End Sub

#End Region

#Region " IDisposable "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Private _AlreadyDisposed As Boolean = False

  ''' <summary>
  ''' Dispose the current object instance
  ''' </summary>
  Protected Overridable Sub Dispose(disposing As Boolean)
    If (Not _AlreadyDisposed) Then
      If (disposing) Then

        Dim activeDiscriminators As Object()
        SyncLock _StateContainersPerDiscriminator
          activeDiscriminators = _StateContainersPerDiscriminator.Keys.ToArray()
        End SyncLock
        For Each discriminator In activeDiscriminators
          Me.Suspend(discriminator)
        Next
      End If
      _AlreadyDisposed = True
    End If
  End Sub

  ''' <summary>
  ''' Dispose the current object instance and suppress the finalizer
  ''' </summary>
  Public Sub Dispose() Implements IDisposable.Dispose
    Me.Dispose(True)
    GC.SuppressFinalize(Me)
  End Sub

#End Region

End Class
