'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Reflection
Imports System.Runtime.CompilerServices

Namespace System

  Public Module ActivationHooks


    '<Extension()>
    'Public Function GetNewInstance(t As Type, ParamArray args() As Object) As Object
    '  'TODO: implementation
    '  Throw New NotImplementedException()
    'End Function






    Public Function TryGetFactoryBySignature(targetType As Type, parameterSignature As Type(), ByRef foundFactoryMethod As FactoryMethod) As Boolean

    End Function

    Public Function TryGetFactoryBySignature(targetType As Type, parameterSignature As Type(), ByRef foundFactory As Factory) As Boolean

    End Function


    Public Function GetFactoriesForType(targetType As Type) As Factory()

    End Function


    Public Class Factory
      Public Property MethodHandle As FactoryMethod
      Public Property ParameterSignature As Type()
    End Class


    Public Delegate Function FactoryMethod(defindedParameterTypes As Type(), givenParameters As Object()) As Object

    <Extension()>
    Public Sub RegisterFactory(targetType As Type, parameterSignature As Type(), method As FactoryMethod)
    End Sub


    <Extension()>
    Public Sub UnregisterFactory(targetType As Type, parameterSignature As Type())
    End Sub












#Region " Hooks "

    <DebuggerBrowsable(DebuggerBrowsableState.Never)>
    Public ActivateNewMethod As Func(Of Type, Object(), Object) = (
    Function(targeType, args)
      Return Activator.CreateInstance(targeType, args)
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

End Namespace
