namespace Erlang;

internal static class BeamApplyExecution
{
    internal static ValueTask<Term> Invoke(
        ProcessContext context,
        BeamLinkedCode code,
        BeamRegisterState registers,
        int arity
    )
    {
        var (target, arguments) = Prepare(
            context,
            code,
            registers,
            arity
        );

        return BeamExternalImports.Invoke(
            context,
            target,
            arguments,
            captureMissingTarget: true
        );
    }

    internal static (BeamExternalImport Target, Term[] Arguments) Prepare(
        ProcessContext context,
        BeamLinkedCode code,
        BeamRegisterState registers,
        int arity
    )
    {
        Term[] arguments = Enumerable.Range(0, arity).Select(registers.Read).ToArray();
        Term module = registers.Read(arity);
        Term function = registers.Read(arity + BeamApplyNames.FunctionOffset);
        if (module is not Atom moduleAtom || function is not Atom functionAtom)
            throw RuntimeExceptionCapture.Call(
                context,
                new ErlangException(ErlangErrorReasons.BadArgument),
                context.CaptureExecutionChain(),
                ErlangModuleNames.Module,
                ErlangBasicBifNames.Apply,
                [module, function, Cons.From(arguments)],
                true
            );
        BeamExternalImport target = BeamExternalImports.Resolve(
            code,
            moduleAtom.Name,
            functionAtom.Name,
            arity
        );

        return (target, arguments);
    }
}
