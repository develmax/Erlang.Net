namespace Erlang;

internal static class ErlangEtfRegistrations
{
    public static void Register(ModuleRegistry registry)
    {
        foreach (string name in new[] { ErlangEtfNames.Encode, ErlangEtfNames.IoVec, ErlangEtfNames.Size, ErlangEtfNames.Decode })
            foreach (int arity in new[] { 1, 2 })
                registry.Register(
                    ErlangModuleNames.Module,
                    name,
                    arity,
                    (context, arguments) => ValueTask.FromResult(Invoke(context, name, arguments))
                );
    }

    private static Term Invoke(ProcessContext context, string name, IReadOnlyList<Term> arguments)
    {
        Term options = arguments.Count == 2 ? arguments[1] : Nil.Value;
        if (name == ErlangEtfNames.Decode)
            return Decode(context, arguments[0], options);
        var encoding = Encoding(context, options);

        return name switch
        {
            ErlangEtfNames.Encode => BitString.FromOwnedBytes(ExternalTermFormat.Encode(arguments[0], encoding)),
            ErlangEtfNames.IoVec => encoding.CompressionLevel == ErlangEtfParameters.Uncompressed ? Cons.From(ExternalTermFormat.EncodeIoVec(arguments[0], encoding)) : BitString.FromOwnedBytes(ExternalTermFormat.Encode(arguments[0], encoding)),
            ErlangEtfNames.Size => Term.I(ExternalTermFormat.EncodedSize(arguments[0], encoding)),
            _ => throw new InvalidOperationException()
        };
    }

    private static EtfEncodingOptions Encoding(ProcessContext context, Term options)
    {
        var wire = context.Runtime.LocalIdentity.Snapshot;
        var result = new EtfEncodingOptions
        {
            LocalNode = wire.Node,
            LocalCreation = wire.Creation,
            NodeIdentity = context.Runtime.LocalIdentity,
            NodeSnapshot = wire,
            LocalSeed = context.Runtime.LocalTermSeed,
            FunctionOwner = context.Runtime.InitializationPid
        };
        foreach (Term value in Cons.Items(options))
        {
            if (value is Atom atom)
            {
                result = atom.Name switch
                {
                    ErlangEtfNames.Compressed => result with { CompressionLevel = ErlangEtfParameters.DefaultCompression },
                    ErlangEtfNames.Deterministic => result with { Deterministic = true },
                    ErlangEtfNames.Local => result with { Local = true },
                    _ => throw new ErlangException(ErlangErrorReasons.BadArgument)
                };
            }
            else if (value is TupleTerm { Items.Count: 2 } tuple && tuple.Items[0] is Atom key && tuple.Items[1] is Integer integer)
            {
                if (key.Name == ErlangEtfNames.Compressed && integer.Value >= ErlangEtfParameters.MinimumCompression && integer.Value <= ErlangEtfParameters.MaximumCompression)
                    result = result with { CompressionLevel = (int)integer.Value };
                else if (key.Name == ErlangEtfNames.MinorVersion && integer.Value >= ErlangEtfParameters.MinimumMinorVersion && integer.Value <= ErlangEtfParameters.MaximumMinorVersion)
                    result = result with { MinorVersion = (int)integer.Value };
                else
                    throw new ErlangException(ErlangErrorReasons.BadArgument);
            }
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }

        return result;
    }

    private static Term Decode(ProcessContext context, Term value, Term options)
    {
        bool safe = false, used = false;
        foreach (Term option in Cons.Items(options))
        {
            if (option is Atom { Name: ErlangEtfNames.Safe })
                safe = true;
            else if (option is Atom { Name: ErlangEtfNames.Used })
                used = true;
            else
                throw new ErlangException(ErlangErrorReasons.BadArgument);
        }
        if (value is not BitString { IsBinary: true } binary)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        var wire = context.Runtime.LocalIdentity.Snapshot;
        var decoding = new EtfDecodeOptions
        {
            Safe = safe,
            LocalNode = wire.Node,
            LocalCreation = wire.Creation,
            LocalSeed = context.Runtime.LocalTermSeed,
            NodeIdentity = context.Runtime.LocalIdentity,
            PidFactory = pid => context.Runtime.RehydratePid(pid, wire),
            PortFactory = port => context.Runtime.RehydratePort(port, wire),
            ReferenceFactory = reference => context.Runtime.ReferenceResources.Rehydrate(reference, wire),
            ExternalFunctionFactory = (identity, requireSafe) => context.Runtime.Functions.External(context.Runtime, identity, requireSafe),
            LocalFunctionFactory = (code, arity, environment) => context.Runtime.Functions.Restore(
                context.Runtime,
                new LocalFunctionIdentity(
                    code.Module,
                    null,
                    code.Index,
                    code.OldUnique,
                    code.Unique,
                    code.OldIndex
                ),
                arity,
                environment
            )
        };
        EtfDecodedTerm result = ExternalTermFormat.DecodeOne(binary.ToArray(), decoding);
        context.Runtime.KnownNodes.Observe(result.Term);

        return used ? Term.Tuple(result.Term, Term.I(result.Used)) : result.Term;
    }
}
