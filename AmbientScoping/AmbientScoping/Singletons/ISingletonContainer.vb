'  +------------------------------------------------------------------------+
'  ¦ this file is part of an open-source solution which is originated here: ¦
'  ¦ https://github.com/KornSW/AmbientScoping                               ¦
'  ¦ the removal of this notice is prohibited by the author!                ¦
'  +------------------------------------------------------------------------+

Imports System
Imports System.Collections.Generic

Namespace Singletons

  Public Interface ISingletonContainer

    Event SingletonInstanceTerminating(sender As ISingletonContainer, declaredType As Type, instance As Object)

    Function GetAllSingletonInstances() As IEnumerable(Of Object)
    Function GetAllSingletonTypes() As IEnumerable(Of Type)

    Function TryGetSingletonInstance(Of TSingleton)(ByRef instance As TSingleton) As Boolean
    Function TryGetSingletonInstance(registeredType As Type, ByRef instance As Object) As Boolean

    Sub SetSingletonInstance(registeredType As Type, instance As Object)

    Sub SetSingletonInstance(Of TSingleton)(instance As TSingleton)

    Sub TerminateSingletonInstance(Of TSingleton)()

    Sub TerminateSingletonInstance(registeredType As Type)

    Sub TerminateAllSingletonInstances()

  End Interface

End Namespace
