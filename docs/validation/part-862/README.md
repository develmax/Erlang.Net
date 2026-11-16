# Part862 Corpus diagnostics and quoted escapes

The corpus tool records the innermost emitter throw site for unsupported modules. A source file and line are included when compiler symbols are available. The release comparison and status definitions are unchanged.

The lexer shares Erlang escape decoding between character literals, strings and quoted atoms. The deterministic regression oracle exercises fixed and braced hexadecimal escapes, octal boundaries, control escapes and supplementary Unicode. Regenerate it with pinned `erlc.exe +deterministic beam_predefined_probe.erl` from the fixture directory.

After enabling escapes, 164 sources were rejected, 1,109 images were unsupported, and 93 matched. All eight previous escape rejections reached BEAM emission.

The same build with the attribute-specific diagnostic reported these first failures in source mapping:

| Attribute | Images |
| --- | ---: |
| `-export_type` | 380 |
| `-import` | 95 |
| `-compile` | 48 |
| `-callback` | 30 |
| `-nifs` | 7 |

The follow-up admits `-export_type` during line mapping and retains its value in release debug forms. Documentation and type lowering have their own coverage limits.

All 380 affected images passed the original directive check. Of those, 329 reached the documentation type guard, 31 hit another source mapping attribute, 18 reached targeted inlining, and two reached expression lowering. The documentation type guard is now the largest first failure across the corpus at 419 images. Full-corpus status counts were unchanged after this follow-up. All three focused cases passed with the updated fixture. The full runtime suite and canonical readiness regeneration were outside this run. [Full corpus](otp-corpus.json) and [focused cases](focused.json).

Build the corpus tool with `dotnet build tools/Validation/Erlang.BeamCorpus -m:1 --no-restore -v:q`. Run the resulting executable with the pinned installation and report destination. Build the test runner with `dotnet build tests/Erlang.Tests -m:1 --no-restore -v:q`.
