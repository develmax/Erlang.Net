# Independent source review before execution

No build, managed HOST run, native fixture/layout execution, install, network, Main edit or Git mutation was performed. Original core manifest and original build directory are unchanged.

Verified all 16 core manifest files and all four input hashes. The recipe hash and four exact pinned Git header hashes match. All APIs consumed from Erlang.Runtime and Erlang.Terms are public. No access expansion is needed for this harness.

The fixture invokes exactly seven callbacks, all implemented with Cdecl UCO signatures: enif_priv_data/0, enif_alloc/1, enif_free/2, enif_get_int/10, enif_make_badarg/18, enif_make_int/19, enif_make_string/27 (numbers are callback ordinals). All 180 declaration ordinals and the two trailing pointers are required. Entry version is 2.18 and minimum ERTS is erts-14.0 in the pinned header. Native compiler/layout evidence is still absent.

## Concrete correction

Original normal Python dict imports vcvars `Path` with mixed case but queries uppercase `PATH`. The revised builder normalizes imported keys to uppercase. The one source substitution preserves all original recipe semantics; the revised plan binds the exact revised builder and copied unchanged layout C. The original plan and manifest remain immutable. See review-receipt.json for hashes.

## Serial commands for root after the current Main gate finishes

Use one exclusive heavy slot, DOTNET_PROCESSOR_COUNT=2, disabled build servers and the same SDK. Commands below are deferred, not executed. Capture separate exact logs and stop immediately on failure. Root must apply an outer process watchdog to build commands; the script itself does not provide compiler process-tree timeout/termination. A timed-out invocation is failure, never a completed proof.

```powershell
$env:DOTNET_PROCESSOR_COUNT = '2'
dotnet build artifacts/memory-allocator-nif-core/NifCore.csproj --disable-build-servers -m:1 -p:UseSharedCompilation=false
# Only after target success with zero warnings/errors:
dotnet artifacts/memory-allocator-nif-core/bin/Debug/net10.0/NifCore.dll
# Only after managed HOST succeeds (its internal wait is 30 seconds), and independent review accepts this exact recipe:
py -3 artifacts/nif-boundary-build-revision2/build_fixture.py --plan artifacts/nif-boundary-build-revision2/source-plan.json --approved-plan-sha256 4F8A20012181EF8728CC8AFF49873DC45CC56AB8ACB3057AA313105BF7662077 --output artifacts/root-nif-fixture-build-revision2
```

The builder refuses any existing output directory. Do not delete/reuse an earlier build or rerun after an observation timeout without checking its actual process. Its build receipt/layout result proves only native structure/export checks. Before loading DLL, root independently checks hydrated exact header/source hashes, all native sizeof/offset fields, exact two-export set, x64 PE/dependency reports, toolchain identities and all receipt hashes. Then supply that independently accepted receipt DLL SHA to the same harness:

```powershell
dotnet artifacts/memory-allocator-nif-core/bin/Debug/net10.0/NifCore.dll D:\Distr\Work\Codex\Erlang.Net\Erlang.Net\artifacts\root-nif-fixture-build-revision2\fixture.dll <independently-accepted-receipt-FixtureDllSha256>
```

The last command is not authorized merely by computing its input hash; it is contingent on all prior gates and review. Require separate original OTP fixture behavior comparison before promoting compatibility evidence.

## Cleanup and lifetime findings

Generation table releases its strong owner/image/unload state after active leases drain and invokes unload outside the table gate. Retired images remain weak keys only. Interlocked lease disposal prevents double release. Native invocation uses a serialized gate; callbacks use a ThreadStatic owner; every normal entry has a scope and opaque environment. Callback failures are stored and rethrown only on managed return. Successful load cancellation retires/unloads acquired private state. Failed/vetoed load reclaims owned allocations and OS handle. Unload records outstanding native allocations before fallback reclamation and reads its counter before OS handle release. The DLL file is opened without write/delete sharing during hash verification and loading.

Unsupported native-created threads, retained stale environment pointers, arbitrary unpopulated callbacks, general filename/error contracts, on_load/purge/replacement transactions, function override installation, dirty scheduling/resource/binary/variadic/upgrade APIs and Linux symbol exports remain actual integration blockers. Public load_nif/2 cannot be declared complete from this internal fixture. Foreign-thread callbacks are not enforced as a generic safe boundary; only this reviewed synchronous fixture is admitted. Native code can always violate the subset, and no sandbox claim is made.
