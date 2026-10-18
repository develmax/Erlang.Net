# Narrow local FUN comparison proposal

Source-only; no Main edits, build, VM, CI or compatibility promotion. This is a bounded correction for direct local FUN ordering/numeric equality, including FUN nested inside lists, tuples and environments. It does not resolve semantic map key ordering or construction.

Root integration:

1. Admit the existing original capture independently. Preserve the historical twenty-case fixture and first run.
2. Verify every `ExistingTargetSha256` in `copy-plan.json` before copying the four source files. A mismatch requires a source review, not a blind overwrite. No runtime registration or factory change is required.
3. `Term.Equals` selects exact equality; `NumericEquals` selects native numeric equality. `TermOrder.Compare` remains ordering. Internal `CanonicalKeyStorage` preserves the previous managed map storage comparator so this narrow fix does not accidentally redefine exact dictionary key identity. The two new equality entry points are internal.
4. Build once, then run the corrected fixture successor in both runtimes. Bind its two declared stub modules separately. Preserve all values, FUN metadata and full stacks. `source-branches.json` contains source-derived predictions for review; it is not an expected-output generator or substitute for original observations.
5. Rerun the existing deep32768 traversal controls, exact identity/hash/map lookup controls, local FUN environment/arity controls and affected comparison/min/max regressions. Confirm managed hash bytes/algorithm remain unchanged and equal canonical entries still have equal hashes. Include distinct NewUnique/same OldUnique entries: numeric equality may be true while exact equality is false.
6. Keep the eight affected APIs pending for full compound-term eligibility until the explicit map construction/order gaps are resolved. Do not integrate the larger private map prototype as part of this patch.

The original comparator's explicit continuation stack and finally disposal remain in place. No additional `Compare` call from a child iterator, nested map sort, recursive CLR traversal, captured native identity, or fixture-specific production branch is introduced. This is a source argument; actual deep/cancellation regression evidence is still required.
