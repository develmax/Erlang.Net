using System.Collections.Frozen;

namespace Erlang;

public sealed partial class ModuleRegistry
{
    private (BeamLinkedCode Code, ModuleLiteralDomain800 Domain, IReadOnlyList<Term> RecordDefaults) PrepareModuleLiteralCode802(ProcessRuntime runtime, BeamLinkedCode code)
    {
        RequireAsyncSourceCaller();
        runtime.RequireModuleLiteralPreparation800();
        // Capture existing instruction-local template identities before arena
        // construction. Their exact dictionary is replaced with actual native
        // terms; no source-null loader-only CWT tuple remains authoritative.
        var templates = code.LoaderMapKeys565.OrderBy(entry => entry.Key).ToArray();
        int literalCount = code.Structure.Literals.Count;
        // Recs metadata remains detached; its immutable defaults receive their own
        // actual entries in this exact image's literal arena.
        Term[] recordDefaults = code.NativeRecords.SelectMany(definition => definition.Fields)
            .Select(field => field.Value).OfType<Term>().Distinct<Term>(ReferenceEqualityComparer.Instance).ToArray();
        Integer[] inlineIntegers360 = code.CaptureInlineIntegerTemplates360();
        Term[] roots = code.Structure.Literals.Concat(templates.Select(entry => (Term)entry.Value))
            .Concat(inlineIntegers360).Concat(recordDefaults).ToArray();
        ModuleLiteralDomain800 domain = ModuleLiteralDomain800.Prepare800(this, runtime, roots);
        try
        {
            var actualTemplates = templates.Select((entry, index) => new KeyValuePair<int, TupleTerm>(
                entry.Key,
                (TupleTerm)domain.Literals800[literalCount + index]
            )).ToFrozenDictionary();
            BeamLinkedCode actual = code with
            {
                Structure = code.Structure with
                {
                    Literals = Array.AsReadOnly(domain.Literals800.Take(literalCount).ToArray())
                }
            };
            actual.BindNativeLoaderMapKeys802(actualTemplates, domain);
            var actualIntegers360 = inlineIntegers360.Select((template, index) => new KeyValuePair<System.Numerics.BigInteger, Integer>(
                template.Value,
                (Integer)domain.Literals800[literalCount + templates.Length + index]
            )).ToFrozenDictionary();
            actual.BindNativeInlineIntegers360(actualIntegers360, domain);

            return (actual, domain, Array.AsReadOnly(recordDefaults));
        }
        catch
        {
            _ = domain.CloseCodeOwner800();
            throw;
        }
    }
}
