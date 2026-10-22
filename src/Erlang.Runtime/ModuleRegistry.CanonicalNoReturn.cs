namespace Erlang;

public sealed partial class ModuleRegistry
{
    private sealed record NoReturnContract(
        (string Module, string Function, int Arity) Signature,
        ModuleCodeImage Image
    );

    private readonly System.Runtime.CompilerServices.ConditionalWeakTable<ErlangFunction, NoReturnContract> noReturnContracts = new();

    internal void SealCanonicalNoReturnContracts()
    {
        // Call only from CoreModules.RegisterCode immediately after ErlangErrorRegistrations.Register(r).
        (string Module, string Function, int Arity)[] signatures =
        [
            (ErlangModuleNames.Module, ErlangModuleNames.Error, 1),
            (ErlangModuleNames.Module, ErlangModuleNames.Error, 2),
            (ErlangModuleNames.Module, ErlangModuleNames.Error, 3),
            (ErlangModuleNames.Module, ErlangModuleNames.Throw, 1),
            (ErlangModuleNames.Module, ErlangModuleNames.Exit, 1),
            // Conditional contract: only valid class plus empty trace cannot return.
            (ErlangModuleNames.Module, ErlangRaiseBifNames.Raise, BeamRaiseEmptyTraceNames.Arity)
        ];
        lock (gate)
            foreach (var signature in signatures)
                noReturnContracts.Add(functions[signature], new(signature, codeImages[signature.Module]));
    }

    internal bool TryInvokeCanonicalNoReturn(
        ProcessContext context,
        string module,
        string function,
        Term[] arguments,
        out ValueTask<Term> pending
    )
    {
        pending = default;
        if (function == ErlangRaiseBifNames.Raise
            && (arguments.Length != BeamRaiseEmptyTraceNames.Arity
                || arguments[BeamTryParameters.RawRaiseClassRegister] is not Atom
                { Name: ErlangExceptionClasses.Error or ErlangExceptionClasses.Exit or ErlangExceptionClasses.Throw }
                || arguments[BeamTryParameters.RawRaiseTraceRegister] is not Nil))
            return false;
        context.Runtime.ThrowIfHalted();
        ErlangFunction? body;
        ModuleCodeImage image;
        IDisposable code;
        lock (gate)
        {
            var signature = (module, function, arguments.Length);
            if (!functions.TryGetValue(signature, out body)
                || sourceOwners.ContainsKey(signature)
                || !noReturnContracts.TryGetValue(body, out NoReturnContract? contract)
                || contract.Signature != signature
                || !ReferenceEquals(contract.Image, codeImages.GetValueOrDefault(module))
                || !runtimeErrorBindings.TryGetValue(signature, out RuntimeErrorBinding? binding)
                || !binding.Native || !ReferenceEquals(binding.Body, body) || !ReferenceEquals(binding.Image, contract.Image))
                return false;
            image = contract.Image;
            code = context.EnterSourceCode(image, body);
        }
        // Never take a runtime lock or invoke mutable host code under the module gate.
        try
        {
            foreach (Term argument in arguments)
                context.Runtime.ObserveIncomingTerm(argument);
        }
        catch
        {
            code.Dispose();
            context.RestoreSourceCodeFlow(code);
            throw;
        }
        ErlangFunction selected = body;
        pending = InvokeAdmittedSelection(
            context,
            module,
            function,
            arguments,
            (caller, values) => RuntimeExceptionCapture.InvokeNative(
                caller,
                selected,
                module,
                function,
                values
            ),
            image,
            code,
            handler: false,
            hostErrorHandler: false
        );

        return true;
    }
}
