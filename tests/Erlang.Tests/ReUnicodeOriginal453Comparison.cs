using Erlang;

namespace Erlang.Tests;

internal static class ReUnicodeOriginal453Comparison
{
    internal static async Task<(int Raw, int OwnReference, int Authenticated)> Rows(
        ProcessRuntime runtime,
        RuntimeNodeFoundationHostActor actor,
        string route,
        Term profile,
        Term expected,
        Term actual,
        List<object> receipts,
        List<object> failures
    )
    {
        Term[] left = Cons.Items(expected).ToArray();
        Term[] right = Cons.Items(actual).ToArray();
        ReUnicodeOriginal453Access.Require(left.Length == ReUnicodeOriginal453Names.Cases && right.Length == left.Length);
        ReUnicodeOriginal453Access.Require(left.Cast<TupleTerm>().Select(row => row.Items[0]).Distinct().Count() == left.Length);
        int rawCount = 0, ownCount = 0, authenticated = 0;
        for (int index = 0; index < left.Length; index++)
        {
            var bindings = new Dictionary<ReferenceTerm, ReferenceTerm>();
            var patterns = new Dictionary<ReferenceTerm, TupleTerm>();
            bool same = Same(
                left[index],
                right[index],
                bindings,
                patterns
            );
            bool raw = ExternalTermFormat.Encode(left[index]).AsSpan().SequenceEqual(ExternalTermFormat.Encode(right[index]));
            string? authenticationFailure = null;
            try
            {
                foreach (TupleTerm pattern in patterns.Values)
                {
                    await actor.InvokeAsync(async context =>
                    {
                        // Authenticate the exact retained error-argument resource, never a replacement compile.
                        Term names = await runtime.Modules.Call(
                            context,
                            ReUnicodeOriginal453Names.Re,
                            ReUnicodeOriginal453Names.Inspect,
                            pattern,
                            Term.A(ReUnicodeOriginal453Names.NameList)
                        );
                        ReUnicodeOriginal453Access.Require(names.Equals(Term.Tuple(Term.A(ReUnicodeOriginal453Names.NameList), Nil.Value)));
                        Term usable = await runtime.Modules.Call(
                            context,
                            ReUnicodeOriginal453Names.Re,
                            ReUnicodeOriginal453Names.Run,
                            new BitString(Array.Empty<byte>()),
                            pattern
                        );
                        ReUnicodeOriginal453Access.Require(usable.Equals(Term.Tuple(Term.A(ReUnicodeOriginal453Names.Match),
                            Term.List(Term.Tuple(Term.I(0), Term.I(0))))));

                        return true;
                    }).WaitAsync(ReUnicodeOriginal453Names.Deadline);
                    authenticated++;
                }
            }
            catch (Exception error)
            {
                authenticationFailure = error.ToString();
            }
            bool passed = same && (raw || bindings.Count > 0) && authenticationFailure is null;
            receipts.Add(new
            {
                Route = route,
                Profile = IoPrintableOriginal404Terms.Typed(profile),
                Index = index,
                Passed = passed,
                WholeRawEqual = raw,
                ReferenceBindings = bindings.Select(pair => new { Original = pair.Key.ToString(), Managed = pair.Value.ToString() }).ToArray(),
                AuthenticatedSamePatterns = patterns.Count,
                AuthenticationFailure = authenticationFailure,
                FrameFieldsProjected = false
            });
            if (!passed)
                failures.Add(new { Route = route, Index = index, Expected = IoPrintableOriginal404Terms.Typed(left[index]), Actual = IoPrintableOriginal404Terms.Typed(right[index]), AuthenticationFailure = authenticationFailure });
            if (raw) rawCount++;
            else ownCount++;
        }

        return (rawCount, ownCount, authenticated);
    }

    private static bool Same(
        Term expected,
        Term actual,
        Dictionary<ReferenceTerm, ReferenceTerm> bindings,
        Dictionary<ReferenceTerm, TupleTerm> patterns
    )
    {
        if (expected is TupleTerm e && actual is TupleTerm a)
        {
            if (e.Items.Count != a.Items.Count) return false;
            if (e.Items.Count == 5 && e.Items[0].Equals(Term.A(ReUnicodeOriginal453Names.Pattern)) && e.Items[4] is ReferenceTerm)
            {
                if (!e.Items.Take(4).SequenceEqual(a.Items.Take(4)) || e.Items[4] is not ReferenceTerm er || a.Items[4] is not ResourceReferenceTerm ar)
                    return false;
                if (er.Node != ar.Node || er.Creation != ar.Creation) return false;
                if (bindings.TryGetValue(er, out ReferenceTerm? previous)) return previous.Equals(ar);
                if (bindings.Values.Contains(ar)) return false;
                bindings.Add(er, ar);
                patterns.Add(ar, a);

                return true;
            }

            return e.Items.Zip(a.Items).All(pair => Same(
                pair.First,
                pair.Second,
                bindings,
                patterns
            ));
        }
        if (expected is Cons ec && actual is Cons ac)
            return Same(
                ec.Head,
                ac.Head,
                bindings,
                patterns
            ) && Same(
                ec.Tail,
                ac.Tail,
                bindings,
                patterns
            );

        return expected.Equals(actual);
    }
}
