# Part487 — contract re-audit

Baseline: OTP-29.1.1, commit `ad05823719d77c8faee87348ea39513d4e2f99c5`.

The initial cohorts are31 recorded differences and559 pending default-backend contracts. Registered exports stay708. This checkpoint starts the requested work; it does not declare all590 contracts complete.

## Recorded differences

`known-differences-review.json` binds a current finding and exact implementation source hashes to every one of the31 previously recorded differences. The retained81-case focused run covers the corresponding registered MFA checks and selected current source-function, spawn-option, pending-handle and ordsets regressions. It is not31 whole-contract differential audits.

Six proc_lib entries (`spawn/1,2`, `spawn_link/1,2`, `initial_call/1`, `translate_initial_call/1`) no longer have a demonstrated current version of their old missing-constructor defect. Exact source-image/fingerprint resolution now restores captured functions without executing their constructor. Current/old/purged/sibling and fresh lexical receiver controls pass. Move these entries to **Needs verification**, rather than counting an obsolete defect as current or claiming whole API compatibility. A missing complete native-to-managed trace comparison is an audit obligation, not proof of a semantic defect.

The other25 entries retain known differences. In particular:

- Heap tuning options remain absent, but priority, scheduler, fullsweep_after and message_queue_data are implemented. The old blanket tuning refusal is incorrect.
- The fullsweep_after system setter exists; min_heap_size and other unsupported controls remain open.
- statistics(garbage_collection) returns actual CLR observations. It is not an unsupported key, and the estimate is not identical ERTS native-word/per-process GC accounting.
- Physical process_info fields, crash-report dependencies, complete NIF/platform closure and the default1024 pending-node budget remain open.

## Complete BIF audit

The first three selected contracts are `erlang:bit_size/1`, `erlang:byte_size/1` and `erlang:is_function/2`. The classification applies to the supported valid Erlang term model and their actual runtime BIF registrations. It does not claim identical allocation ceilings, native object layout, scheduling costs or whole compiler/backend completion.

For bit_size, the source partition is bitstring versus every other term. A valid bitstring exposes its logical bit length, including an empty value, an unaligned view and a binary slice. The managed BIF returns that length exactly; all other terms raise badarg through the shared native error-stack path. For byte_size, the same partition returns `(logical_bits+7)/8`, using a widened addition so the calculation cannot overflow at the maximum representable managed bit length. Neither operation invokes a guest function or modifies process/runtime state.

For is_function/2, validate argument2 before inspecting argument1. A nonnegative integer is valid with no255 arity ceiling on the supplied integer; a positive bignum cannot equal a valid function arity and returns false. Negative small/big integers and nonintegers raise badarg. On a valid arity, local/external function terms compare their arity; every nonfunction returns false. The implementation's arbitrary precision integer comparison implements the same partition without narrowing. Valid Erlang function terms are distinguished from arbitrary host-created objects outside the Erlang term contract.

The authorities are pinned `erts/emulator/beam/erl_bif_guard.c` (`bit_size_1`, `byte_size_1`) and `erts/emulator/beam/erl_bif_op.c` (`is_function_2`, `erl_is_function`). Source identities and the reviewed managed dependencies are recorded separately in `bif-source-audit.json`. The proof covers the branch/term partitions; the witnesses alone are not its justification.

The unchanged compiled observer runs through the existing monitored ordsets worker/support protocol, so original and managed launch contexts match. It compares84 complete values and420 complete stack outcomes at default/0/1/2/8 backtrace depths, byte-for-byte and as decoded whole terms, with no field normalization or truncation beyond the guest-selected native depth. Foreign PID/reference/port arguments use explicit fixed ETF identities; no own-runtime identifier is disguised as cross-VM equality. The observer source, original BEAM, expected ETFs and SHA seals live in `tests/Erlang.Tests/Resources/ContractReaudit487`.

`tools/Validation/ContractReaudit487/collect-original.py` reuses the existing admitted OTP provider, full6427-file before/after inventory and bounded owned-process helper. It builds only the new observer and reuses the original compiled worker/support. The ordinary validation workflow reacquires the same whole results from pinned OTP and checks both ETFs before running .NET tests. An original-provider failure blocks this dependent acceptance; no golden is regenerated in CI.

## Delivery boundary

The initial standalone504-outcome comparison passes. The final runtime-dispatch families add six numeric BIF audits and a separate integer-constant control:2016 complete outcomes match. See numeric-source-proof.md for the mathematical partitions, real native entry and the two identity corrections. Exact final-source regression, ledger validation, source layout and publication-head CI are required before final delivery. The frozen first focused81 run and acquisition receipt are retained separately. The original559 cohort loses nine pending contracts if the full audit gates pass; the six reclassified historical defects add six audit obligations, not six new APIs. Thus the expected ledger becomes127 verified /556 pending /25 known, still708 registered.
