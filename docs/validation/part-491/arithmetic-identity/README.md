# Part491 — arithmetic values and actual result identity

Pinned oracle: OTP-29.1.1, `ad05823719d77c8faee87348ea39513d4e2f99c5`. Complete native contracts under audit: binary +, -, *, /, div, rem, band, bor and bxor. The35 final groups pass3332 values and16660 complete stack outcomes (19992 whole comparisons). Final full regression and publication-head CI remain acceptance gates.

## Actual implementation repair

Pinned erl_arith.c returns an existing integer operand for adding zero, subtracting zero, multiplying by one and remainder when the dividend magnitude is smaller than the divisor. CoreModules previously wrapped the mathematically equal BigInteger result in a new Integer object. Genuine original erts_debug:same therefore disagreed for boxed integers.

The isolated pre-fix identity run passes5/9 groups and fails exactly addition, subtraction, multiplication and remainder. Complete actual/expected ETF files remain in before-identity, and identity-before-fix.json retains the failures. The earlier aggregate run has nine outer timeouts; complete late diagnostic outputs independently show addition and remainder false-versus-true observations. Timeout receipts are not relabelled semantic failures or successful tests.

Nine added lines in the integer branch return the actual argument in the corresponding native cases. Existing operand/divisor validation executes first. Both integer operands must already be valid; no shortcut accepts a float or invalid type. Every other integer operation/allocation and the entire float conversion/finite-result path are unchanged. Returning an existing immediate integer object preserves our own VM word protocol as well. This also avoids an unnecessary wrapper allocation; no performance benchmark is claimed.

## Complete semantic partitions

- Integer addition/subtraction/multiplication use exact arbitrary-precision operations, with native result reference rules above. Native small/big representation boundaries do not change the mathematical value. Other boxed integer results are fresh.
- Mixed numeric operations use Integer.TryToDouble, whose nearest-even/overflow argument is already bound by Part487. IEEE binary64 operations produce fresh finite float results, preserving signed zero and subnormal behavior. The pinned Windows and Unix floating macros reject nonfinite results, matching FloatTerm. Invalid types or conversion/operation overflow raise badarith. Division always returns float and refuses zero/nonfinite division.
- div and rem require integer operands and a nonzero divisor. Quotient truncates toward zero; remainder has the dividend sign and is smaller in magnitude than the divisor. Native small/large branches implement the same arithmetic. The smaller-dividend remainder returns the original dividend; other boxed results are fresh.
- band, bor and bxor use infinite signed two-complement semantics for all integer magnitudes/signs. The pinned big.c result writes into new heap storage; managed boxed results are fresh, while small values follow immediate identity. Noninteger operands raise badarith.

These partitions include argument validation, error class/arguments, result type/value/reference and effects. No callback or input mutation is introduced. Identical physical allocation limits, heap/reduction accounting, timing and whole compiler completion are not asserted. source-audit.json binds current/pinned sources, existing conversion proof and all resources. prior-contract-preservation.json explains the narrow source delta; the other1686 source guards are unchanged. Matching current CoreModules rows and workflow seals advance in the two existing native source plans; no native ABI acceptance is promoted.

## Preserve the whole matrix and full stacks

The original nine aggregate groups exceed their unchanged15-second outer limits. Keep before-fix.json. Split by integer inputs, floating-input side, invalid inputs and actual identity; partition-proof.json checks every original probe expression and every case index. All3332 original cases remain exactly once. Recompile/reacquire the35 actual subjects so filename/line/caller stacks refer to the correct source. No global deadline, input or expected outcome is weakened.

Every function atom crosses the existing opaque runtime barrier, and original disassembly proves dynamic apply. Whole raw values and default/depth0/1/2/8 stacks compare without normalization. Existing monitored worker/support BEAMs are reused. Every original capture verifies6427 provider files before/after and normal owned-process settlement. The actual native same facet is admitted from the original erts_debug image with checked ownership, not supplied by a test mock.

## Regression and delivery boundaries

The first local full command ran inside sandbox. It records four external-process/port failures and ends without a completion JSON; incomplete-sandboxed-full.log is retained. All four exact scenarios pass4/4 outside sandbox (port-preflight.json). The unrestricted full final-source run completes8947/8948: one CLR assembly-load failure reports0x800705AF (pagefile/commit exhaustion) before the guest method executes. The exact case passes1/1 in a fresh process. Retain full-original-failure.json and resource-case-rerun.json; full-results.json is explicitly composite8948 coverage. It is not an uninterrupted green run. Every earlier8913 ordered case remains present, and every new group passes.

Part490 parent exact head4df8a05ed is independently verified green: continuous8913/8913,8376 full contract outcomes,17 cold consumers and native/child cleanup. That parent result does not prove the changed Part491 runtime. Require separate exact-publication-head full CI/cold packages and all35 fresh original subjects. LICENSE.local.txt remains excluded. The broader31-difference/559-contract request is unfinished.

Current candidate:153 full contracts/530 pending/25 known,708 registered. Original559 cohort35 closed/524 remaining (6.26%), plus six reclassified audits. erlang56 verified/282 pending/13 known of351. The broad request remains unfinished.
