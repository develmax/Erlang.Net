using System.Globalization;
using System.Numerics;
using System.Text;

namespace Erlang.Compiler;

public sealed record MapPattern(IReadOnlyList<MapPatternField> Fields) : Pattern
{
    protected override bool MatchCore(Term value, Dictionary<string, Term> bindings, ProcessContext? context = null, Dictionary<string, Term>? keyScope = null)
    {
        if (value is not MapTerm map)
            return false;
        // Resolve all keys before any value pattern binds variables.
        var keys = new Term[Fields.Count];
        try
        {
            for (int i = 0; i < keys.Length; i++)
                keys[i] = Execution.PatternKey(Fields[i].Key, keyScope ?? bindings, context);
        }
        catch (ErlangException) { return false; }
        for (int i = 0; i < keys.Length; i++)
        {
            if (!map.TryGet(keys[i], out var item) || !Fields[i].Value.Match(item!, bindings, context, keyScope))
                return false;
        }
        return true;
    }
}
