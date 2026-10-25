# Trusted NIF fixture development host

Standalone repository development tool, not `erlang:load_nif/2`, a general NIF loader, or a new runtime API. No new dependencies; references the existing runtime. The repository tool has passed a local Windows x64 standalone build, internal checks and the exact reviewed native fixture contracts. These results are separate from pending original OTP observations.

No arguments run internal environment/term isolation, native Cdecl callback pointer, allocation ownership, source-image lease and deferred once-only retirement checks. Two arguments run those checks plus the exact reviewed Windows x64 fixture DLL:

```powershell
dotnet run --project tools/Native/Erlang.NifFixtureHost
dotnet run --project tools/Native/Erlang.NifFixtureHost -- <absolute-fixture.dll> <independently-reviewed-fixture-sha256>
```

The trusted hash must come from an independently checked build receipt, source/header/tool identities, native layout and exports. Computing a hash from an arbitrary input DLL does not establish trust. This host implements only seven callbacks: priv_data, alloc, free, get_int, make_badarg, make_int and make_string. Other 173 callback slots and two internal tail pointers are null. Arbitrary NIFs may crash the process and are outside this tool's contract.

The native recipe uses exact OTP-29.1.1 headers at ad05823719d77c8faee87348ea39513d4e2f99c5 and explicit Windows x64 LLP64 size definitions. Run only after reviewing `native/source-plan.json` and its exact hash:

```powershell
py -3 tools/Native/Erlang.NifFixtureHost/native/build_fixture.py --plan tools/Native/Erlang.NifFixtureHost/native/source-plan.json --approved-plan-sha256 <reviewed-plan-sha256> --output artifacts/nif-fixture-build
```

Output must be a new artifacts directory. The recipe records native compiler/linker identities, exact headers/sources, sizeof/offsetof for all 180 callback declarations plus two tail pointers, exact exports and dependencies. Native layout executable runs during building; fixture behavior remains separate. No compiled DLL, bin or obj is included in source.

Callbacks keep managed objects rooted in managed tables and pass opaque native addresses/integer term handles. Callback exceptions stay pending until return to managed code. Native calls are serialized, allocation ownership is tracked, unload's outstanding count is observed before fallback reclamation. Caller cancellation is checked before and after synchronous native calls; native instructions cannot safely be interrupted. Unload ignores caller cancellation for cleanup.

Expired environment pointers may be reused by the allocator; stale callbacks, native-created threads, retained pointers, async/dirty jobs, general binaries/resources, reload/upgrade, on_load, source/BEAM function replacement and purge/delete integration are unsupported. The standalone generation table is not an atomic module-registry transaction. Current-image admission is checked, but concurrent registry replacement/purge is not claimed solved. These limits prevent any whole-erlang or generic NIF completion claim.

Original fixture and native-header adaptation provenance belongs to the consolidated repository OTP port register and existing OTP license. No C# banners or local draft license are introduced.

The manual `Pinned OTP NIF fixture observations` workflow builds the Windows fixture from the exact public headers and collects seven original Linux VM observations from the unchanged C source. Original collection completion is not semantic comparison. It retains raw ETF, complete exceptions and metadata; native unload-counter comparison through Erlang remains unavailable. See original/README.md for the collection scope.
