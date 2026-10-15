# Pinned native entry options correction

Source-only two-file delta against root-nif-native299-isolated/candidate. Candidate2 remains unchanged. No native execution/build/Main/Git action occurred here.

The actual Windows failure is caused by our incorrect entry.Options!=0 restriction. Pinned OTP-29.1.1 erl_nif.h documents this field as unused and allowing zero or one, and its ERL_NIF_INIT macro unconditionally initializes it to one. The exact published290 reviewed DLL's export-resolved nif_init code returns entry RVA0x3040; reading those PE bytes gives ABI2.18, expected module, variant, functions, load/unload pointers, zero reload/upgrade and options1. evidence.json retains binary hash, actual decoded metadata and pinned source hash/lines. No native initializer was called to obtain these fields.

The fix is intentionally narrower than general OTP compatibility: add NifNativeAbi.InitializerEntryOptions=1 and require that exact value for this trusted fixture profile. All other module, function, flags, callback and lifecycle conditions are retained. This field is not enif_set_option and does not enable dirty scheduling. The existing successful standalone fixture host never rejected options1.

Root should apply these two files after its candidate2, veto-disposal and layout deltas, then rerun actual target/managed/native validation. Current source evidence does not claim that retry passes.

Revision 2 is an explicit raw-EOL derivative: duplicate CR characters in the two C# files were collapsed to single CRLF. Revision 1 and its freeze remain unchanged. eol-repair.json binds both before/after hashes; no further semantic edit occurred.
