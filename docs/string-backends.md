# string backends

The C# alternative and original Erlang/BEAM are integrated under the same71 MFA
exports of OTP29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`). Both execute
on the project's .NET runtime. Original `string.erl` and `string.beam` remain
unchanged; the production application does not invoke an external Erlang VM.

Current [integration evidence](validation/string-otp-merge-20261104/README.md)
records9706 passed full tests,119 reviewed public clauses,8522 complete outcomes
per backend and six independent backtrace-depth profiles. All observed values,
error reasons, complete stacks and binary backing match the pinned oracle.
[All71 API rows](validation/string-otp-merge-20261104/final-api-audit.md) retain
per-signature counts and source handlers. Finite measurements do not promote
compatibility classifications or certify every input, platform or ERTS resource
limit. Final-head CI/review/merge status is tracked in [PR105](https://github.com/develmax/Erlang.Net/pull/105).

## Startup selection

Include exactly one string backend in `RuntimeLibrarySelection`:

```csharp
string[] dependencies = ["erlang", "lists", "unicode", "unicode_util"];
var stringModule = RuntimeModuleSelection.Managed("string", dependencies);
// For original Erlang, select this alternative instead:
// RuntimeModuleSelection.Beam("string", originalStringImage, dependencies)
```

Both choices require explicitly selected `erlang`, `lists`, pinned original
`unicode` and `unicode_util` owners. Create the runtime with
`RuntimeNativePrimitives.InternalScalars413`; this supplies the shared native
number-parser primitive. Missing dependencies or invalid image hashes fail
startup without substituting another backend. Selection is made at startup.

The existing native-only default remains unchanged: it registers
`string:list_to_float/1`. The readiness table's one default registration is
separate from the71 explicitly selectable library exports. Managed string is
supported by `RuntimeModuleSelection.Managed("string", dependencies)` and the
MSBuild manifest; manual registry installation is no longer the only entry.

## MSBuild example

The [complete example](../examples/HelloOtpString/README.md) uses the same calls
and records backend/image/assembly identities:

```powershell
dotnet run --project examples/HelloOtpString -p:StringBackend=Managed
dotnet run --project examples/HelloOtpString -p:StringBackend=Beam
```

`ErlangLibrary` items bind both original image digests and dependencies. Neither
version replaces the other; no package publication or backend default switch is
part of this integration.

## Validation

```powershell
pwsh -NoProfile -File tools/validate-string-original.ps1 -Output artifacts/string-check-unique -IncludeUpstreamSuite -DepthProfiles
```

Use a fresh output directory and an admitted OTP29.1.1 development provider.
The command verifies original execution inputs before and after acquisition,
executes44 unchanged upstream testcases and replays2112 unique upstream calls
inside the8522-case corpus. It independently runs both startup selections and
depths0,1,2,3,8,32. `-RunnerDll` selects an explicitly retained validator.

Returned terms retain list/binary/improper-tail shape and backing; exceptions
retain class, reason and complete source/helper/caller frames. Live compiled
binary-pattern resources require actual native queries and a bijective binding,
with negative controls; arbitrary user references and stack frames remain exact.
No frame filtering or broad reference normalization is used.

Fourteen permanent public reduction-counter tests cover length/reverse, binary
slice, direct-list equality, raw trim/take and empty binary fragments. Negative
controls fail against the applicable previous assemblies. Existing allocation
limits remain unchanged; compiled search patterns are reused only within their
own operation. Full-regression OOM/stack-overflow crashes were not observed.

## Reference, history and scope

The retained `string.erl` Git blob is
`33eaf44956fe832a88da5520ce1340bd6c29a0f4`. The original image SHA256 is
`37C66C6A5DF38AE239CA1EEDEDBD2CE2FBC4B8B4F3EB7605E115D0AB73FDA697`.
Unicode segmentation, mapping and normalization use the selected pinned Erlang
images on .NET; BCL culture/string APIs do not replace their rules. Rune is used
for bounded UTF8 scalar decoding and BigInteger for exact integer arithmetic.

The [earlier failed stage](validation/string-port-20261103/README.md) and archived
local receipts560-562 remain historical, under their own source/assembly
identities. Their5226/37145 counts are not relabelled as current integration
acceptance. Physical resource/timing and universal decimal/platform equivalence
remain separate from finite same-platform observations. No performance advantage
is asserted. Preserve [upstream attribution](OTP-PORTS.md) and
[Apache-2.0 notices](../licenses/README.md).
