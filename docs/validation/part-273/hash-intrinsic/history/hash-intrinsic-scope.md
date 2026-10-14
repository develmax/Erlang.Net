# Hash intrinsic registration allocation correction

The three existing pinned C hash BIF registrations (`phash2/1`, `phash2/2`, and `phash/2`) now carry an internal intrinsic binding. Module dispatch accepts that binding only when both the exact registered delegate and current code-image identity match. It uses the existing image-free dispatch branch, preserving actual call tracing, saved calls, errors, and the halt boundary. Hash algorithms and argument validation remain unchanged.

Ordinary registration and module deletion clear the metadata. Transactional module application snapshots and restores it on failure. Ordinary same-MFA overrides and Erlang source overrides retain their existing generation admission rules; suspended ordinary callbacks retain their actual old image until completion.

The controlled ambient-context allocation experiment on the previous actual Main DLL measured registry dispatch from 352 bytes with no controlled slots to 17032 bytes with 256 slots; the direct hash body remained 32 bytes. This demonstrates a real dependence on execution-context map copying. The historical failing full-suite row measured 12688 bytes against the unchanged 4096-byte budget, but its ambient map size was not captured, so the exact historical map cardinality is unknown.

The candidate passed the unchanged original hash resource test, the new actual-runtime intrinsic contracts, and the existing module lease contracts. New synchronous same-thread measurements with 64, 128, and 256 ambient slots each allocated 136 bytes, including a repeated 256-slot measurement after a real transactional rollback. Trace and return messages, saved-call recording, ordinary/source overrides, an actual suspended old generation, and a trace-driven concurrent halt were exercised. These are local bounded SDK results; they do not replace the root full suite or pinned CI.

Historical failures remain recorded: an inaccessible internal mailbox inspection in the initial HOST fixture, an incorrect reflected method name in the first focus runner, and the resulting owned aborted-process DLL lock during a rebuild. The production intrinsic change was unaffected by those fixture/runner repairs. The final runner reports caught failures with an exit status.

The candidate adds no Erlang exports, changes no hash algorithm, and changes neither the original allocation budget nor its single warmup. Native setnode admission and distribution gateway work remain separate incomplete work.
