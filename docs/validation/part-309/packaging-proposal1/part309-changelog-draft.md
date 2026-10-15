## Part 309 — source-bound BEAM fixture exporter and table correction

2026-10-15

This tooling-only change reuses the exact published part 308 validation: 6,637 passing cases, all eleven integration/package results and trusted Windows native checks against 1,020 unchanged source inputs. No new full suite, native execution, original compilation or original body execution was performed for part 309. Registry 505, optional IO 507 and erlang API presence 347 of 351 remain unchanged.

The standalone tool target built with zero warnings/errors. The retained isolated proof covers six managed CLI refusals, one append refusal and two verifier refusals. Three CSharp token streams are exact. All 103 frozen tooling members, the 46-row compile closure and independent review are retained. Original source hypotheses target call_ext, call_ext_last with a positive frame and saved Path/Info Y slots, and call_ext_only. Their actual pinned compiler opcode shapes remain unobserved; the follow-up compiler/disassembly collector is separate from executing fixture bodies or native loading.

The human API table corrects three stale Missing rows for dist_get_stat/1, load_nif/2 and process_info/1. Their bounded behavior remains partially compatible. Exactly four APIs remain unregistered: alloc_info/1, alloc_sizes/1, memory/0 and memory/1. Other table content is preserved, apart from the new part 309 note.

Published part 308 remote evidence at the archived snapshot: {{Remote308Summary}} Pending observations are not represented as completed. API presence, the exporter checks and reused validation do not establish full erlang compatibility, physical resources, general NIF ABI, upstream-suite coverage or performance parity.
