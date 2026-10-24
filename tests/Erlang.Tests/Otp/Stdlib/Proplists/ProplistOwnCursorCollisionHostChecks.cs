using System.Numerics;

namespace Erlang.Tests;

internal static class ProplistOwnCursorCollisionHostChecks
{
    internal static async Task Run()
    {
        await using var runtime = new ProcessRuntime();
        await using var owner = new RuntimeNodeFoundationHostActor(runtime);
        await owner.InvokeAsync(async context =>
        {
            var first = new BitString(Convert.FromHexString(ProplistOwnCursorNames.CollisionFirstHex));
            var second = new BitString(Convert.FromHexString(ProplistOwnCursorNames.CollisionSecondHex));
            using var scope = new ProplistOwnHashScope(runtime);
            ulong collisionHash = scope.Hash(first);
            ProplistOwnCursorAssertions.Require(!first.Equals(second) && collisionHash == scope.Hash(second), ProplistOwnCursorDiagnostics.Collision);
            MapTerm map = new(Enumerable.Range(0, ProplistOwnCursorNames.ScalarKeys - 1)
                .Select(index => new KeyValuePair<Term, Term>(Term.I(index), Term.I(index)))
                .Concat(new[] { new KeyValuePair<Term, Term>(first, first), new KeyValuePair<Term, Term>(second, second) }));
            var candidates = map.Entries.Select(pair => (Pair: pair, Hash: scope.Hash(pair.Key))).ToArray();
            int rootCount = candidates.Select(item => item.Hash & ProplistOwnCursorNames.RootNibbleMask).Distinct().Count();
            BigInteger cursor = BigInteger.Zero;
            for (int level = 0; level < ProplistOwnCursorNames.HashLevels; level++)
            {
                int shift = level * ProplistOwnCursorNames.HashNibbleBits;
                ulong slot = (collisionHash >> shift) & ProplistOwnCursorNames.RootNibbleMask;
                ulong[] occupied = candidates.Select(item => (item.Hash >> shift) & ProplistOwnCursorNames.RootNibbleMask).Distinct().Order().ToArray();
                int compressedIndex = Array.IndexOf(occupied, slot);
                cursor |= (BigInteger)compressedIndex << shift;
                candidates = candidates.Where(item => ((item.Hash >> shift) & ProplistOwnCursorNames.RootNibbleMask) == slot).ToArray();
            }
            ProplistOwnCursorAssertions.Require(candidates.Length == 2, ProplistOwnCursorDiagnostics.Collision);
            Term zeroInput = new Cons(new Integer(cursor), map);
            Term oneInput = new Cons(new Integer(cursor | (BigInteger.One << (ProplistOwnCursorNames.HashLevels * ProplistOwnCursorNames.HashNibbleBits))), map);
            Term zero = await ProplistOwnCursorAssertions.Read(context, zeroInput);
            Term one = await ProplistOwnCursorAssertions.Read(context, oneInput);
            ProplistOwnCursorAssertions.Require(
                zero is Cons zeroList && one is Cons oneList
                && zeroList.Head.Equals(Term.Tuple(first, first))
                && oneList.Head.Equals(Term.Tuple(second, second))
                && zeroList.Tail.Equals(one),
                ProplistOwnCursorDiagnostics.Collision
            );
            foreach (Term pair in Cons.Items(zero))
            {
                var entry = (TupleTerm)pair;
                ProplistOwnCursorAssertions.Require(map.TryGet(entry.Items[0], out Term? value)
                    && value!.Equals(entry.Items[1]), ProplistOwnCursorDiagnostics.Collision);
            }
            ProplistOwnCursorAssertions.Require(rootCount <= ProplistOwnCursorNames.RootNibbleMask, ProplistOwnCursorDiagnostics.Collision);
            Term invalid = new Cons(Term.I(rootCount), map);
            bool rejected = false;
            try
            {
                await ProplistOwnCursorAssertions.Read(context, invalid);
            }
            catch (ErlangException error)
            {
                rejected = error.ExceptionClass == ProplistOwnCursorNames.ErrorClass
                    && error.Reason.Equals(Term.Tuple(Term.A(ProplistOwnCursorNames.BadMap), invalid));
            }
            ProplistOwnCursorAssertions.Require(rejected, ProplistOwnCursorDiagnostics.Collision);

            return Nil.Value;
        });
    }
}
