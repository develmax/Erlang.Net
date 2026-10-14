using Erlang;

namespace Erlang.Tests;

public static class BeamHostChecks
{
    public static async Task Run()
    {
        byte[] original = Resource(BeamHostCheckNames.Constant + BeamHostCheckNames.BeamExtension);
        BeamModule constant = BeamModule.Prepare(original);
        original[0] = BeamHostCheckNames.InvalidFormFirstByte;
        Require(constant.Name == BeamHostCheckNames.Constant && constant.Supports(BeamHostCheckNames.Run, BeamHostCheckNames.NoArgumentsArity));
        Require(constant.Exports.Any(value => value.Name == BeamHostCheckNames.ModuleInfo));
        Require(!constant.Supports(BeamHostCheckNames.ModuleInfo, BeamHostCheckNames.NoArgumentsArity) && !constant.Supports(BeamHostCheckNames.MissingExport, BeamHostCheckNames.NoArgumentsArity));
        Require(!constant.HasOnLoad);
        Require(constant.Exports is System.Collections.IList list && list.IsReadOnly);
        await using var runtime = new ProcessRuntime();
        Term expectedConstant = Term.Tuple(
            Term.A(BeamHostCheckNames.ResultTag),
            Term.I(BeamHostCheckNames.ExpectedConstantNumber),
            Term.List(Term.A(BeamHostCheckNames.ConstantFirstAtom), Term.A(BeamHostCheckNames.ConstantSecondAtom))
        );
        var owners = Enumerable.Range(0, BeamHostCheckNames.ConcurrentOwners).Select(_ => Invoke(runtime, constant, BeamHostCheckNames.Run)).ToArray();
        foreach (Term result in await Task.WhenAll(owners))
        {
            Require(result.Equals(expectedConstant));
            NativeBytes(result, BeamHostCheckNames.Constant);
        }
        BeamModule add = BeamModule.Prepare(Resource(BeamHostCheckNames.Add + BeamHostCheckNames.BeamExtension));
        Term addResult = await Invoke(runtime, add, BeamHostCheckNames.Run);
        Require(addResult.Equals(Term.I(42)));
        NativeBytes(addResult, BeamHostCheckNames.Add);
        BeamModule local = BeamModule.Prepare(Resource(BeamHostCheckNames.Local + BeamHostCheckNames.BeamExtension));
        Term localResult = await Invoke(runtime, local, BeamHostCheckNames.Run);
        Require(localResult.Equals(Term.Tuple(
            Term.A(BeamHostCheckNames.ResultTag),
            Term.List(
                Term.I(BeamHostCheckNames.LocalListFirst),
                Term.I(BeamHostCheckNames.LocalListSecond),
                Term.I(BeamHostCheckNames.LocalListThird)
            )
        )));
        NativeBytes(localResult, BeamHostCheckNames.Local);
        BeamModule receive = BeamModule.Prepare(Resource(BeamHostCheckNames.Receive + BeamHostCheckNames.BeamExtension));
        Require(!receive.Supports(BeamHostCheckNames.Run, BeamHostCheckNames.NoArgumentsArity));
        BeamModule onLoad = BeamModule.Prepare(Resource(BeamHostCheckNames.OnLoad + BeamHostCheckNames.BeamExtension));
        Require(onLoad.HasOnLoad && !onLoad.Supports(BeamHostCheckNames.Run, BeamHostCheckNames.NoArgumentsArity));
        await Reject<NotSupportedException>(() => Invoke(runtime, constant, BeamHostCheckNames.ModuleInfo));
        await Reject<InvalidDataException>(() => Invoke(runtime, constant, BeamHostCheckNames.MissingExport));
        try
        {
            _ = BeamModule.Prepare(original);
            throw new InvalidOperationException(BeamHostCheckNames.InvalidMutableInput);
        }
        catch (InvalidDataException)
        {
        }
        var ownerContext = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle departed = runtime.Spawn(context =>
        {
            ownerContext.TrySetResult(context);

            return ValueTask.FromResult<Term>(Term.A(BeamHostCheckNames.ResultTag));
        });
        ProcessContext stopped = await ownerContext.Task;
        _ = await departed.Completion;
        await Reject<OperationCanceledException>(async () => await constant.Invoke(stopped, BeamHostCheckNames.Run));
    }

    private static async Task<Term> Invoke(ProcessRuntime runtime, BeamModule module, string function)
    {
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle owner = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await module.Invoke(context, function);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }
        });
        try
        {
            return await observed.Task;
        }
        finally
        {
            _ = await owner.Completion;
        }
    }

    private static void NativeBytes(Term value, string module)
    {
        byte[] expected = Resource(module + BeamHostCheckNames.ExpectedEtfExtension);
        byte[] actual = ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamHostCheckNames.ResultTag), value));
        Require(actual.AsSpan().SequenceEqual(expected));
    }

    private static byte[] Resource(string filename)
    {
        var assembly = typeof(BeamHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamHostCheckNames.ResourceSeparator + filename, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var bytes = new MemoryStream();
        stream.CopyTo(bytes);

        return bytes.ToArray();
    }

    private static async Task Reject<T>(Func<Task<Term>> body) where T : Exception
    {
        try
        {
            _ = await body();
        }
        catch (T)
        {
            return;
        }
        throw new InvalidOperationException(BeamHostCheckNames.UnexpectedInvocation);
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(BeamHostCheckNames.FailedContract);
        }
    }
}
