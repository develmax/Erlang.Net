using System.Security.Cryptography;

namespace Erlang;

public sealed class ModuleCodeDescriptor
{
    public IReadOnlyList<(string Name, int Arity)> Functions { get; }
    public IReadOnlyList<(string Name, int Arity)> Exports { get; }
    public BitString Fingerprint { get; }
    public Term Compilation { get; }
    public Term? Attributes { get; }

    public ModuleCodeDescriptor(
        IEnumerable<(string Name, int Arity)> functions,
        IEnumerable<(string Name, int Arity)> exports,
        BitString fingerprint,
        Term compilation,
        Term? attributes = null
    )
    {
        if (!fingerprint.IsBinary || fingerprint.ToArray().Length != MD5.HashSizeInBytes)
            throw new ErlangException(ErlangErrorReasons.BadArgument);
        Functions = Array.AsReadOnly(functions.ToArray());
        Exports = Array.AsReadOnly(exports.ToArray());
        Fingerprint = fingerprint;
        Compilation = compilation;
        Attributes = attributes;
    }
}
