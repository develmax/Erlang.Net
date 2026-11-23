# Part 1843 — Use a terminal list directly in a returned tuple

A binding-free checked entry can fold closed constants in its body.
When a list binding is consumed once at the end of the returned tuple
spine, preserve its direct result register. An existing binding still
checks the value, and a repeated reference still evaluates once.

The integrated 44-module selection reports 10 Equal, 34 Different,
zero Unsupported and zero SourceRejected in 47.47 seconds. Compared
with Part 1839, ten images changed and all prepare in ERTS. Twelve
normalized functions gain equality and zero lose it. The prior
ssl_dh_groups byte match is retained in the accumulated candidate.
The isolated patch changes the init functions of megaco_tcp_sup and
mnesia_kernel_sup, whose frame widths now agree with OTP. Both
released modules agree with their reference execution on valid and
invalid init calls. All 24 integrated focused tests pass, including
the exact closed-list fixture and binding controls.

[Corpus](cohort.json) · [Normalized functions](normalized-delta.terms) ·
[ERTS preparation](loader.terms) · [Isolated patch evidence](isolated-README.md) ·
[Focused tests](focused.json) · [Source identity](source-freeze.json).
