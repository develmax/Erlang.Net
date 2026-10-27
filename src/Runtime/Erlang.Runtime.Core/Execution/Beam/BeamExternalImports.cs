namespace Erlang;

internal static class BeamExternalImports
{
    internal static BeamExternalImport Resolve(BeamLinkedCode code, int index)
    {
        BeamImport source = code.Structure.Imports[index];
        string module = code.Structure.Atoms[source.ModuleAtomIndex]!;
        string function = code.Structure.Atoms[source.FunctionAtomIndex]!;

        return Resolve(
            code,
            module,
            function,
            source.Arity
        );
    }

    internal static BeamExternalImport Resolve(
        BeamLinkedCode code,
        string module,
        string function,
        int arity
    )
    {
        bool supported = NativeBuiltinCatalog.Contains(module, function, arity) || module == ErlangModuleNames.Module && (
            arity == 0 && function == ErlangModuleNames.Self
            || function == ProcessGarbageCollectionNames.Function && arity is 0 or 1 or 2
            || function is ErlangModuleNames.Spawn or ErlangModuleNames.SpawnLink or ErlangModuleNames.SpawnMonitor && arity is 1 or 3
            || arity == 1 && function is ErlangModuleNames.WhereIs or ErlangModuleNames.Error or BeamLoaderNames.CallOnLoad or ErlangErrorMetadataNames.NifError
                or ErlangModuleNames.IsList or ErlangModuleNames.Length or ErlangModuleNames.Spawn
            || arity == 2 && function is BeamRegisterNames.Add or BeamRegisterNames.Subtract or ErlangModuleNames.Put or SourceLoadNifNames.Function
                or ErlangModuleNames.Error or ErlangOperators.Append or ErlangOperators.IntegerDivide
        ) || module == BeamOnLoadNames.CodeModule && function == BeamOnLoadNames.IsLoaded && arity == 1
            || module == BeamRegisterNames.OsModule && function == BeamRegisterNames.GetEnvironment && arity == 1
            || module == ListModuleNames.Module && (function == ListModuleNames.Reverse || function == ListModuleNames.Member) && arity == 2
            || code.RegistryModuleDependencies.Contains(module);
        supported |= MapNativeBridge.IsCompilerHelper(module, function, arity);
        if (!supported)
            throw new NotSupportedException(BeamOnLoadDiagnostics.UnknownImport);

        return new(module, function, arity);
    }

    internal static bool IsSourceLoadNif(BeamExternalImport import)
        => import.Module == ErlangModuleNames.Module && import.Function == SourceLoadNifNames.Function && import.Arity == 2;

    internal static bool IsNativePrimitive(BeamExternalImport import)
        => NativeBuiltinCatalog.Contains(import.Module, import.Function, import.Arity);

    internal static ValueTask<Term> Invoke(
        ProcessContext context,
        BeamExternalImport import,
        Term[] arguments,
        bool captureMissingTarget = false
    )
    {
        if (IsSourceLoadNif(import))
            return context.Runtime.Modules.InvokeSourceLoadNif(context, arguments);
        if (import.Module == ErlangModuleNames.Module && import.Function == BeamLoaderNames.CallOnLoad && import.Arity == 1)
            return context.Runtime.Modules.InvokeLoaderOnLoad(context, arguments[0]);
        if (import.Module == BeamOnLoadNames.CodeModule && import.Function == BeamOnLoadNames.IsLoaded && import.Arity == 1)
        {
            if (arguments[0] is not Atom module)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Term result = context.Runtime.Modules.IsModuleLoaded(module.Name)
                ? Term.Tuple(Term.A(BeamOnLoadNames.File), Cons.From(BeamOnLoadNames.InMemory.Select(character => Term.I(character))))
                : Term.A(ErlangBooleanAtoms.False);

            return ValueTask.FromResult(result);
        }

        if (captureMissingTarget)
            return ErlangApplyDispatch.InvokeTarget(
                context,
                import.Module,
                import.Function,
                arguments
            );

        return context.Runtime.Modules.Call(
            context,
            import.Module,
            import.Function,
            arguments
        );
    }
}
