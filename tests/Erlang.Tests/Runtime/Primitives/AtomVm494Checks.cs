using Erlang.Differential;

namespace Erlang.Tests;

internal static class AtomVm494Checks
{
    internal static async Task Isolation()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        string name = AtomVm494Names.Prefix + Guid.NewGuid().ToString();
        int hostCount = AtomNameRegistry.Count;
        int otherCount = second.AtomCount;
        using (first.EnterAtomScope())
        {
            int before = AtomNameRegistry.Count;
            Term.A(name);
            await Task.Yield();
            Check(AtomNameRegistry.Exists(name) && first.AtomCount == before + 1);
            using (second.EnterAtomScope())
            {
                Check(!AtomNameRegistry.Exists(name));
                await Task.Yield();
                Check(!AtomNameRegistry.Exists(name));
            }
            Check(AtomNameRegistry.Exists(name));
        }
        Check(!AtomNameRegistry.Exists(name) && AtomNameRegistry.Count == hostCount);
        Check(second.AtomCount == otherCount);
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = first.Spawn(async _ =>
        {
            await Task.Yield();
            Check(AtomNameRegistry.Exists(name));
            completion.SetResult();

            return Term.A(AtomVm494Names.Ok);
        });
        await completion.Task.WaitAsync(TimeSpan.FromSeconds(AtomVm494Names.TimeoutSeconds));
        await actor.Completion;
    }

    internal static async Task Import()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        Term imported;
        using (first.EnterAtomScope())
            imported = Term.Tuple(Term.A(AtomVm494Names.Imported));
        using (second.EnterAtomScope())
            Check(!AtomNameRegistry.Exists(AtomVm494Names.Imported));
        second.Modules.Register(
            AtomVm494Names.Erlang,
            AtomVm494Names.Identity,
            1,
            (_, arguments) => ValueTask.FromResult(arguments[0])
        );
        await Execute(second, context => second.Modules.Call(
            context,
            AtomVm494Names.Erlang,
            AtomVm494Names.Identity,
            imported
        ));
        using (second.EnterAtomScope())
            Check(AtomNameRegistry.Exists(AtomVm494Names.Imported));
    }

    internal static async Task BinaryIdentity()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        Term atom = Term.A(AtomVm494Names.Imported);
        Term one = await Execute(first, context => first.Modules.Call(
            context,
            AtomVm494Names.Erlang,
            AtomVm494Names.AtomToBinary,
            atom
        ));
        Term repeat = await Execute(first, context => first.Modules.Call(
            context,
            AtomVm494Names.Erlang,
            AtomVm494Names.AtomToBinary,
            atom
        ));
        Term other = await Execute(second, context => second.Modules.Call(
            context,
            AtomVm494Names.Erlang,
            AtomVm494Names.AtomToBinary,
            atom
        ));
        Check(ReferenceEquals(one, repeat) && !ReferenceEquals(one, other) && one.Equals(other));
    }

    internal static async Task SafeDecode()
    {
        await using var first = new ProcessRuntime();
        await using var second = new ProcessRuntime();
        string name = AtomVm494Names.Prefix + Guid.NewGuid();
        byte[] data = ExternalTermFormat.Encode(Term.A(name));
        bool rejected = false;
        try
        {
            first.ImportExternalTerm(data, new EtfDecodeOptions { Safe = true });
        }
        catch (ErlangException error) when (error.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument)))
        {
            rejected = true;
        }
        Check(rejected);
        first.ImportExternalTerm(data);
        Check(first.ImportExternalTerm(data, new EtfDecodeOptions { Safe = true }).Term.Equals(Term.A(name)));
        using (second.EnterAtomScope())
            Check(!AtomNameRegistry.Exists(name));
    }

    internal static Task Exhaustion() => Exhaustion(AtomVm494Names.ListToAtom, false);

    internal static Task BinaryExhaustion() => Exhaustion(AtomVm494Names.BinaryToAtom, false);

    internal static Task EncodedExhaustion() => Exhaustion(AtomVm494Names.BinaryToAtom, true);

    private static async Task Exhaustion(string function, bool encoded)
    {
        await using var first = new ProcessRuntime(
            ProcessNodeDefaults.Local,
            null,
            AtomVm494Names.Processes,
            null,
            2,
            new ProcessRuntimeOptions { MaximumAtoms = AtomVm494Names.Capacity }
        );
        await using var survivor = new ProcessRuntime();
        bool caught = false;
        bool unwound = false;
        int guestCatch = 0;
        int guestAfter = 0;
        using var module = GeneratedModuleCompiler.Compile(AtomVm494Names.Source, AtomVm494Names.SourceName);
        module.Definition.Register(first.Modules);
        first.Modules.Register(
            AtomVm494Names.Erlang,
            AtomVm494Names.CatchMarker,
            0,
            (_, _) =>
        {
            guestCatch++;

            return ValueTask.FromResult<Term>(Term.A(AtomVm494Names.Ok));
        }
        );
        first.Modules.Register(
            AtomVm494Names.Erlang,
            AtomVm494Names.AfterMarker,
            0,
            (_, _) =>
        {
            guestAfter++;

            return ValueTask.FromResult<Term>(Term.A(AtomVm494Names.Ok));
        }
        );
        int before = first.AtomCount;
        string text = AtomVm494Names.Last;
        Term argument = function == AtomVm494Names.ListToAtom ? Term.String(text) : new BitString(System.Text.Encoding.UTF8.GetBytes(text));
        Term[] arguments = encoded ? [argument, Term.A(AtomVm494Names.Utf8)] : [argument];
        Term[] warmArguments = arguments.ToArray();
        warmArguments[0] = function == AtomVm494Names.ListToAtom
            ? Term.String(AtomVm494Names.Warm)
            : new BitString(System.Text.Encoding.UTF8.GetBytes(AtomVm494Names.Warm));
        await Execute(
            first,
            context => first.Modules.Call(
                context,
                AtomVm494Names.Module,
                AtomVm494Names.Run,
                Term.A(function),
                Cons.From(warmArguments)
            )
        );
        Check(guestCatch == 0 && guestAfter == 1);
        guestAfter = 0;
        first.Spawn(context =>
        {
            try
            {
                for (int index = 0; first.AtomCount < first.AtomLimit; index++)
                    Term.A(AtomVm494Names.ExhaustedPrefix + index);
                first.Modules.Call(
                    context,
                    AtomVm494Names.Module,
                    AtomVm494Names.Run,
                    Term.A(function),
                    Cons.From(arguments)
                ).GetAwaiter().GetResult();
            }
            catch (ErlangException)
            {
                caught = true;
            }
            finally
            {
                unwound = true;
            }

            return ValueTask.FromResult<Term>(Term.A(AtomVm494Names.Ok));
        });
        RuntimeHaltResult result = await first.HaltCompletion.WaitAsync(TimeSpan.FromSeconds(AtomVm494Names.TimeoutSeconds));
        await first.HaltQuiescence.WaitAsync(TimeSpan.FromSeconds(AtomVm494Names.TimeoutSeconds));
        Check(before < first.AtomLimit && first.AtomCount == first.AtomLimit);
        Check(result.Status == 1 && !caught && unwound && survivor.Lifecycle == RuntimeLifecycleState.Running);
        Check(guestCatch == 0 && guestAfter == 0);
        Term live = await Execute(survivor, _ => ValueTask.FromResult<Term>(Term.A(AtomVm494Names.Ok)));
        Check(live.Equals(Term.A(AtomVm494Names.Ok)));
    }

    private static async Task<Term> Execute(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> action)
    {
        var result = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle actor = runtime.Spawn(async context =>
        {
            try
            {
                result.SetResult(await action(context));
            }
            catch (Exception error)
            {
                result.SetException(error);
                throw;
            }

            return Term.A(AtomVm494Names.Ok);
        });
        Term value = await result.Task.WaitAsync(TimeSpan.FromSeconds(AtomVm494Names.TimeoutSeconds));
        await actor.Completion;

        return value;
    }

    private static void Check(bool condition)
    {
        if (!condition)
            throw new InvalidOperationException(AtomVm494Names.Failure);
    }
}
