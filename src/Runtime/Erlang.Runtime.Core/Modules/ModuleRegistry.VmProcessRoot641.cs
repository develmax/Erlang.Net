namespace Erlang;

public sealed partial class ModuleRegistry
{
    internal ValueTask<Term> CallVmMfaRoot641(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        RuntimeProcessPayload641? resumedArguments641 = null
    )
    {
        bool genuine;
        lock (gate)
        {
            var key = (module, function, arguments.Count);
            genuine = functions.TryGetValue(key, out var body)
                && SelectedOwnedBinding507(key, body) is OwnedBindingKind507.CompilerGuest or OwnedBindingKind507.PreparedGuest;
        }
        if (!genuine || !context.Runtime.OwnedTermsPilot506)
            return CallLegacyVmRoot641(
                context,
                module,
                function,
                arguments,
                resumedArguments641
            );

        return ExecuteVmRoot641(
            context,
            arguments,
            resumedArguments641,
            (rootedArguments, _) => CallInto506(
                context,
                module,
                function,
                rootedArguments.ToArray()
            ),
            null
        );
    }

    internal ValueTask<Term> CallVmCallbackRoot641(
        ProcessContext context,
        Term callback,
        IReadOnlyList<Term> arguments,
        RuntimeProcessPayload641? resumedArguments641 = null
    )
    {
        if (!context.Runtime.OwnedTermsPilot506)
            return CallLegacyVmRoot641(
                context,
                ErlangModuleNames.Module,
                ErlangBasicBifNames.Apply,
                [callback, Cons.From(arguments)],
                resumedArguments641
            );

        if (callback is TupleTerm { Items: [Atom module, Atom function] })
            return CallVmMfaRoot641(
                context,
                module.Name,
                function.Name,
                arguments,
                resumedArguments641
            );
        if (callback is FunctionTerm callable
            && RuntimeStoredFunctionRecipes.TryCaptureTransfer506(context.Runtime, callable, out var recipe)
            && recipe is ICanonicalGuestRecipe507 admitted && admitted.AdmittedTo507(context.Runtime))
            return ExecuteVmRoot641(
                context,
                arguments,
                resumedArguments641,
                (rootedArguments, rootedCallable) => context.InvokeGuestFunction507(rootedCallable!, rootedArguments),
                callable
            );

        return CallLegacyVmRoot641(
            context,
            ErlangModuleNames.Module,
            ErlangBasicBifNames.Apply,
            [callback, Cons.From(arguments)],
            resumedArguments641
        );
    }

    private async ValueTask<Term> CallLegacyVmRoot641(
        ProcessContext context,
        string module,
        string function,
        IReadOnlyList<Term> arguments,
        RuntimeProcessPayload641? resumed
    )
    {
        try
        {
            _ = await Call(
                context,
                module,
                function,
                arguments.ToArray()
            ).ConfigureAwait(false);

            return Nil.Value;
        }
        finally
        {
            resumed?.Close641();
        }
    }

    private async ValueTask<Term> ExecuteVmRoot641(
        ProcessContext context,
        IReadOnlyList<Term> arguments,
        RuntimeProcessPayload641? resumed,
        Func<IReadOnlyList<Term>, FunctionTerm?, ValueTask<Term>> body,
        FunctionTerm? callable
    )
    {
        RuntimeProcessPayload641? payload = null;
        ProcessVmFailure641? failure = null;
        OwnedTermInvocation506? root = null;
        Exception? flow = null;
        bool transferred = false;
        bool previous = context.VmSourceRoot641;
        try
        {
            if (!ReferenceEquals(context.Runtime.Modules, this) || context.CurrentOwned506 is not null
                || !RuntimeActorExecutionTurn.IsOwnedBy(context))
                throw new InvalidOperationException(RuntimeProcessPayloadNames641.Invalid);
            payload = RuntimeProcessPayload641.Reserve641(context.Runtime, context);
            failure = new ProcessVmFailure641(context, payload); // BEFORE native capture
            root = context.EnterOwnedInvocation506(arguments)!;
            FunctionTerm? rootedCallable = callable is null ? null : (FunctionTerm)root.HoldGuestResult507(callable);
            // New real frame owns resumed inputs before the old producer ends.
            resumed?.Close641();
            resumed = null;
            context.VmSourceRoot641 = true;
            try
            {
                _ = await body(root.Arguments, rootedCallable).ConfigureAwait(false);
            }
            catch (ErlangException error)
            {
                using var atoms = new AtomNameScope(context.Runtime.AtomNames);
                Term reason = error.ExceptionClass == ErlangExceptionClasses.Exit ? error.Reason
                    : Term.Tuple(
                        error.ExceptionClass == ErlangExceptionClasses.Throw
                        ? Term.Tuple(Term.A(ErlangErrorReasons.NoCatch), error.Reason) : error.Reason,
                        error.StackTraceTerm
                    );
                // Nested EXISTING Transfer retained these terms into root.
                payload.Capture641(reason);
                flow = failure;
                transferred = true;
            }
            catch (ProcessHibernateTransfer hibernate) when (hibernate.GuestArguments641)
            {
                payload.Capture641(Term.Tuple(hibernate.Arguments.ToArray()));
                hibernate.SetVmArguments641(((TupleTerm)payload.Value641).Items, payload);
                flow = hibernate;
                transferred = true;
            }
        }
        catch (Exception error)
        {
            flow = error;
        }
        finally
        {
            context.VmSourceRoot641 = previous;
            resumed?.Close641();
            try
            {
                root?.Dispose();
            }
            catch (Exception teardown)
            {
                if (flow is ProcessVmFailure641 captured) captured.Teardown641 = teardown;
                else if (flow is ProcessHibernateTransfer hibernate && transferred) hibernate.Teardown641 = teardown;
                else flow = flow is null ? teardown : new AggregateException(flow, teardown);
            }
            if (!transferred) payload?.Close641();
        }
        if (flow is not null) System.Runtime.ExceptionServices.ExceptionDispatchInfo.Capture(flow).Throw();

        return Nil.Value; // actual spawned body result is discarded, no Host export
    }
}
