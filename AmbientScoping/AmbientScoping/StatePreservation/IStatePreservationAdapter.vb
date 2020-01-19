'Imports System

'''' <summary>
'''' the implementing adater itself also needs to be serializable
'''' </summary>
'''' <typeparam name="TStateOwner"></typeparam>
'Public Interface IStatePreservationAdapter(Of TStateOwner)
'  Inherits IDisposable


'  'das ding hier muss auch als provider im singletonstateperservationcontroller landen



'  ''' <summary>
'  ''' This is the only one Property which will be backuped and restored,
'  ''' The contained object Type needs to:
'  ''' be a public class,
'  ''' have a public parameterless constructor
'  ''' be not nested,
'  ''' be not generic,
'  ''' be fulls serializable (object tree with only leafes of value types)
'  ''' </summary>
'  Property RawStateSnapshot As Object

'  ReadOnly Property StateName As String

'  Sub RestoreOwnerStateFromSnapshot()


'  Sub EnsureStateSnapshotIsActual()



'  WriteOnly Property BoundStateOwner As TStateOwner




'End Interface
