# ordsets compatibility

Target: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

Current integration: the original12247-row corpus is executed as12 lossless raw partitions per backend; all input and expected row bytes reconstruct the unchanged full ETF. The190-term36100-pair matrix retains direct comparison/equality and public ordsets routes for every pair, with ten additional genuine guest probes. [Coordinated evidence](validation/integration-20261103/ordsets-owner-handoff/README.md) records their exact source windows.

The reproduced native-binding replacement race is fixed by selecting the body and native-only caller marker atomically, preserving captured code/ownership and source-scope unwind. [Atomic source audit and baseline/control](validation/integration-20261103/atomic-native-caller/README.md) retain the old failure and new deterministic controls. Full final-source/CI/review and a separate per-MFA source-closure audit remain required; the6/15 classifications below are unchanged.

Both supported alternatives expose all 21 APIs: the existing C# implementation
and unchanged original `ordsets.beam` executed by the .NET runtime. Tests select
the backend explicitly, verify original-image SHA-256 and source ownership, and
select corresponding managed/original `lists`. Application registrations are
unchanged.

## Differential verification

The permanent `OrdsetsCompatibility` observer covers 12,247 complete outcomes
per backend. It compares the entire deterministic ETF byte sequence: input
arguments, return values, exception class/reason, complete source stacks and
recorded callback effects. Numeric representatives and signed zero remain
observable. No stack filtering, numerical tolerance or result normalization is
used. Every registered MFA must occur in the corpus.

Cases include improper/unsorted lists, duplicates, numeric equivalence,
large integer/float boundaries, tuples, nested lists, bitstrings, external
functions, exact numeric map keys, 32/33/64-entry maps, callback kind/arity
errors, throws/errors, multi-set merges and sorting inputs up to 192 elements.
Native local functions, arbitrary opaque identities and every possible
map/function key layout are not exhaustively enumerated by this corpus.

The official local OTP provider's 6,427 files are verified before and after
capture. A repeat run checks retained fixture hashes and reproduces the whole
oracle byte for byte without regenerating expected values:

```powershell
py -3 tools/Validation/capture-ordsets-compatibility.py --check
```

The tool requires the already admitted local OTP installation. It does not
download, install or change the reference checkout. Without `--check`, it
deliberately recompiles the observer and generates new fixture evidence.

## Fixed discrepancy

`ordsets:union([[2,1|tail],[1]|tail])` reaches managed `lists:umerge/1`
and native `lists:reverse/2`. The error class, reason and reverse arguments
were correct, but the C# source trampoline omitted the current
`lists:umerge2_2/4` frame at `lists.erl:3471`.

Retain the current source PC around native reverse calls in all six terminal
two-way merge branches. Keep the already pending non-tail continuations,
actual native registration and dynamic reverse dispatch. Ordinary replacement
handlers do not acquire the native-only frame. Values, ordering, duplicate
representatives and upstream Erlang assets remain unchanged. The new corpus
also covers distinct `umerge2_1/5` reverse sites at lines 3462 and 3464.

## Acceptance scope

The current ledger has 6 verified APIs, 12 requiring verification and 3 with known differences. This
checkpoint adds differential evidence; a finite successful corpus does not
establish a universal compatibility proof.

| APIs | Existing status | Remaining general contract |
| --- | --- | --- |
| `new/0`, `is_empty/1`, `to_list/1`, `size/1`, `fold/3`, `filter/2` | Verified compatible | Preserve existing evidence and regression gates |
| `add_element/2`, `del_element/2`, `union/2`, `intersection/1,2`, `is_disjoint/2`, `subtract/2`, `is_subset/2` | Needs verification | Complete shared term-order domain and source continuation proof |
| `is_equal/2`, `is_element/2`, `is_set/1` | Known differences | Repair the known native flatmap physical-key numeric/order discrepancy; complete compound equality and native HAMT/function-key coverage |
| `from_list/1`, `union/1`, `map/2`, `filtermap/2` | Needs verification | Complete dependent `lists` sorting/merge/helper partitions and applicable comparison/callback contracts |

All 21 APIs are available; API and full-contract counters each change by zero.
Cross-platform acceptance, complete project validation and canonical readiness
regeneration remain separate gates. The broad baseline regression already had
nine failures before the production fix: eight port launches returned `eacces`,
and one BEAM loader refusal control failed. Preserve that failed report; it is
not a passing full run.

See [checkpoint evidence](validation/part-557/ordsets/README.md) and
[the compatibility ledger](mfa-compatibility.json).

## Existing native flatmap boundary

[Preserved native witness and source history](validation/integration-20261103/native-flat-order-pending/README.md) identify a pre-existing mismatch in numeric/order comparisons after a native binary-key mutation. The current corpus does not close this physical-layout contract; is_equal/is_element/is_set are classified as known differences; other shared comparison-dependent APIs remain pending. A separate source fix and full validation are required.
