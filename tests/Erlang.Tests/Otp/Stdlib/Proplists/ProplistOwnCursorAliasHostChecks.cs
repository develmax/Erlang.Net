using System.Text.Json;

namespace Erlang.Tests;

internal static class ProplistOwnCursorAliasHostChecks
{
    private const int PrefixAliasSearchLimit = 512;
    private const int PrefixAliasScalarCount = 33;
    private const uint PrefixAliasCreation = 29;

    internal static async Task CountSensitive()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            ReferenceTerm[] aliases = Enumerable.Range(1, ReferenceIdentityLayout.MaximumWords)
                .Select(count => new ReferenceTerm(ProplistOwnCursorNames.Foreign,
                    Enumerable.Range(0, count).Select(index => index == 0 ? 1U : 0U), 23))
                .ToArray();
            using var reservation = runtime.ReserveExternalTerm(Term.List(aliases));
            using var scope = new ProplistOwnHashScope(runtime);
            ulong[] hashes = aliases.Select(alias => scope.Hash(alias)).ToArray();
            ProplistOwnCursorAssertions.Require(
                aliases.All(alias => alias.Equals(aliases[0]))
                && hashes.Distinct().Count() == aliases.Length,
                ProplistOwnCursorDiagnostics.Epochs
            );
            var rows = new List<object>();
            var encoding = new EtfEncodingOptions { FunctionOwner = runtime.InitializationPid };
            for (int index = 0; index < aliases.Length; index++)
            {
                Term result = await ProplistOwnCursorAssertions.RootSuffix(context,
                    ProplistOwnCursorAssertions.WithKey(aliases[index]), scope);
                rows.Add(new
                {
                    StoredWordCount = aliases[index].Words.Count,
                    Hash = hashes[index].ToString(),
                    PublicOutcomeEtf = Convert.ToBase64String(ExternalTermFormat.Encode(result, encoding))
                });
            }
            MapTerm large = LeafMap(scope, aliases[0]);
            int rejectedAliases = 0;
            MapTerm flat = new([new KeyValuePair<Term, Term>(aliases[0], aliases[0])]);
            for (int index = 0; index < aliases.Length; index++)
            {
                Term flatInput = new Cons(Term.List(aliases[index]), flat);
                ProplistOwnCursorAssertions.SameAssociations(
                    await ProplistOwnCursorAssertions.Read(context, flatInput), flat.Entries);
                Term largeInput = new Cons(Term.List(aliases[index]), large);
                if ((hashes[index] & ProplistOwnCursorNames.RootNibbleMask)
                    == (hashes[0] & ProplistOwnCursorNames.RootNibbleMask))
                {
                    ProplistOwnCursorAssertions.SameAssociations(
                        await ProplistOwnCursorAssertions.Read(context, largeInput), flat.Entries);
                }
                else
                {
                    await ProplistOwnCursorAssertions.BadMap(context, largeInput);
                    rejectedAliases++;
                }
            }
            ProplistOwnCursorAssertions.Require(rejectedAliases > 0, ProplistOwnCursorDiagnostics.Epochs);
            await PrefixAlias(context, scope);
            Directory.CreateDirectory(Path.GetDirectoryName(ProplistOwnCursorNames.AliasReport)!);
            await File.WriteAllTextAsync(
                ProplistOwnCursorNames.AliasReport,
                JsonSerializer.Serialize(
                    new
                    {
                        CountSensitiveSourceContractAsserted = true,
                        NativeReferenceStorageWitnessPending = false,
                        NativeHashNumbersInjected = false,
                        CrossVmHashEqualityClaimed = false,
                        EqualTerms = true,
                        DistinctHashCount = hashes.Distinct().Count(),
                        Rows = rows
                    },
                    new JsonSerializerOptions { WriteIndented = true }
                )
            );

            return Nil.Value;
        });
    }

    private static async Task PrefixAlias(ProcessContext context, ProplistOwnHashScope scope)
    {
        for (uint firstWord = 1; firstWord <= PrefixAliasSearchLimit; firstWord++)
        {
            var stored = new ReferenceTerm(ProplistOwnCursorNames.Foreign, [firstWord], PrefixAliasCreation);
            var query = new ReferenceTerm(ProplistOwnCursorNames.Foreign, [firstWord, 0U], PrefixAliasCreation);
            ulong storedHash = scope.Hash(stored);
            ulong queryHash = scope.Hash(query);
            ulong root = storedHash & ProplistOwnCursorNames.RootNibbleMask;
            if (storedHash == queryHash || root != (queryHash & ProplistOwnCursorNames.RootNibbleMask))
                continue;

            ProplistOwnCursorAssertions.Require(stored.Equals(query), ProplistOwnCursorDiagnostics.Epochs);
            MapTerm map = LeafMap(scope, stored);
            Term input = new Cons(Term.List(query), map);
            Term actual = await ProplistOwnCursorAssertions.Read(context, input);
            Term expected = Term.List(Term.Tuple(query, stored));
            ProplistOwnCursorAssertions.Require(
                actual.Equals(expected)
                && actual is Cons { Head: TupleTerm { Items.Count: 2 } pair }
                && pair.Items[0] is ReferenceTerm returnedKey
                && returnedKey.Words.Count == query.Words.Count,
                ProplistOwnCursorDiagnostics.Epochs
            );

            return;
        }

        throw new InvalidOperationException(ProplistOwnCursorDiagnostics.Epochs);
    }


    private static MapTerm LeafMap(ProplistOwnHashScope scope, ReferenceTerm stored)
    {
        ulong root = scope.Hash(stored) & ProplistOwnCursorNames.RootNibbleMask;
        KeyValuePair<Term, Term>[] scalars = Enumerable.Range(0, PrefixAliasSearchLimit)
            .Select(index => Term.I(index))
            .Where(key => (scope.Hash(key) & ProplistOwnCursorNames.RootNibbleMask) != root)
            .Take(PrefixAliasScalarCount)
            .Select(key => new KeyValuePair<Term, Term>(key, key))
            .ToArray();
        ProplistOwnCursorAssertions.Require(
            scalars.Length == PrefixAliasScalarCount,
            ProplistOwnCursorDiagnostics.Epochs
        );

        return new MapTerm(scalars.Append(new KeyValuePair<Term, Term>(stored, stored)));
    }

}
