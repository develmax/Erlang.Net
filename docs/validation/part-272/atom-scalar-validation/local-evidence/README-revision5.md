# Atom scalar validation and lexical diagnostics

This is isolated candidate evidence against published part 271 (`2c1f6dadcf97b7301b60ea153dd6895a6a1a4687`). The final source freeze owns 20 paths; 1520 other published seed files remain byte exact. Main integration and original OTP execution are separate, pending checks.

The unchanged original Terms DLL accepts unpaired UTF-16 surrogates. Two distinct invalid names compare equal through replacement runes while retaining different ordinal hashes; a map then stores two logically equal keys. The old negative capture preserves the actual DLL SHA, MVID, PDB source bindings, accepted overlong atom and registry growth.

The candidate validates only a first new atom name, before pool insertion: strict UTF-16 decoding, at most 255 Unicode scalar values. Existing names retain their fast lookup. The 256th remaining character yields `system_limit` before decoding the tail, matching pinned OTP conversion precedence; invalid encoding within the limit yields `badarg`. Ten cached atoms and primordial indices are unchanged.

The existing `system_info/1` registration now exposes the real `atom_count` selector through `AtomNameRegistry.Count`. Tests compare insertion, reuse and rejected-conversion deltas instead of comparing SDK and OTP absolute counts. No MFA export is added.

The first validation-only execution passed the HOST group and six of eight compiled contracts; two failed because `atom_count` was unsupported. Those original outcomes and unchanged case declarations remain preserved. A peer also reproduced a lexical regression: overlong quoted atoms, bare atoms and variables became `ERL999`. The candidate catches atom-observation errors inside the lexer and emits `ERL001` with the correct atom/variable message and source offset, as pinned `erl_scan` reports illegal atom/variable tokens.

Actual final checks: target build 0 errors and 0 warnings; final source layout 0 changes; HOST group passes; 13 new atom contracts plus 11 existing identifier-order contracts pass; all four overlong-token diagnostics pass with exact code, message and offset. The 255-character quoted, bare, variable and supplementary quoted forms remain accepted. Focus assembly MVID: `c4135fd9-6e7c-4db9-9261-89cc13c34741`.

Original source files are preserved from OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`): `atom.c`, `bif.c`, `utils.c`, `erl_unicode.c`, `erl_bif_info.c`, and `erl_scan.erl`. The differential fixtures declare source-derived expected outcomes; these are not claims of observed native execution.

`receipt-revision5.json` and `revision5-focus.json` contain the latest results. Historical freezes, initial failures, formatting failures, old DLL closure and the earlier new DLL closure remain separate. `actual-revision5-dll-closure` binds the final executed candidate. Expected Main suite size is 6515, not yet observed by this agent. Native execution count is zero.
