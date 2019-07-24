Imports System
Imports AmbientScoping
Imports ComponentDiscovery
Imports DemoWithComponentDiscovery

Public Module DemoModule

  'NOTE: THIS IS A REALY COMPLEX FIREWORK, BASED ON THE COMBINATION OF MULTIPLE FRAMEWORKS!
  'IF YOU WANT TO LEARN SOMETHING ABOUT THE FRAMEWORKS, YOU SOULD GO THRU IT REALLY SLOW AND STEP BY STEP!!!

  Sub Main()

    Console.WriteLine("Please enter Tenant name ('TenantA' or 'TenentB'):")
    Dim currentTenantName As String = Console.ReadLine()

    '##### wire-up between ActivationHooks & AmbientScoping #########################################

    'the ActivationHooks are the lowest layer -> in .net we cannot hijack constructors, so we need
    'to have a really really lightwight hook, which can be used by anyone, who wants to support
    'this basically! The ActivationHooks package gives a Pattern to us, based on some elementar
    'extension-methods Like:
    '      anyExtendee.ActivateSingleton()     < the instance will be applied byRef
    '      anyExtendee.ActivateNew(...)        < the instance will be applied byRef


    'MAJOR GOAL 1. FOR THIS SHOWCASE: for each singleton, we want the AmbientScoping to be used
    ActivationHooks.ActivateSingletonMethod = (
      Function(requestdType)

        'the scope for each singleton comes from the centralized configuration of the AmbientScoping
        Dim scope As Scope = ScopedSingleton.ResolveDefaultScopeForType(requestdType)

        'the factory itself will be redirected back to the ActivationHooks
        'to keep the posibillity to change the implementation type of services
        Dim factory As Func(Of Object) = Function() ActivationHooks.GetParameterlessFactoryForType(requestdType).Invoke()

        'now the singleton can be requested from the AmbientScoping
        Return ScopedSingleton.GetOrCreateInstance(requestdType, scope, factory)

      End Function
    )

    '##### apply the configuation for our demo project

    TenantScope.ActivateTenant(currentTenantName) '< this should be done before the first Tenant-Scoped singleton in requested

    'our 'MyService' should be used in 'ProfileScope'
    ScopedSingleton.SetDefaultScopeResolverForType(Of IMyService)(Function(t) ProfileScope.Current)

    'the ComponentDiscovery should be living in 'TenantScope' (the state will be different for different tenants)
    ScopedSingleton.SetDefaultScopeResolverForType(Of ClassificationBasedAssemblyIndexer)(Function(t) TenantScope.Current)
    ScopedSingleton.SetDefaultScopeResolverForType(Of ClassificationBasedTypeIndexer)(Function(t) TenantScope.Current)

    'MAJOR GOAL 2. FOR THIS SHOWCASE:  we want the possibillity to request singletons via interface instead of using the concrete type
    ActivationHooks.SetEffectiveTypeResolver(Of IAssemblyIndexer)(Function(base) GetType(ClassificationBasedAssemblyIndexer))
    ActivationHooks.SetEffectiveTypeResolver(Of ITypeIndexer)(Function(base) GetType(ClassificationBasedTypeIndexer))
    'Note: this is a hard wire-up, for our IMyService we want to find the implementation dynamically -> see below...

    'the typeindexer has no parameterless constructor (it need an instance of IAssemblyIndexer) so we need to help!
    ActivationHooks.SetParameterlessFactoryForType(Of ClassificationBasedTypeIndexer)(
      Function() New ClassificationBasedTypeIndexer(ActivationHooks.GetSingleton(Of ClassificationBasedAssemblyIndexer))
    )

    '####  wire up ComponetDiscovery  ###########################################################################

    'ComponentDiscovery is a framework which builds an index of all Assemblies which are available to the
    'application (using the 'AssemblyIndexer') and provides convinience for searching types within these
    'assemblies (using the 'TypeIndexer'). It also provides a MandatoryAccessControl system based on 
    'Classifications and Clearances to restrict the set of Assemblies which will be available in the
    'current context (in this demo we use this feature to separate several tenant-specific providers
    'which are used to customize the default behaviour of our application)

    'lets specify an on-demand initializer to the AssemblyIndexer (which shall be execution when activating the singleton)
    ActivationHooks.SetOnDemandInitializerForType(Of ClassificationBasedAssemblyIndexer)(
      Sub(instance)

        'MAJOR GOAL 3. FOR THIS SHOWCASE: use ComponetDiscovery in order with Classifications
        'to support tenant related extensibility (customizing)

        instance.AddTaxonomicDimension("CustomizingLevel")
        instance.AddClearances("CustomizingLevel", "BaseFeatures")
        instance.AddClearances("CustomizingLevel", currentTenantName)

        'after the assembly-indexer has been initialized, it will create an index of all assemblies which 
        'having an <Assembly: AssemblyClassification(...)> - Attribute matching with the clearances above!
        'Classified assemblies with 'BaseFeatures' and/or 'DemoTenant' will be included,
        'but if they are classified with anything else (e.g. 'AnoterTenant'), they will be excluded from the index
      End Sub
    )

    'now the brain endless-loop: lets use the component-discovery to DYNAMICALLY find the actual implementation of IMyService
    ActivationHooks.SetEffectiveTypeResolver(Of IMyService)(
      Function(base) ActivationHooks.GetSingleton(Of ITypeIndexer).GetApplicableTypes(base, True)(0)
    )

    '####  everything was wire-up until here, now lets start the on-demand cascade!  ################################

    Do
      Console.WriteLine("Please enter to Profile under which the next call will be executed (or 'EXIT'):")
      Dim input = Console.ReadLine()
      If (input = "EXIT") Then
        Exit Do
      End If

      ProfileScope.ActivateProfile(input)

      Dim myService = ActivationHooks.GetSingleton(Of IMyService)
      myService.CountAndPrint()

    Loop

  End Sub

End Module

Public Interface IMyService
  Sub CountAndPrint()
End Interface


<TypeClassification("CustomizingLevel", "TenantA")>
Public Class MyServiceA
  Implements IMyService

  Private _Counter As Integer = 0

  Public Sub CountAndPrint() Implements IMyService.CountAndPrint
    _Counter += 1
    Console.WriteLine($"This is the concrete implementation 'MyServiceA' (exclusive for TenantA)")
    Console.WriteLine($"The singleton instance for the current profile has increased its counter-value to: {_Counter}")
    Console.WriteLine()
  End Sub

End Class

<TypeClassification("CustomizingLevel", "TenantB")>
Public Class MyServiceB
  Implements IMyService

  Private _Counter As Integer = 0

  Public Sub CountAndPrint() Implements IMyService.CountAndPrint
    _Counter += 1
    Console.WriteLine($"This is the concrete implementation 'MyServiceB' (exclusive for TenantB)")
    Console.WriteLine($"The singleton instance for the current profile has increased its counter-value to: {_Counter}")
    Console.WriteLine()
  End Sub

End Class
