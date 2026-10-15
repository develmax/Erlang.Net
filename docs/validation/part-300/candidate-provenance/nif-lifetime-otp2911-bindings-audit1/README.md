# Exact OTP 29.1.1 lifetime ownership audit

Read-only peer audit of pinned commit `ad05823719d77c8faee87348ea39513d4e2f99c5`. The author captured exact `git show commit:path` stdout bytes, hashed each complete pinned blob, and retained independently hashed line excerpts. This audit changes neither Main nor the immutable lifetime overlay. No build, HOST, native fixture, original emulator, or network request was executed.

`source-bindings.json` binds the immutable 15-path lifetime source freeze and the three exact OTP source blobs. The newer local OTP working tree is not the evidence source.

| Ownership fact | Exact pinned source and lines | Consequence for this candidate |
| --- | --- | --- |
| Deletion invalidates exports and moves current to old | beam_bif_load.c 679-735, 2448-2455 | Keeping the native owner on DeleteModule agrees with the original ownership boundary. |
| Purge completes after process/code-reference handling | erts_code_purger.erl 105-128; beam_bif_load.c 2222-2258 | Old-image retirement must follow actual code-reference drain, rather than ordinary deletion or a kill request alone. |
| Raw failed-on_load finishing only clears deferred export references | beam_bif_load.c 918-940 | Leaving the raw primitive distinct from full failed-image discard is correct. |
| The purger subsequently prepares and purges failed pending code | erts_code_purger.erl 154-182; beam_bif_load.c 2128-2166, 2295-2304 | Full raw prepare_on_load remains missing; the candidate's custom high-level discard is a separate host API. Saved prior old code is restored after pending purge. |
| Native ownership outlives a code instance when references remain | erl_nif.c 88-121, 5334-5373 | The candidate's admitted-call/initializer/owner ledger is bounded managed ownership, not the original resource/dirty-work/finisher reference-count implementation. |
| Actual unload is scheduled as auxiliary work | erl_nif.c 2528-2556, 5367-5372 | Reaching purge metadata completion does not mean that the original unload callback has already run. |
| Unload callbacks and dlclose are serialized against subsequent library loads | erl_nif.c 2559-2617 | The original acquires code-modification permission before unload callbacks and dlclose, releasing it afterward. A native library fence is still needed if callbacks run outside that permission. |

The lifetime README's original line references 2256, 922, 2134 and purger around 168 do match the pinned blob. Its erl_nif.c around 5356 reference points into resource-type unlinking; the complete unload operation begins at 5335, with module ownership cleared at 5367 and native-library reference decrement at 5369. This is a line-reference correction, not evidence that the original differs on deletion/pending-purge ownership.

Two semantic distinctions require an explicit boundary before publication:

1. The held managed overlay deliberately invokes cleanup outside the registry/runtime monitors and outside the caller's code-modification ownership; its HOST also allows cleanup to acquire a fresh writer. OTP instead retains code-modification permission across the committed native unload/load exclusion window (2559-2617). Existing coordinator upgrade refusal cannot replace that protection after purge removes old metadata while a queued cleanup is still pending. Before exposing concurrent public native loads of the same library, add a library-identity fence or another proven exclusion mechanism. This audit does not prescribe running callbacks under registry/runtime monitors.
2. The custom host purge and high-level discard await cleanup and surface its failures. OTP schedules actual unload asynchronously, so those host methods have a stronger completion contract and potentially different return timing. Do not label their return timing as exact OTP parity. Runtime Dispose, initializer death and halt quiescence ownership are host-runtime cleanup guarantees; this source audit does not establish equivalence to whole-VM OTP shutdown/on_halt callback behavior.

Neither distinction requires changing the immutable source overlay merely to publish it as a managed ownership prerequisite with these limits. Both prohibit a claim that general native lifecycle, arbitrary native library reuse, resources, dirty work, thread callbacks or public load_nif support are complete. Original/native execution evidence remains separate.
