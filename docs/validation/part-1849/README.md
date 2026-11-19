# Part 1849 — Fold unused tuple bindings and private constructor arguments

The first change replaces a tuple element match with its constant
value when the clause has exactly one occurrence of the fresh
binding. References in guards, existing patterns and subsequent
matches all count. The rewrite preserves every effectful expression
and excludes clauses with nested binding scopes. Pinned OTP's Core
substitution and literal construction provide the reference.

The second change extends the private-caller proof to atoms and the
empty list in pure tuple and list constructors. It joins every call
site, declines escaped functions and incompatible arguments, and
obeys the SSA option. Pinned beam_ssa_type performs corresponding
singleton substitution while preserving entry arity. Both changes reuse
C# analyses and operand emission.

The integrated 46-module selection takes 225.91 seconds on one
worker. It reports 4 Equal, 42 Different, 0 Unsupported and 0
SourceRejected, preserving all prior exact files. All 34 changed
images prepare in pinned ERTS. Across 157 changed normalized
functions, 15 reach equality and none lose it. Five raw sections
become exact, including two LitT chunks. There are no section
losses. The two new constructor-related witnesses against Part 1845
are diameter_session:sequence/0 and mnesia_kernel_sup:supervisor_spec/1.

All 29 selected integrated cases pass. Independent permanent
regressions reproduce the defects on their exact prior compilers.
The isolated execution checks cover retained effects, improper tails,
bad matching and released Diameter and Mnesia behavior. Readiness and
full acceptance remain pending.

[Corpus](pilot.json) · [Timing](pilot-receipt.json) ·
[Source and compiler identities](source-freeze.json) ·
[Compiler regressions](focused.json) ·
[Normalized functions](pilot-summary.terms) · [Sections](pilot-section-delta.json) ·
[ERTS preparations](pilot-loader.terms) ·
[Unused binding experiment](isolated-unused-README.md) ·
[Singleton proof](isolated-singleton-README.md).
