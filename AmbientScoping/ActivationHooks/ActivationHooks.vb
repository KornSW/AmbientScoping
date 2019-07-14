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
  Public ActivateSingletonMethod As Func(Of Type, Object(), Object) = (
    Function(targeType, args)
      SyncLock _SingletonInstances
        If (_SingletonInstances.ContainsKey(targeType)) Then
          Return _SingletonInstances(targeType)
        End If
      End SyncLock
      Dim newInstance = ActivateNewMethod.Invoke(targeType, args)
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
  Public Sub ActivateNew(Of T)(ByRef target As T, ParamArray args() As Object)
    target = DirectCast(ActivateNewMethod.Invoke(GetType(T), args), T)
  End Sub

  ''' <summary>
  ''' Scope: AppDomain global
  ''' </summary>
  <Extension()>
  Public Sub ActivateSingleton(Of T)(ByRef target As T, ParamArray args() As Object)
    target = DirectCast(ActivateSingletonMethod.Invoke(GetType(T), args), T)
  End Sub

  ''' <summary>
  ''' Scope: Call (based on AsyncLocal which is like TreadStatic including all Sub-Threads)
  ''' </summary>
  <Extension()>
  Public Sub ActivateCallStatic(Of T)(ByRef target As T, ParamArray args() As Object)
    target = DirectCast(ActivateThreadStaticMethod.Invoke(GetType(T), args), T)
  End Sub

End Module
