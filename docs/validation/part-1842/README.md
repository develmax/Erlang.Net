# Part 1842 — Reuse known atoms in reordered private tail relays

The existing closed-caller proof now applies to a direct remote tail
whose variable arguments are a permutation of the formal inputs.
A known atom replaces a moved argument. Same-position and unknown
values follow the original register moves.

The 22-module sample reports 3 Equal, 19 Different, zero Unsupported
and zero SourceRejected in 33.09 seconds. Compared with an aligned
Part 1841 sample, only snmp_app_sup and snmpm_supervisor changed.
Both get_opt/3 functions gain normalized equality. No file or
normalized function lost equality. Both images prepare in ERTS.
The compiled fixture is byte-identical to OTP and produces five
matching outcomes for swap, rotation, identity and unknown inputs.
All 23 focused tests pass. Full candidate acceptance is pending.

[Corpus](cohort.json) · [Paired image hashes](paired-comparison.json) ·
[Normalized functions](paired-normalized.terms) · [Execution](candidate-execution.log) ·
[Focused tests](focused.json) · [Source identity](source-freeze.json).
