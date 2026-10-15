namespace Erlang;

internal static class BeamExternalImports
{
    internal static BeamExternalImport Resolve(BeamLinkedCode code, int index)
    {
        BeamImport source = code.Structure.Imports[index];
        string module = code.Structure.Atoms[source.ModuleAtomIndex]!;
        string function = code.Structure.Atoms[source.FunctionAtomIndex]!;
        bool supported = module == ErlangModuleNames.Module && (
            source.Arity == 0 && function == ErlangModuleNames.Self
            || source.Arity == 1 && function is ErlangModuleNames.WhereIs or ErlangModuleNames.Error or BeamLoaderNames.CallOnLoad
            || source.Arity == 2 && function is BeamRegisterNames.Add or BeamRegisterNames.Subtract or ErlangModuleNames.Put
        ) || module == BeamOnLoadNames.CodeModule && function == BeamOnLoadNames.IsLoaded && source.Arity == 1
            || module == BeamRegisterNames.OsModule && function == BeamRegisterNames.GetEnvironment && source.Arity == 1;
        if (!supported)
            throw new NotSupportedException(BeamOnLoadDiagnostics.UnknownImport);

        return new(module, function, source.Arity);
    }

    internal static ValueTask<Term> Invoke(ProcessContext context, BeamExternalImport import, Term[] arguments)
    {
        if (import.Module == ErlangModuleNames.Module && import.Function == BeamLoaderNames.CallOnLoad && import.Arity == 1)
            return context.Runtime.Modules.InvokeLoaderOnLoad(context, arguments[0]);
        if (import.Module == BeamOnLoadNames.CodeModule)
        {
            if (arguments[0] is not Atom module)
                throw new ErlangException(ErlangErrorReasons.BadArgument);
            Term result = context.Runtime.Modules.IsModuleLoaded(module.Name)
                ? Term.Tuple(Term.A(BeamOnLoadNames.File), Cons.From(BeamOnLoadNames.InMemory.Select(character => Term.I(character))))
                : Term.A(ErlangBooleanAtoms.False);

            return ValueTask.FromResult(result);
        }

        return context.Runtime.Modules.Call(
            context,
            import.Module,
            import.Function,
            arguments
        );
    }
}
