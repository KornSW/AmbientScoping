'Imports System
'Imports System.Collections.Generic

'Public Class SingletonStatePreservationController

'#Region " Singleton "

'  Shared Sub New()
'    ScopedSingleton.SetDefaultScopeResolverForType(Of SingletonStatePreservationController)(Function(t) ApplicationStateAcessor.Current)
'  End Sub

'  Public Shared Function GetInstance() As SingletonStatePreservationController
'    Return ScopedSingleton.GetOrCreateInstance(Of SingletonStatePreservationController)(Function() New SingletonStatePreservationController)
'  End Function

'#End Region

'  Private Sub New()
'  End Sub

'  'TODO: her compoonentdiscovery:  suchen nach IStatePreservationAdapter

'  Public Function TryBindPreservationAdaptersTo(stateOwner As Object, Optional snapshotToRestoreAfterBind As IDictionary(Of String, Object) = Nothing) As Boolean

'    'hier aus dem snapshot was rausholen



'    'IStatePreservationAdapter(Of X= ProfileBinding ownertype suchen   auch pro basisklasse  und der reihe nach aufrufen!!!!
'    'restore von base nach spaziel!!!






'  End Function

'  Public Function TryUnbindPreservationAdaptersFrom(stateOwner As Object, Optional snapshotToUpdateBeforeUnbind As IDictionary(Of String, Object) = Nothing) As Boolean






'  End Function

'End Class
