# Part371 narrow flatmap/FUN key ordering proposal

Hold integration until the14 actual pinned-original map/FUN witnesses are
admitted and demonstrate the current Main divergence. No Main file, build,
network action, classification or publication was performed by this helper.

`copy-plan.json` lists seven production files (two replacements, five new
types) and two permanent HOST files. Append this new registration before the
catalog snapshot; preserve every prior ordered test:

    Test(Erlang.Tests.MapFunKeyOrderHostNames.TestName, Erlang.Tests.MapFunKeyOrderHostChecks.Run);

The native branch is pinned utils.c1958-2065. Equal-sized flatmaps compare
native key sequence first (exact numeric key mode), then values in numeric
mode. FUN order2157+ uses OLD_UNIQ; managed canonical map storage deliberately
uses NEW_UNIQ to retain exact key identity. Therefore those storage positions
cannot be reused as source ordering positions.

The candidate changes only ordinary non-exact Ordering for2..32-entry maps.
It creates a temporary stable bottom-up key-order view, then compares all keys
and values. Auxiliary key comparisons run on one explicit heap scope stack,
sharing the original NodeComparisonContext. There is no recursive Compare,
callback into a sorting comparer, key lookup or hash inside comparison. The
finally path disposes all outstanding scopes and iterator stacks.

MapTerm.cs, FunctionTerm.cs, serialization, ExactEquality, NumericEquality,
CanonicalKeyStorage, key lookup, key retention and hash remain unchanged.
The HOST checks reversed key/value map order, both comparison directions,
two exact-distinct keys tied in numeric FUN order, original key lookup, and
32768 tuple wrappers both around map values and inside the auxiliary map-key
comparison. The latter exercises deep auxiliary comparisons without a CLR
recursive comparator. Root must compile/style/execute this test and the
existing deep/snapshot/hash/cancellation cleanup regression.

Scope limits are explicit: HAMT>32 behavior, exact nested-map key mode, and
native compare-zero/exact-nequal FUN key tie protocol are not repaired or
promoted here. Stable merge retains every exact entry; it never coalesces keys.
This focused fix cannot alone establish all21 ordsets contracts. Final native
and actual Main captures, source guard review and affected regression remain
mandatory. No speed claim is made; temporary work is bounded to32 entries.

Creator correction: pinned OTP itself discards the decoded NEW_FUN creator
and emits its local init PID. No creator preservation patch is needed or
included. Full serialized creator fields remain visible to the strict
comparator; no generic identity normalization is proposed.

Authority: OTP-29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Root extracted
the exact pinned utils.c into authority/. Existing central OTP attribution
applies; no license banners or extra dependencies were added.
