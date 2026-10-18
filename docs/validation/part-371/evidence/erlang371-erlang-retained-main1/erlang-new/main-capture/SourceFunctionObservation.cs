using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using Erlang;
using Erlang.Compiler;

internal static class SourceFunctionObservation
{
    internal static object Read(IEnumerable<ModuleDefinition> definitions, params Term[] observations)
    {
        var modules = definitions.ToDictionary(module => module.Name, StringComparer.Ordinal);
        var resolver = typeof(Execution).Assembly.GetType(SourceFunctionObservationNames.ResolverType, true)!
            .GetMethod(SourceFunctionObservationNames.ResolverMethod, BindingFlags.Static | BindingFlags.NonPublic)!;
        var pending = new Stack<Term>(observations);
        var visited = new HashSet<Term>(ReferenceEqualityComparer.Instance);
        var bindings = new List<object>();
        while (pending.TryPop(out Term? term))
        {
            if (!visited.Add(term))
                continue;
            if (visited.Count > SourceFunctionObservationNames.MaximumVisitedTerms)
                throw new InvalidOperationException(SourceFunctionObservationNames.TraversalLimit);
            switch (term)
            {
                case Cons cell:
                    pending.Push(cell.Head);
                    pending.Push(cell.Tail);
                    break;
                case TupleTerm tuple:
                    foreach (var item in tuple.Items)
                        pending.Push(item);
                    break;
                case MapTerm map:
                    foreach (var item in map.Entries)
                    {
                        pending.Push(item.Key);
                        pending.Push(item.Value);
                    }
                    break;
                case FunctionTerm function:
                    foreach (var item in function.Environment)
                        pending.Push(item);
                    if (function.LocalIdentity is not { } identity || !modules.TryGetValue(identity.Module.Name, out var module))
                        break;
                    object? resolved = resolver.Invoke(null, [module, identity, function.Arity]);
                    LocalFunctionIdentity? sourceIdentity = resolved?.GetType()
                        .GetField(SourceFunctionObservationNames.IdentityField)!.GetValue(resolved) as LocalFunctionIdentity;
                    string? named = resolved?.GetType()
                        .GetField(SourceFunctionObservationNames.NamedField)!.GetValue(resolved) as string;
                    Expr.Fun? anonymous = resolved?.GetType()
                        .GetField(SourceFunctionObservationNames.AnonymousField)!.GetValue(resolved) as Expr.Fun;
                    bindings.Add(new
                    {
                        Module = identity.Module.Name,
                        Name = identity.Name?.Name,
                        function.Arity,
                        identity.Index,
                        identity.OldIndex,
                        identity.OldUnique,
                        UniqueHex = Convert.ToHexString(identity.Unique.ToArray()),
                        EnvironmentCount = function.Environment.Count,
                        ResolverBound = sourceIdentity is not null
                            && sourceIdentity.Module.Equals(identity.Module)
                            && Equals(sourceIdentity.Name, identity.Name)
                            && sourceIdentity.Index == identity.Index
                            && sourceIdentity.OldIndex == identity.OldIndex
                            && sourceIdentity.OldUnique == identity.OldUnique
                            && sourceIdentity.Unique.Equals(identity.Unique),
                        SourceIndex = sourceIdentity?.Index,
                        SourceOldIndex = sourceIdentity?.OldIndex,
                        SourceOldUnique = sourceIdentity?.OldUnique,
                        SourceUniqueHex = sourceIdentity is null ? null : Convert.ToHexString(sourceIdentity.Unique.ToArray()),
                        SourceLegacyIndex = sourceIdentity?.LegacyIndex,
                        ObservedLegacyIndex = identity.LegacyIndex,
                        ResolvedName = sourceIdentity?.Name?.Name,
                        ResolvedNamedFunction = named,
                        NamedDefinitionExists = named is not null && module.Functions.Any(value => value.Name == named && value.Arity == function.Arity),
                        ResolvedAnonymousBodySha256 = anonymous is null ? null : Convert.ToHexString(SHA256.HashData(
                            Encoding.UTF8.GetBytes(CodeGeneration.ExpressionCode(anonymous))
                        ))
                    });
                    break;
            }
        }

        return new { VisitedTerms = visited.Count, Bindings = bindings, SourceResolverInvoked = bindings.Count != 0, GuestCodeExecutedByDiagnostic = false };
    }
}
