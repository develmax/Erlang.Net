using Erlang;
using Erlang.Compiler;
using Erlang.Tests;

internal static class EtfHostChecks
{
    public static async Task ZeroWordReferences()
    {
        await using var runtime = new ProcessRuntime();
        await Execute(
            runtime,
            async context =>
        {
            foreach (byte[] packet in new byte[][]
            {
                [131, 90, 0, 0, 119, 1, 110, 0, 0, 0, 0],
                [131, 114, 0, 0, 119, 1, 110, 0]
            })
                foreach (byte[] suffix in new byte[][] { [], [0, 0, 0, 0], [255, 255, 255, 255] })
                {
                    byte[] bytes = packet.Concat(suffix).ToArray();
                    try
                    {
                        ExternalTermFormat.Decode(bytes);
                        throw new InvalidDataException(EtfCheckDiagnostics.ZeroWordReference);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))) { }
                    try
                    {
                        await runtime.Modules.Call(
                            context,
                            "erlang",
                            "binary_to_term",
                            new BitString(bytes)
                        );
                        throw new InvalidDataException(EtfCheckDiagnostics.ZeroWordReference);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))) { }
                }

            Term valid = await runtime.Modules.Call(
                context,
                "erlang",
                "binary_to_term",
                new BitString(new byte[] { 131, 90, 0, 1, 119, 1, 110, 0, 0, 0, 0, 255, 255, 255, 255 })
            );
            Require(
                valid is ReferenceTerm reference && reference.Words.Count == 1 && reference.Words[0] == uint.MaxValue,
                EtfCheckDiagnostics.ZeroWordReference
            );
        }
        );
    }

    public static async Task IoVec()
    {
        await using var runtime = new ProcessRuntime();
        new Parser("-module(etf_host). -export([make/1]). make(Value)->fun()->Value end.").ParseModule().Register(runtime.Modules);
        await Execute(
            runtime,
            async context =>
        {
            var original = new BitString(Enumerable.Range(0, 10000).Select(index => (byte)index).ToArray());
            var function = (FunctionTerm)await runtime.Modules.Call(
                context,
                "etf_host",
                "make",
                original
            );
            foreach (Term value in new Term[] { original, Term.Tuple(original, original), function, new BitString(new byte[] { 255, 128 }, 9) })
                foreach (int minor in new[] { 0, 1, 2 })
                    foreach (int compression in new[] { 0, 1, 6, 9 })
                        foreach (bool local in new[] { false, true })
                        {
                            var options = new EtfEncodingOptions { MinorVersion = minor, CompressionLevel = compression, Local = local, LocalNode = runtime.Node, FunctionOwner = runtime.InitializationPid, LocalSeed = 123 };
                            byte[] encoded = ExternalTermFormat.Encode(value, options);
                            var fragments = ExternalTermFormat.EncodeIoVec(value, options);
                            using var flattened = new MemoryStream();
                            foreach (BitString fragment in fragments) fragment.WriteTo(flattened);
                            Require(encoded.AsSpan().SequenceEqual(flattened.ToArray()), EtfCheckDiagnostics.IoVecBytes);
                            Require(
                                ExternalTermFormat.EncodedSize(value, options) == ExternalTermFormat.Encode(value, options with { CompressionLevel = 0 }).Length,
                                EtfCheckDiagnostics.Size
                            );
                            if (compression == 0 && !local && (ReferenceEquals(value, original) || ReferenceEquals(value, function)))
                            {
                                // A public make/1 call may detach its captured input.
                                // ETF must borrow the actual supplied function environment,
                                // while direct binary input must retain its exact original.
                                BitString expectedBorrowed = ReferenceEquals(value, function)
                                    ? (BitString)function.Environment[0] : original;
                                Require(expectedBorrowed.Equals(original), EtfCheckDiagnostics.BorrowedBinary);
                                Require(fragments.Any(fragment => ReferenceEquals(fragment, expectedBorrowed)), EtfCheckDiagnostics.BorrowedBinary);
                            }
                            foreach (BitString fragment in fragments)
                            {
                                byte[] copy = fragment.ToArray();
                                if (copy.Length != 0) copy[0] ^= 255;
                            }
                            using var again = new MemoryStream();
                            foreach (BitString fragment in fragments) fragment.WriteTo(again);
                            Require(encoded.AsSpan().SequenceEqual(again.ToArray()), EtfCheckDiagnostics.ImmutableFragment);
                        }
        }
        );
    }

    public static void CompressionBoundary()
    {
        var random = new Random(2461);
        foreach (int length in new[] { 1, 100, 1000, 10000, 100000 })
            foreach (int level in Enumerable.Range(0, 10).Prepend(-1))
                foreach (bool compressible in new[] { false, true })
                {
                    byte[] bytes = new byte[length];
                    if (!compressible) random.NextBytes(bytes);
                    var term = new BitString(bytes);
                    byte[] encoded = ExternalTermFormat.Encode(term, new EtfEncodingOptions { CompressionLevel = level });
                    var decoded = ExternalTermFormat.DecodeOne(encoded.Concat(new byte[] { 131, 97, 42 }).ToArray(), new EtfDecodeOptions());
                    Require(term.Equals(decoded.Term) && decoded.Used == encoded.Length, EtfCheckDiagnostics.CompressedBoundary);
                    try
                    {
                        ExternalTermFormat.Decode(encoded.Concat(new byte[] { 131, 97, 42 }).ToArray());
                        throw new InvalidDataException(EtfCheckDiagnostics.StrictTrailing);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))) { }
                    try
                    {
                        ExternalTermFormat.DecodeOne(encoded[..^1], new EtfDecodeOptions());
                        throw new InvalidDataException(EtfCheckDiagnostics.TruncatedCompression);
                    }
                    catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))) { }
                }
    }

    public static async Task FunctionsAndLocalDomains()
    {
        await using var origin = new ProcessRuntime();
        await using var target = new ProcessRuntime();
        const string source = "-module(etf_env). -export([make/1]). make(Value)->fun()->Value end.";
        new Parser(source).ParseModule().Register(origin.Modules);
        new Parser(source).ParseModule().Register(target.Modules);
        byte[]? transfer = null, localTransfer = null;
        await Execute(
            origin,
            async context =>
        {
            var function = (FunctionTerm)await origin.Modules.Call(
                context,
                "etf_env",
                "make",
                Term.I(42)
            );
            var changed = new FunctionTerm(
                function.LocalIdentity!,
                0,
                [Term.I(43)],
                (_, _) => ValueTask.FromResult<Term>(Term.I(-1))
            );
            transfer = ((BitString)await origin.Modules.Call(
                context,
                ErlangModuleNames.Module,
                "term_to_binary",
                changed
            )).ToArray();
            var restored = (FunctionTerm)await origin.Modules.Call(
                context,
                ErlangModuleNames.Module,
                "binary_to_term",
                new BitString(transfer)
            );
            Require(Term.I(43).Equals(await restored.Invoke(context, [])), EtfCheckDiagnostics.DecodedEnvironment);
            localTransfer = ((BitString)await origin.Modules.Call(
                context,
                ErlangModuleNames.Module,
                "term_to_binary",
                Term.Tuple(context.Self, origin.MakeReference()),
                Term.List(Term.A("local"))
            )).ToArray();
            Require(
                (await origin.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    "binary_to_term",
                    new BitString(localTransfer)
                )) is TupleTerm,
                EtfCheckDiagnostics.LocalDomain
            );
        }
        );
        await Execute(
            target,
            async context =>
        {
            await target.Modules.Call(
                context,
                "etf_env",
                "make",
                Term.I(99)
            );
            var restored = (FunctionTerm)await target.Modules.Call(
                context,
                ErlangModuleNames.Module,
                "binary_to_term",
                new BitString(transfer!)
            );
            Require(Term.I(43).Equals(await restored.Invoke(context, [])), EtfCheckDiagnostics.TargetEnvironment);
            try
            {
                await target.Modules.Call(
                    context,
                    ErlangModuleNames.Module,
                    "binary_to_term",
                    new BitString(localTransfer!)
                );
                throw new InvalidDataException(EtfCheckDiagnostics.OtherLocalDomain);
            }
            catch (ErlangException exception) when (exception.Reason.Equals(Term.A(ErlangErrorReasons.BadArgument))) { }
        }
        );
    }

    public static async Task References()
    {
        var words = new uint[] { 42, 0, 0, 0, 0 };
        var padded = new ReferenceTerm("foreign", words, 7);
        words[0] = 99;
        var shortReference = new ReferenceTerm("foreign", 42, 7);
        Require(
            padded.Equals(shortReference) && padded.GetHashCode() == shortReference.GetHashCode(),
            EtfCheckDiagnostics.ReferenceEquality
        );
        try
        {
            ((IList<uint>)padded.Words)[0] = 99;
            throw new InvalidDataException(EtfCheckDiagnostics.ReferenceMutable);
        }
        catch (NotSupportedException) { }
        var longReference = new ReferenceTerm("foreign", new uint[] { 1, 2, 3, 4, 5 }, 9);
        Require(
            longReference.Equals(ExternalTermFormat.Decode(ExternalTermFormat.Encode(longReference))),
            EtfCheckDiagnostics.ReferenceRoundtrip
        );
        try
        {
            _ = longReference.Id;
            throw new InvalidDataException(EtfCheckDiagnostics.ReferenceTruncation);
        }
        catch (OverflowException) { }
        await using var runtime = new ProcessRuntime();
        var allocated = new HashSet<Term>();
        for (int index = 0; index < 300000; index++)
        {
            ReferenceTerm reference = runtime.MakeReference();
            Require(
                reference.Words.Count == ReferenceIdentityLayout.LocalWords && reference.Words[0] <= ReferenceIdentityLayout.LocalFirstWordMask && allocated.Add(reference),
                EtfCheckDiagnostics.ReferenceAllocation
            );
        }
    }

    private static async Task Execute(ProcessRuntime runtime, Func<ProcessContext, Task> body)
    {
        Exception? failure = null;
        var process = runtime.Spawn(async context =>
        {
            try
            {
                await body(context);
            }
            catch (Exception exception)
            {
                failure = exception;
                throw;
            }

            return Nil.Value;
        });
        await process.Completion.WaitAsync(TimeSpan.FromSeconds(30));
        if (failure is not null) throw failure;
    }

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidDataException(message);
    }
}
