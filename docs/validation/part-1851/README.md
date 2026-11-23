# Part 1851 — Order map-root forwarding after frame simplification

Map input forwarding now runs after the alias and frame passes. These
passes need to see the saved value before its slot can be eliminated.
The collecting BIF forwarding phase keeps its earlier order.
The integrated release witness restores four slots in
ct_doctest:try_parse_exprs/1 and retains both map roots. OTP uses one
slot. A new permanent fixture reproduces the five-slot case on the
preceding compiler and observes both X0 roots with the change. Pinned
ERTS agrees on the valid and malformed doctest outcomes.

The compiler records the original logical -I search directory when
it selects the matching physical directory. It retains local header
precedence and nested ordinary-include ancestry. An explicit recorded
file override keeps precedence. The release kernel profile derives
the logical prefix from the pinned Makefile. The audit includes all
1,366 profiles and reports a library root on 65 Megaco profiles and
the kernel search binding on 104 profiles.

The package compiles 62 distinct release modules in 130.04 seconds.
It reports 12 Equal, 50 Different, 0 Unsupported and 0 SourceRejected,
preserving every prior file match. Forty-four images change and all
prepare in pinned ERTS. Of 94 changed normalized functions, 17 gain
equality and none lose it. It retains all four map-related normalized
wins from Part 1846. Twenty-nine complete raw chunks gain equality
and none lose it, including 24 Dbgi sections.

The integrated frozen build passes 33 selected cases. An initial
integration build caught an omitted profile property. After resolving
the property, the test build identified an existing two-argument
resolver caller. The optional argument preserves its behavior and the
final compiler, corpus and test builds pass. Both failed logs remain.
The full uncached corpus and full repository runtime gate are still
pending for this exact seal.

[Source, compiler and test identity](source-freeze.json) ·
[Scope](pilot-modules.txt) · [Corpus](pilot.json) · [Timing](pilot-receipt.json) ·
[Functions](pilot-summary.terms) · [Complete sections](pilot-section-delta.json) ·
[Prepared images](pilot-loader.terms) · [Frame witness](ct-doctest-restored.terms) ·
[Focused tests](focused.json) · [Resolved profiles](windows-build-profiles.json.gz).
