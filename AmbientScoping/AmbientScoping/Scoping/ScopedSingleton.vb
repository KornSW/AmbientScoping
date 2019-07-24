Imports System
Imports System.Collections.Concurrent
Imports System.Collections.Generic
Imports System.ComponentModel
Imports System.Diagnostics
Imports System.Linq
Imports System.Runtime.CompilerServices

Friend Interface ISingletonContainer

  ReadOnly Property Singletons As IDictionary(Of Type, Object)

End Interface


Public Class ScopedSingleton

  'TODO: in activationhooks registrieren

  Shared Sub New()
    Scope.SubscribeForNewSingletonContainerInitialized(AddressOf OnContainerInitialize)
    Scope.SubscribeForSingletonContainerShutdown(AddressOf OnContainerShutdown)
  End Sub

  Private Shared Sub OnContainerInitialize(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
  End Sub

  'TODO autodispose via subscription on Scope.sysbcribeforshutdown

  Public Shared Function GetOrCreateInstance(Of T)(scope As Scope) As T
    Return GetOrCreateInstance(Of T)(scope, Function() ActivationHooks.GetNewInstance(Of T))
  End Function

  Public Shared Function GetOrCreateInstance(Of T)(scope As Scope, factory As Func(Of T)) As T
    Return DirectCast(GetOrCreateInstance(GetType(T), scope, factory), T)
  End Function

  Public Shared Function GetOrCreateInstance(t As Type, scope As Scope, factory As Func(Of Object)) As Object

    Dim singletonContainer = DirectCast(scope, ISingletonContainer).Singletons

    If (singletonContainer.ContainsKey(t)) Then
      Return singletonContainer(t)
    End If

    Dim newInstance As Object = factory.Invoke()
    singletonContainer.Add(t, newInstance)

    If (TypeOf (newInstance) Is ISupportsStateSnapshot) Then
      DirectCast(newInstance, ISupportsStateSnapshot).RecoverStateSnapshot(Function(key As String) scope.StateContainer(key))
    End If

    Return newInstance
  End Function

  Private Shared Sub OnContainerShutdown(scope As Scope, discriminator As Object, singletonContainer As IDictionary(Of Type, Object))
    TerminateSingletonInstances(scope, singletonContainer)
  End Sub

  Public Shared Sub TerminateSingletonInstances(scope As Scope)
    Dim singletonContainer = DirectCast(scope, ISingletonContainer).Singletons
    TerminateSingletonInstances(scope, singletonContainer)
  End Sub

  Private Shared Sub TerminateSingletonInstances(scope As Scope, singletonContainer As IDictionary(Of Type, Object))
    For Each key In singletonContainer.Keys.ToArray()
      Dim singletonInstance = singletonContainer(key)
      singletonContainer.Remove(key)

      If (TypeOf (singletonInstance) Is ISupportsStateSnapshot) Then
        DirectCast(singletonInstance, ISupportsStateSnapshot).CreateStateSnapshot(Sub(k, v) scope.StateContainer(k) = v)
      End If

      If (TypeOf (singletonInstance) Is IDisposable) Then
        DirectCast(singletonInstance, IDisposable).Dispose()
      End If

    Next
  End Sub

#Region " Configuration "

  Public Shared Sub SetDefaultScopeResolverForType(Of T)(resolver As Func(Of Type, Scope))
    SetDefaultScopeResolverForType(GetType(T), resolver)
  End Sub

  Public Shared Sub SetDefaultScopeResolverForType(targetType As Type, resolver As Func(Of Type, Scope))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Shared Function ResolveDefaultScopeForType(targetType As Type) As Scope
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

#End Region

End Class
