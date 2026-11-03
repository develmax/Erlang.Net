# MR104 physical flat values and conflict reconciliation

2026-11-04. Local stage based on MR104 head2b7e3b263, before integration of main332be1a. No full-suite or21/21 claim.

`MapIteration.Next` previously reconstructed every physical duplicate's value through key lookup, repeating the first value. Source/native flat arrays now retain the actual key-value occurrences. Flat cursors, iteration, hashing and detached copies read the preserved sequence without another sort. Owned map payloads expose their registered physical view; arithmetic comparison avoids caching a second boxed child array beyond its heap owner.

[Build log](build.log): zero warnings/errors. [Selected report](physical-tests.json) and [log](physical-tests.log):3/3 PASS. The new `runtime/maps-source-flat-physical-values-through-copy` invokes the actual source flat publication boundary and checks distinct ordinal values through the public to-list facade, registered native call and actual mailbox copy. [Controlled previous-DLL report](negative-before-fix.json) and [complete stack](negative-before-fix.log): same regression FAILS with previous MR104 runtime assemblies. [Current assembly hashes](assemblies.json).

The existing48-case ordsets whole-corpus/term-order subset is a separate running diagnostic at this checkpoint. It is not a full report, and its eventual result must retain its own source window.

GitHub Validate41722076712 failed before SDK/build at passive source admissions. Original job logs are preserved in [Windows log](126217184463.json) and [Linux log](126217184218.json). [Local raw source mismatch report](local-mfa.json) identifies current unrefreshed source bindings. These are open gates, not bypassed checks. The unrelated base-main consumer-pin/CI corrections are accepted from MR106 through the main merge.

Pinned original maps expose physical key/value slots; an exact-key lookup is a distinct operation. Reuse existing .NET10 map/heap/copy/ownership machinery. No new package, public ABI removal or production ERTS dependency. Remaining source contracts, scheduler entropy, complete current-head CI/review and postmerge acceptance remain required; ordsets stays6 Verified/15 KnownDifferences/0 PendingVerification.
