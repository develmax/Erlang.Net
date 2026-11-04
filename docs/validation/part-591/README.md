# Managed Unicode utility — current integration

The complete 20-export C# unicode_util port, the 22-export managed unicode surface,
original Erlang modules and independent selection are preserved. Managed Unicode
executes without original Erlang code. Baseline: OTP29.1.1 / Unicode17.0.

Concurrent main publications consumed the earlier work numbers. All prior Unicode
publication and review evidence remains byte-for-byte in unicode-prior-integration.zip
(commit e34b7ad38); its nested archive preserves the first published evidence too.
Main's original Part588/589 and closed changelog volumes remain unchanged.
The current Unicode integration work number is591.

The snapshot records 29 connected passing groups, 14,801 utility outcomes on each
backend, all 1,114,112 codepoints /8,912,896 GC probes, 401,446 conformance checks,
55 managed long cases and the linked gen_statem corpus (74 API +311 scenarios
per backend). Unaligned UTF8 now reads at most four bytes while preserving backing;
the allocation regression rejects the old decoder. Tail badfun frames retain
only real suspended callers; complete 66-row function/error comparisons pass.

This integration retains timer and consumer build fixes from main, refreshes
source identities without changing memberships/statuses/comparisons, rebuilds
and repeats the connected acceptance. Current reports retain their actual source
and assembly identities. No full local regression or global readiness promotion;
required current-head hosted CI remains a delivery gate. These finite results do
not establish universal 100% equivalence across all inputs/platform/resource states.

Current candidate: 29/29 connected groups pass. Both linked timer alternatives
pass 728/728 API rows and 11 scenarios at depth 8. The tail-call distinction also
applies to badarity in the newly added shared helper, preserving non-tail
application behavior. No stack frame is removed from expected observations.
