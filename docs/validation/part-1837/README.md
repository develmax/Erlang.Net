# Part 1837 — Preserve closed literal order before a local tail

The full Part 1833 diagnostic found one normalized loss in
`ssl_handshake:path_validate/9`. Folding two closed assignments
before the terminal local call reversed the literal-error and nil
argument loads. The compiler keeps the original assignments when
the next operation is a local tail call whose arguments are ready.

Release ERTS prepares the modified `ssl_handshake`, and the pinned
normalized sequence now matches. The permanent three-argument
fixture is byte-identical to OTP. The original, previous and
corrected images return identical values for three ERTS inputs.

The 28-module sample retained 7 Equal and 21 Different with zero
Unsupported and zero SourceRejected. It prepared all 17 changed
images and gained nine normalized matches against Part 1833 with
zero losses. All 15 selected permanent regression cases passed.
The sample took 28.10 seconds.

The full uncached run compiled all 1,366 files in 575.95 seconds.
It retained 404 Equal and 962 Different with zero Unsupported and
zero SourceRejected. There were 346 changed images. ERTS prepared
345 in the batch and `crypto` through its ordinary on-load path.
The three SHA-256 results matched.

Normalized comparison against Part 1833 gained 64 functions and
lost ten. Nine losses arise from preserving closed tail bindings in
functions that still need outgoing register moves. One arises from
keeping a proven local constant in a differently positioned selector
register in `mod_esi:match_script/3`. The raw instruction evidence is
retained. The standard solution build, Tooling and five provider
smoke tests passed. Full regression is deferred to the corrected
linked compiler.

[Full corpus](otp-corpus.json) · [Full timing](timing.json) ·
[Normalized functions](full-normalized-delta.terms) · [ERTS](full-loader.log).

[Corpus](cohort.json) · [Source and binaries](source-freeze.json) ·
[Normalized functions](cohort-normalized.terms) · [Recovered SSL](ssl-path-recovered.log) ·
[Fixture execution](closed-tail-execution.log) · [Focused tests](focused.json) ·
[Full partitions](partitions-manifest.json).
