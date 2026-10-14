using System.Security.Cryptography;
using System.Buffers.Binary;
using System.Reflection;
using System.Text;
using Erlang;

namespace Erlang.Tests;

public static class BeamClosureHostChecks
{
    public static async Task Run()
    {
        byte[] bytes = Resource(BeamClosureHostNames.Module + BeamClosureHostNames.BeamExtension);
        Require(Convert.ToHexString(SHA256.HashData(bytes)) == BeamClosureHostNames.Hash);
        Require(Convert.ToHexString(Resource(BeamClosureHostNames.Module + BeamClosureHostNames.ActualEtfExtension)) == BeamClosureHostNames.NativeEtf);
        BeamModule image = BeamModule.Prepare(bytes);
        bytes[0] = 0;
        Require(image.Name == BeamClosureHostNames.Module && image.Supports(BeamClosureHostNames.Run, 0)
            && image.Supports(BeamClosureHostNames.Make, 1) && !image.Supports(BeamClosureHostNames.ModuleInfo, 0));
        await using var runtime = new ProcessRuntime();
        Require(!runtime.Modules.IsModuleLoaded(BeamClosureHostNames.Module));
        byte[] valid = Resource(BeamClosureHostNames.Module + BeamClosureHostNames.BeamExtension);
        BeamModule malformed = BeamModule.Prepare(AppendIncompleteLambdaTail(valid));
        _ = await Invoke(
            runtime,
            async context =>
        {
            FieldInfo counter = typeof(ProcessContext).GetField(BeamClosureHostNames.ReductionField, BindingFlags.Instance | BindingFlags.NonPublic)!;
            object before = counter.GetValue(context)!;
            try
            {
                await malformed.Invoke(context, BeamClosureHostNames.Run);
                throw new InvalidOperationException(BeamClosureHostNames.Invalid);
            }
            catch (InvalidDataException)
            {
                Require(before.Equals(counter.GetValue(context)));
            }

            return Term.A(BeamClosureHostNames.Done);
        }
        );
        foreach (string previous in BeamClosureHostNames.PreviousExecutions)
        {
            string fixture = $"beam_fixture_{previous}";
            BeamModule oldImage = BeamModule.Prepare(Resource(fixture + BeamClosureHostNames.BeamExtension));
            Term oldResult = await Invoke(runtime, context => oldImage.Invoke(context, BeamClosureHostNames.Run));
            Require(ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamClosureHostNames.Success), oldResult))
                .SequenceEqual(Resource(fixture + BeamClosureHostNames.ActualEtfExtension)));
        }
        Term expected = Term.Tuple(Term.A(BeamClosureHostNames.Success), Term.I(BeamClosureHostNames.Result), Term.A(BeamClosureHostNames.True));
        Term result = await Invoke(runtime, context => image.Invoke(context, BeamClosureHostNames.Run));
        Require(result.Equals(expected));
        Require(Convert.ToHexString(ExternalTermFormat.Encode(Term.Tuple(Term.A(BeamClosureHostNames.Success), result))) == BeamClosureHostNames.NativeEtf);
        FunctionTerm captured = (FunctionTerm)await Invoke(runtime, context => image.Invoke(context, BeamClosureHostNames.Make, Term.I(BeamClosureHostNames.Capture)));
        Require(captured.Arity == 1 && captured.Environment.Count == 1 && captured.Environment[0].Equals(Term.I(BeamClosureHostNames.Capture)));
        LocalFunctionIdentity identity = captured.LocalIdentity!;
        Require(identity.Module.Name == BeamClosureHostNames.Module && identity.Name!.Name == BeamClosureHostNames.Generated
            && identity.Index == 0 && identity.OldIndex == 0 && identity.OldUnique == BeamClosureHostNames.OriginalUnique
            && Convert.ToHexString(identity.Unique.ToArray()) == BeamClosureHostNames.Checksum);
        try
        {
            ((IList<Term>)captured.Environment)[0] = Term.I(BeamClosureHostNames.MutatedEnvironmentValue);
            throw new InvalidOperationException(BeamClosureHostNames.Invalid);
        }
        catch (NotSupportedException)
        {
        }
        FunctionTerm other = (FunctionTerm)await Invoke(runtime, context => image.Invoke(context, BeamClosureHostNames.Make, Term.I(BeamClosureHostNames.OtherCapture)));
        Term first = await Invoke(runtime, context => captured.Invoke(context, new[] { Term.I(BeamClosureHostNames.Argument) }));
        Term second = await Invoke(runtime, context => other.Invoke(context, new[] { Term.I(BeamClosureHostNames.Argument) }));
        Require(first.Equals(Term.I(BeamClosureHostNames.Result)) && second.Equals(Term.I(BeamClosureHostNames.OtherResult)));
        var owners = Enumerable.Range(0, BeamClosureHostNames.OwnerCount).Select(_ => Invoke(runtime,
            context => captured.Invoke(context, new[] { Term.I(BeamClosureHostNames.Argument) }))).ToArray();
        Require((await Task.WhenAll(owners)).All(value => value.Equals(Term.I(BeamClosureHostNames.Result))));
        Term badArity = await Invoke(
            runtime,
            async context =>
        {
            try
            {
                await captured.Invoke(context, Array.Empty<Term>());
            }
            catch (ErlangException error)
            {
                return error.Reason;
            }
            throw new InvalidOperationException(BeamClosureHostNames.Invalid);
        }
        );
        Require(badArity.Equals(Term.Tuple(Term.A(ErlangErrorReasons.BadArity), Term.Tuple(captured, Nil.Value))));
        var stoppedContext = new TaskCompletionSource<ProcessContext>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle exited = runtime.Spawn(context =>
        {
            stoppedContext.TrySetResult(context);

            return ValueTask.FromResult<Term>(Term.A(BeamClosureHostNames.Done));
        });
        ProcessContext stopped = await stoppedContext.Task;
        _ = await exited.Completion;
        try
        {
            await captured.Invoke(stopped, new[] { Term.I(BeamClosureHostNames.Argument) });
            throw new InvalidOperationException(BeamClosureHostNames.Invalid);
        }
        catch (OperationCanceledException)
        {
        }
        Require(!runtime.Modules.IsModuleLoaded(BeamClosureHostNames.Module));

    }

    private static async Task<Term> Invoke(ProcessRuntime runtime, Func<ProcessContext, ValueTask<Term>> invoke)
    {
        var observed = new TaskCompletionSource<Term>(TaskCreationOptions.RunContinuationsAsynchronously);
        ProcessHandle process = runtime.Spawn(async context =>
        {
            try
            {
                Term result = await invoke(context);
                observed.TrySetResult(result);

                return result;
            }
            catch (Exception error)
            {
                observed.TrySetException(error);
                throw;
            }
        });
        Term value = await observed.Task;
        _ = await process.Completion;

        return value;
    }

    private static byte[] AppendIncompleteLambdaTail(byte[] original)
    {
        for (int offset = BeamClosureMutationWire.FormHeaderBytes; offset < original.Length;)
        {
            int size = BinaryPrimitives.ReadInt32BigEndian(original.AsSpan(offset + BeamClosureMutationWire.LengthFieldOffset));
            int padded = (size + BeamClosureMutationWire.AlignmentMask) & ~BeamClosureMutationWire.AlignmentMask;
            if (Encoding.ASCII.GetString(original, offset, BeamClosureMutationWire.IdentifierBytes) == BeamClosureMutationWire.LambdaChunk)
            {
                Require(size == padded);
                int end = offset + BeamClosureMutationWire.ChunkHeaderBytes + size;
                byte[] mutated = [.. original[..end], .. new byte[BeamClosureMutationWire.AddedAlignedBytes], .. original[end..]];
                BinaryPrimitives.WriteInt32BigEndian(
                    mutated.AsSpan(BeamClosureMutationWire.LengthFieldOffset),
                    BinaryPrimitives.ReadInt32BigEndian(original.AsSpan(BeamClosureMutationWire.LengthFieldOffset)) + BeamClosureMutationWire.AddedAlignedBytes
                );
                BinaryPrimitives.WriteInt32BigEndian(mutated.AsSpan(offset + BeamClosureMutationWire.LengthFieldOffset), size + 1);

                return mutated;
            }
            offset += BeamClosureMutationWire.ChunkHeaderBytes + padded;
        }
        throw new InvalidOperationException(BeamClosureHostNames.Invalid);
    }

    private static byte[] Resource(string file)
    {
        Assembly assembly = typeof(BeamClosureHostChecks).Assembly;
        string name = assembly.GetManifestResourceNames().Single(value => value.EndsWith(BeamClosureHostNames.ResourceSeparator + file, StringComparison.Ordinal));
        using Stream stream = assembly.GetManifestResourceStream(name)!;
        using var output = new MemoryStream();
        stream.CopyTo(output);

        return output.ToArray();
    }

    private static void Require(bool condition)
    {
        if (!condition)
        {
            throw new InvalidOperationException(BeamClosureHostNames.Invalid);
        }
    }
}
