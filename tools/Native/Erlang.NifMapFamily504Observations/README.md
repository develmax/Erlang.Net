# Complete native map-family fixture

Pinned authority: OTP-29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`. The inventory is fifteen NIF APIs: is_map108, size109, new110, put111, get112, update113, remove114, iterator create/destroy/head/tail/next/prev/get_pair115–121, and from_arrays165. The independent original golden is440639 bytes (SHA256 F5BAA4479C6C886E1C5534E0F90C5FB496EFA2D7E822395B43401DCEC1C450A2), acquired from pinned OTP. Sealed source plan SHA256 is D0F1BB1E678627E19685EFF5E77A064C2EB90CB35ECB250E09166CF26407A9A3.

## Parent execution gates

All new tool files must preserve raw bytes (`-text` in repository attributes). The source plan seals C/Erlang, recipe and collector as raw SHA256; imported Python dependencies explicitly use TextLF digests and the collector retains their actual raw byte hashes separately. Every dependency is checked before importing helpers or starting any native process. This declaration avoids silently assuming identical Windows LF/CRLF checkout bytes. ETF output is never normalized.

Initial acquisition (Windows x64): `py -3 -B tools/Native/Erlang.NifMapFamily504Observations/collect.py --repository . --provider-admission artifacts/library376-official-compiler/official-admission.json --plan-sha256 <parent-approved-source-plan-sha256> --output artifacts/native-map504/original1 --original-only`.

Original-only mode does not require the managed host and makes no managed-native comparison claim. It writes the whole original ETF and a typed readable JSON representation for studying pending binary/copy behavior. Parent must inspect that result, retain the original bytes as original-values.etf, add OriginalGoldenSha256 to the plan, reseal it, and run a fresh golden repeat. Never derive expectations from managed returns.

Two serialization profiles are produced by actual `erlang:term_to_binary/2` calls over the same observed term in each VM. `returned.etf` uses `[deterministic,{minor_version,2}]` and is compared strictly as a whole byte sequence. `returned-default.etf` uses `[{minor_version,2}]` and is retained unchanged. There is no post-capture sorting, rewriting or normalization.

The default-profile comparison permits precisely the observed own-VM atom-index ordering at `root.group9.independent.result_map`, with exactly `{added => 99, old => kept}`. All other typed groups/fields must match; raw spans prove every byte outside this single map is equal and both complete encoded key/value pairs match exactly. Different values, different keys, another changed map, or changed outside bytes fail. The previous default whole-file mismatches remain preserved in original1/2 and compare1/2; no failed receipt is relabeled as passed. The independent deterministic-profile golden must be acquired from a new actual original run, never reused from the older default profile.

Full mode requires the managed host binary before any native execution. After a successful C build/admission it runs original, ordinary and reviewed routes independently, retains their exit/stdout/stderr receipts even on failure, then aggregates mismatches. An original protocol/golden validation error does not suppress the two independently prepared managed routes. Failed preparation gates dependent execution. Pass `--check-golden` only after reference acquisition and parent resealing. Backend integration: `--map-family504` dispatches to MapFamily504CallbackEntry.Run with admission path/hash, fresh output directory and ordinary|reviewed. Actual DLL presence/unload, policy counts0/1 and environment/buffer baseline restoration are enforced.

## Fourteen complete protocol groups

1. Type/size/missing-get with real, NULL and unrelated active environment hints; failed size/get preserve their sentinels.
2. Put/update/remove exact keys and unchanged old maps at sizes0/1/2/31/32/33/65, numeric integer/float distinction and compound/bitstring keys.
3. Nonmap refusal with untouched output and source-verified NULL environment/output calls on the early nonmap branch only.
4. Creation from arrays, including zero NULL arrays, numeric exact keys and duplicate atom/compound keys.
5. Iterator forward/reverse whole visit order, initial/boundary states, repeated movement, reentry and destroy; NULL/unrelated environment hints too.
6. Nonmap/invalid-direction iterator creation. Invalid directions are tested only on flat maps: the pinned HAMT implementation allocates a private stack before refusing, so the fixture does not exercise that leaking caller path.
7. Pending map values created by put/update/from_arrays; borrowed direct, tuple and iterator values must retain graph identity until mutation/return.
8. Independent-copy boundaries at63/64/65/4096 bytes, with actual original results deciding heap/refc alias behavior.
9. Temporary equal-key replacement before mutating only the argument, plus a separate one-entry installed-key mutation probe. No unrestricted classification is inferred for arbitrary corrupted key layouts.
10. Explicit copy into an independent destination, put there, copy back, clear/free and surviving copied result.
11. Aliasing map_out with the caller's input variable across put/update/remove.
12. Real reference/PID/function keys tested through whole own-identity protocols without returning their nonportable opaque values.

Missing update has representation-specific output: flatmap returns0 without writing, HAMT writes THE_NON_VALUE. Duplicate arrays also write THE_NON_VALUE. The C protocol records status and whether the sentinel changed, never serializes an invalid word. Iterator EOF get_pair is checked with ordinary outputs before invoking source-safe NULL outputs; success with NULL outputs is never executed.

13. Stored writable binary keys at map sizes2/32/33/65, binary lengths64/65, duplicate targets and sixteen new hash targets:136 cases retaining complete scalarized size/lookup/iterator/update/remove/put/copy behavior. Removal of duplicate mutated keys at33 entries violates the original conversion precondition and faults the original VM; only those two removals are explicitly not invoked (-2), after preserved independent failures and localization.
14. Equal temporary keys replaced by put/update at2/32/33/65 entries and64/65 binary bytes, with same/changed value words:32 cases retaining map-word reuse and flat/HAMT key-argument identity differences after mutation.

Parent local execution passes the whole canonical golden against both loaders, provider6427 retention and actual DLL/environment/buffer cleanup. Default-profile raw captures retain the one declared two-atom order permutation. Earlier failed captures/setup/decoder runs remain failed. Post-return/nested-map topology and other-platform execution remain open; no whole-library or full-MFA claim follows. Exact published-head CI remains a delivery gate. [Part504 evidence](../../../docs/validation/part-504/native-map-family/README.md).
