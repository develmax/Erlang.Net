# Complete the proplists-owned stack frame

Read-only source/data analysis. No runtime/shared source, registry, workflow, CI, SDK, VM, or native execution by reviewer.

Pinned authority: OTP-29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`.

- `lib/stdlib/src/proplists.erl:207`: /2 tail delegates to /3 with undefined.
- `proplists.erl:222`: first /3 clause; recursive misses at line234 tail delegate. Reached invalid tails therefore retain only get_value/3, with `[Key,ReachedTail,EffectiveDefault]`, not the initial list, /2 frame, or recursion history.
- `erts/emulator/beam/beam_common.c:1015-1026`: function_clause takes current function arguments from registers and ignores current PC. Lines1230-1233 resolve function information from the current function for this error. This supports the /3 entry location222, rather than the /2 line207 or recursive call line234.
- `lib/compiler/src/beam_asm.erl:84`: implicit filename0 is module name plus `.erl`; line-table encoding322 onward preserves filename and source line entries.
- `erts/emulator/beam/utils.c:3430-3450`: builds line then prepends file, producing ordered `[{file,Filename},{line,Line}]` when a valid location exists.
- `beam_common.c:885`: allocates `max(backtrace_depth-1,0)` continuation slots; current frame is built separately1285. `erts/emulator/test/exception_SUITE.erl:626-636` explicitly tests depths0..8 with `Expected=max(1,D)`. Depth0 must still contain the top error frame; it must not be empty.

Passive inspection of the already captured installed proplists BEAM (SHA256 `B027D227385C342C4153C0CA8627E6E8428FAA4A88F879E3C85A098067FF1882`) confirms source locations207 and222 both use implicit filename0 `proplists.erl`. Evidence: installed-line-table.json, SHA256 `955E502BB5390E31EA063CACEE78877ECC1A35A02656B6B3A155BE36B00331C2`. This is compiled line-table evidence, not a newly observed runtime stack.

The expected API-owned top frame for both arities is:

```
{proplists,get_value,[Key,ReachedTail,EffectiveDefault],
 [{file,"proplists.erl"},{line,222}]}
```

Root's suggested optional sourceLocation argument to RuntimeExceptionCapture.Call is a bounded way to supply this location only for the proplists failure frame. Cache the immutable source location separately from semantic missing-value atoms. Preserve existing depth truncation, caller chain, automatic-capture marker and native error-info behavior. No compiler metadata redesign is required to fix this particular API-owned frame.

## Finite observer additions

1. Strengthen all14 existing bad-list/improper-tail cases to assert the exact ordered file/line list instead of `_`, keeping current exact key/tail/default argument assertions. First acquire the original output to confirm the source-derived expectation.
2. Add12 bounded cases: arity2/3 × tail/non-tail caller × backtrace_depth0/1/2. Use exported/no-inline helpers if needed to ensure a real caller boundary. A non-tail helper must use the call's returned value (for example construct `{found,Value}`); an ordinary tail helper delegates directly. Preserve raw complete stacks in an observation artifact, including caller locations, even when comparing only the declared proplists-owned frame and caller identities.
3. For every case require error:function_clause; exact top frame above; no proplists:get_value/2 frame; exactly one proplists:get_value frame despite recursion; stack length1 at depths0/1. At depth2 require the expected logical non-tail helper caller MFA/arity, and require a tail helper to be absent. The caught exception harness must itself provide at least one non-tail continuation so the depth2 test is meaningful. Do not assert arbitrary caller source file/line values until those are actually observed on both engines.
4. Optionally add two deep-prefix errors (/2 and /3 with a bounded 12,000-entry proper prefix ending in the bad tail) to demonstrate that the top frame still carries only the reached tail and no recursion history. Existing53 already cover long successful/missing traversal, so do not duplicate that family.
5. Set backtrace_depth inside try/after and restore the old value. Run these cases serially within a VM, or in separate VMs: the original flag is VM-global, so parallel workers changing it would make depth results unreliable. Record restoration and cleanup even on assertion failure.

The exact number of branch tests is a finite acceptance set, not a mathematical universal proof. Together with the reviewed source branches and original/SDK executions, it can close the declared proplists-owned return/error/top-frame contract. Full caller file/line metadata, whole-compiler trace fidelity, tracing/saved-call accounting and identical reduction counts belong to inherited runtime/compiler contracts and must remain explicitly separate. Do not silently omit their raw differences and then claim every observable runtime behavior became identical. Keep implementation count(+2), contract acceptance, inherited limitations and final integrated regression distinct in the publication.
