# Part 361: bounded NIF callback contract audit

`enif_get_map_size` (Windows slot 109) and `enif_make_ref` (slot 28) are supported for the audited valid-use contracts on admitted Windows/glibc Linux x64 routes. They are NIF callbacks, not new Erlang MFAs. No whole-NIF, whole-loader, general TLS or unqualified 100% classification follows. Publication-head CI remains pending. Registry identity stays 587 MFAs with 16 verified; erlang 16/351 is unchanged.

## Pinned contract and applicability

Authority: OTP 29.1.1 `ad05823719d77c8faee87348ea39513d4e2f99c5`; Apache 2.0 source-port attribution retained, no new dependency.

The [ABI declarations][api] preserve native widths. [Size][nif], line 3613: map success writes cardinality and returns 1; nonmap returns 0 without writing; env is unused. `MapTerm.Entries.Count` plus one `nuint` write covers admitted maps independently of their content. Future pending maps are not preapproved.

[MakeRef][nif], line 2274, allocates an ordinary local reference in the valid environment. [Domain selection][domains] and [packing][packing] govern finite identities. The adaptation uses the environment's actual runtime and the language reference generator. VM words are opaque, not reproducible cross-VM values. The [environment contract][env] keeps bound handles within their call/thread. Synchronized independent environments survive later calls until clear/free; copies in another environment survive source clear/free. Callers stop workers before unload.

| Contract branch | Source argument and accepted witnesses |
| --- | --- |
| Size output/domain | Empty,31/32/33,8193; nested/large values and numeric keys; status/count/canaries. Nonmap refusal precedes pointer checks. Env hints do not redefine ownership. |
| Size lifetime | Copied independent map, joined foreign read and later retained read use the actual term owner. |
| Reference identity/lifetime | Local node/type, language/native generator, returned-call/load callback, independent copy/clear/free and foreign allocation/make/handoff. |
| ABI/concurrency | V4/360-byte table, Size offset 312; per-runtime namespace/table/allocator; two-runtime reviewed/ordinary routes; atomic packing/domain boundary controls. |

Pinned source encodes zero as a reference, not NULL. Period is 2^64 per domain; retain the [conservative public bound][limits] `(NumSchedulers+1)*(2^64-1)`, not unbounded uniqueness, physical scheduler identity or timing equivalence. Native handles have a separate process-global signed-long ceiling: released handles are not reused and nonpositive increments fail. Do not conflate that resource boundary with generator capacity. Invalid pointers, expired handles, unsynchronized access, MakeRef(NULL) and successful-map NULL-output writes are outside valid-use parity; managed containment is not an OTP crash oracle.

## Current source and execution

C5 plan `26771833B3E7C2A70166C36ED0854354198F4DC5E6270E01CD1B00E97101A2E2` seals 3320 rows, 221 guards and 104 styled C# files. [source review](linux-to-final-source-review.json) binds 52 runtime/native/diagnostic sources to tested Linux 91a28131 and chained style proofs, including three retained diagnostic dependencies: 52 total, not 55. This is source equivalence, not execution.

Independent [independent full acceptance](full4-acceptance.json), SHA256 `63640464A9ECC6560E1A2C6CD94A5EADC910A8E8B3AA49470BA8D88F5A0EE02A`, accepts 7438/0, exact 7425-name prefix, 11 integration outcomes, two packages, 12 normal children and 26 stable native/focus DLL snapshots. Full3's 16 eacces failures remain preserved. A separate Root diagnostic records Win32 error 5 at CreateFileW opening the process-owned named-pipe client; the same binaries pass all sixteen cases outside the restricted environment. This does not change runtime code or ACLs. The exporter replaced 32 Limits strings; C5 restores curated 77B061DE..., preserving raw output.

Windows 34/34 row acceptance uses fresh originals 39029306519/39027923905 in [Windows acceptance](windows-original-acceptance.json), with reconstructed 12008-file source and fixture/header/image bindings. LP64 [4,8,8,40] and LLP64 [4,4,8,40] widths differ; whole envelopes are not byte-identical.

Linux collector 91a28131/run39037669366 repeats the passing suite without debugger installation. [Independent collector acceptance](linux-collector-acceptance.json) for 91a28131, retaining the [111e17f0 TLS-repair acceptance](linux-tls-repair-acceptance.json), establishes 68 exact original rows (17 groups*2 routes*2 runtimes), 16 managed-only outcomes, three ordinary-loader cases, three destructor-loader cases and 50 normal process receipts. Delete/purge, body equality and SDK retirement/disposal are separate. Managed DLL hashes are recorded; absent DLL bytes are not independently rehashed.

Eight provider bindings are witnessed. Callback fixtures exercise create/join; ordinary controls additionally count two C++ TLS registrations and one explicit exit. Return/explicit-exit probes produce 1111. Earlier 4b54f60a baseline 1111/SDK-40 and changed fixture remain explicit. Earlier SIGSEGV causes remain unproved. Origin/dependency, spaced/read-only path, bare-name and missing-dependency controls are not whole-loader proof.

## Limits and publication

Hidden libc calls, cancellation, arbitrary C++ TLS/DSO-unload combinations, namespace/TLS exhaustion, wider platforms and other native APIs remain outside acceptance. Failed close retains callback roots/leases; this source-reviewed quarantine differs from successful cleanup evidence. Linux comparison 15/16, map 110-114, resources, dirty scheduling and upgrades remain separate work.

Root alone integrates this audit, source-port/changelog evidence and metadata. Verify published source identity and same-head CI before claiming release delivery. The 60/40 work board records acquired evidence and pending integration/CI; it is not a compatibility score.

[api]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/erl_nif_api_funcs.h
[nif]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/erl_nif.c
[domains]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/erl_bif_unique.c#L91
[packing]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/emulator/beam/erl_bif_unique.h#L118
[env]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/erts/doc/references/erl_nif.md
[limits]: https://github.com/erlang/otp/blob/ad05823719d77c8faee87348ea39513d4e2f99c5/system/doc/efficiency_guide/system_limits.md#unique_references
