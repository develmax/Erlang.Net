using System.Collections.Frozen;
using System.Numerics;

namespace Erlang;

internal sealed class ModuleInformationSnapshot
{
    private readonly FrozenDictionary<string, Term> fields;
    public Term All { get; }

    public ModuleInformationSnapshot(
        string module,
        IEnumerable<(string Name, int Arity)> exports,
        IEnumerable<(string Name, int Arity)> functions,
        BitString fingerprint,
        Term compilation,
        Term? attributes = null,
        ModuleAttributePolicy attributePolicy = ModuleAttributePolicy.AddDefaultVersion
    )
    {
        fields = new Dictionary<string, Term>(StringComparer.Ordinal)
        {
            [ModuleInformationKeys.Module] = Term.A(module),
            [ModuleInformationKeys.Exports] = Signatures(exports),
            [ModuleInformationKeys.Functions] = Signatures(functions),
            [ModuleInformationKeys.Md5] = fingerprint,
            [ModuleInformationKeys.Compile] = compilation,
            [ModuleInformationKeys.Attributes] = attributePolicy == ModuleAttributePolicy.PreserveDecoded ? attributes ?? Nil.Value : ModuleCodeAttributes.WithDefaultVersion(attributes, fingerprint),
            [ModuleInformationKeys.Nifs] = Nil.Value,
            [ModuleInformationKeys.Native] = Term.A(ErlangBooleanAtoms.False),
            [ModuleInformationKeys.NativeAddresses] = Nil.Value
        }.ToFrozenDictionary(StringComparer.Ordinal);
        All = Term.List(new[] { ModuleInformationKeys.Module, ModuleInformationKeys.Exports, ModuleInformationKeys.Attributes, ModuleInformationKeys.Compile, ModuleInformationKeys.Md5 }.Select(key => Term.Tuple(Term.A(key), fields[key])).ToArray());
    }

    public Term Field(string key) => fields.TryGetValue(key, out Term? value) ? value : throw new ErlangException(ErlangErrorReasons.BadArgument);

    private static Term Signatures(IEnumerable<(string Name, int Arity)> signatures) =>
        Cons.From(signatures.Distinct().Select(signature => Term.Tuple(Term.A(signature.Name), Term.I(signature.Arity))));
}
