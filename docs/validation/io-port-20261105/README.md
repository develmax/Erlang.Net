# io port receipt — 2026-11-05

Implementation covers all 53 explicit OTP-29.1.1 exports (52 C# library functions,
one shared native primitive), with original Erlang/BEAM retained. Implementation
preceded the focused comparisons. Final focused acceptance passes on Windows;
no universal compatibility or hosted-CI claim follows from API counts.

## Final admitted results

| Check | Result | Evidence |
| --- | --- | --- |
| Ordinary focused build, no source exclusions | 0 warnings, 0 errors | [Build](final/build.log) |
| Exact retained source and exported API inventory | 53/53; 52 C# library bodies and one native primitive | [Inventory](final/source-audit.json) |
| Default depth8, C# / original Erlang | 666/666 each | [Managed](final/depth-8/Managed.json), [Beam](final/depth-8/Beam.json) |
| Depth0, both backends | 666/666 each | [Managed](final/depth-0/Managed.json), [Beam](final/depth-0/Beam.json) |
| Depth1, both backends | 666/666 each | [Managed](final/depth-1/Managed.json), [Beam](final/depth-1/Beam.json) |
| Depth2, both backends | 666/666 each | [Managed](final/depth-2/Managed.json), [Beam](final/depth-2/Beam.json) |
| Depth3, both backends | 666/666 each | [Managed](final/depth-3/Managed.json), [Beam](final/depth-3/Beam.json) |
| Depth32, both backends | 666/666 each | [Managed](final/depth-32/Managed.json), [Beam](final/depth-32/Beam.json) |
| SDK C# source layout | 0 files need formatting | [Layout](final/style.log) |
| Deliberately changed corpus byte | Refused before execution | [Negative control](final/negative-admission.log) |

The 666-case corpus contains 656 API/protocol/error observations, seven connected
lifecycle/callback/dependency scenarios and three unchanged upstream testcase
bodies: `io_SUITE:calling_self/1`, `otp_5403/1` and `io_lib_fread_literal/1`.
The same cases were compared at six depths in each backend: 7,992 observations,
not 7,992 distinct contracts. Complete values, reasons, stacks, locations,
metadata and protocol messages remain compared. Only fixture-owned device/self
PIDs and fresh opaque error references are alpha-renamed.

[Admission](final/admission.json) binds all twelve reports to the same actual
runtime/stdlib assemblies, records the tool/native assembly hashes and verifies
that the eight owned C# source files are byte-identical to the prebuild snapshot.
The original io source is byte-identical to the pinned tag. The validator checks
the actual owner and the exact registered set for all53 exports. The development
frozen build is not used for any of these final results.

Commands:

```powershell
py -3 tools/Validation/audit-io-source.py
dotnet build tools/Validation/Erlang.IoCompatibility/Erlang.IoCompatibility.csproj -m:1 -v:minimal -o artifacts/io-final-run
pwsh -File tools/validate-io-original.ps1 -Output NEW_DIRECTORY -CaptureOnly -BacktraceDepth 8
dotnet artifacts/io-final-run/Erlang.IoCompatibility.dll . NEW_DIRECTORY Managed
dotnet artifacts/io-final-run/Erlang.IoCompatibility.dll . NEW_DIRECTORY Beam
```

Each of depths0,1,2,3,32 has its own fresh official-OTP capture and the same two
comparison commands. The preserved corpus/exports/features/provenance files are
under each profile, and the exact compiled observer/suite assets are in
[final/assets](final/assets). Source layout was applied/checked using the SDK
Erlang.Style tool against a scoped copy of only the owned files, then copied
back before the final build. The current shared checkout is not a frozen full
repository revision; assembly and owned-source identities are explicit.

The working checkout already contained unrelated dirty library/native/runtime
work. Initial builds failed while concurrent erts_internal, sys, supervisor,
gen_server and error_logger files were incomplete. A temporary frozen profile
was used during development; it is not final-current-source build evidence.
Later ordinary focused builds succeeded without exclusions. A failed copy over
an executing validator also refused the rebuild; only subsequently successful
builds are admitted for their own runs.

Development comparisons preserved in [development](development) include 525/597 (output error
source locations plus a differing user-device fixture), 597/597 after repair,
608/618 then 618/618 after private clause/parser-frame repair, and 658/662 versus
660/662 before geometry-frame and actual callback-dependency closure. Real
callback failures exposed missing erl_features/epp/code dependencies in the
minimal test profile, rather than different io backend results. Feature state
is copied from the actual admitted OTP startup and restored through real native
persistent_term operations. The unchanged io_fread_newlines upstream testcase
was initially invoked without its required CT priv_dir and failed in OTP with
enoent; that failed acquisition is retained and is not a compatibility result.
The selected literal-fread testcase requires no CT filesystem profile.

The original results above were collected in the integration checkout. The PR
was prepared separately on main at `498e10d62`, using only the io changes.
See [the branch replay](main-replay/README.md) for validation on that base.

No full regression, validate.ps1 or global readiness regeneration is run. Full
upstream CT/terminal/filesystem, whole release-manifest startup and
cross-platform/distributed-old-peer/resource acceptance remain separate.

See [backend contract](../../io-backends.md).
