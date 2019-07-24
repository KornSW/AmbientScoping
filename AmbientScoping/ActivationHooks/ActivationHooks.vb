Imports System
Imports System.Runtime.CompilerServices

Public Module ActivationHooks

  Private _SingletonInstances As New Dictionary(Of Type, Object)

  <ThreadStatic>
  Private _ThreadStaticInstances As New Dictionary(Of Type, Object)

#Region " Hooks "

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Public ActivateNewMethod As Func(Of Type, Object(), Object) = (
    Function(targeType, args)
      Return Activator.CreateInstance(targeType, args)
    End Function
  )

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Public ActivateSingletonMethod As Func(Of Type, Object) = (
    Function(targeType)
      SyncLock _SingletonInstances
        If (_SingletonInstances.ContainsKey(targeType)) Then
          Return _SingletonInstances(targeType)
        End If
      End SyncLock

      'TODO: factory und type resolving

      Dim newInstance = ActivateNewMethod.Invoke(targeType, {})
      SyncLock _SingletonInstances
        If (_SingletonInstances.ContainsKey(targeType)) Then
          newInstance = _SingletonInstances(targeType)
        Else
          _SingletonInstances.Add(targeType, newInstance)
        End If
        Return newInstance
      End SyncLock
    End Function
  )

  <DebuggerBrowsable(DebuggerBrowsableState.Never)>
  Public ActivateThreadStaticMethod As Func(Of Type, Object(), Object) = (
    Function(targeType, args)
      SyncLock _ThreadStaticInstances
        If (_ThreadStaticInstances.ContainsKey(targeType)) Then
          Return _ThreadStaticInstances(targeType)
        End If
      End SyncLock
      Dim newInstance = ActivateNewMethod.Invoke(targeType, args)
      SyncLock _ThreadStaticInstances
        If (_ThreadStaticInstances.ContainsKey(targeType)) Then
          newInstance = _ThreadStaticInstances(targeType)
        Else
          _ThreadStaticInstances.Add(targeType, newInstance)
        End If
        Return newInstance
      End SyncLock
    End Function
  )

#End Region

  <Extension()>
  Public Function GetNewInstance(t As Type, ParamArray args() As Object) As Object
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

  <Extension()>
  Public Sub ApplyNewInstance(Of T)(ByRef target As T, ParamArray args() As Object)
    target = DirectCast(ActivateNewMethod.Invoke(GetType(T), args), T)
  End Sub

  Public Function GetNewInstance(Of T)(ParamArray args() As Object) As T
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

  <Extension()>
  Public Sub ApplySingleton(Of T)(ByRef target As T)
    target = DirectCast(ActivateSingletonMethod.Invoke(GetType(T)), T)
  End Sub

  Public Function GetSingleton(Of T)() As T
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

  Public Sub SetEffectiveTypeResolver(targetType As Type, resolver As Func(Of Type, Type))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Sub SetEffectiveTypeResolver(Of T)(resolver As Func(Of Type, Type))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Sub SetParameterlessFactoryForType(Of T)(factory As Func(Of T))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Function GetParameterlessFactoryForType(targetType As Type) As Func(Of Object)
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

  Public Function GetParameterlessFactoryForType(Of T)() As Func(Of T)
    'TODO: implementation
    Throw New NotImplementedException()
  End Function

  Public Sub SetParameterlessFactoryForType(targetType As Type, factory As Action(Of Object))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Sub SetOnDemandInitializerForType(targetType As Type, initializer As Action(Of Object))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

  Public Sub SetOnDemandInitializerForType(Of T)(initializer As Action(Of T))
    'TODO: implementation
    Throw New NotImplementedException()
  End Sub

End Module
