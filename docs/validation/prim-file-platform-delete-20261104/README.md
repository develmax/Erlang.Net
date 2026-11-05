# Part592 — OTP-style platform file deletion

Baseline: OTP-29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5.
This is the focused follow-up to DETS PR119, not a complete Unix filesystem
or runtime acceptance certificate. Original OTP source/BEAM assets are unchanged.

## Behavior

The common `PrimFileDeleteOperations` builds Erlang results and selects an OS
backend before path marshalling. Windows owns DeleteFileW and the existing UTF16
path conversion. It now preserves the original error before inspecting a failed
directory deletion, returns EPERM for directories and EACCES for invalid native
names. Linux/macOS use libc unlink with the original byte sequence and immediate
errno capture. No Unicode roundtrip, File.Exists precheck or Windows helper
occurs in the Unix route. EISDIR is normalized to EPERM as in OTP. Linux/Darwin
numeric ABI values use different dictionaries; unknown values become errno_N.

Only public errno ABI constants/names were inspected in Linux UAPI and Apple XNU
headers, then filtered through OTP erl_errno_str.c. No header body/helper binary
or new package is distributed. [Reference URLs and hashes](abi-reference-inputs.json).

## Actual focused results

| Check | Result | Evidence |
| --- | --- | --- |
| Final ordinary test build |0 warnings/errors | Reproduction command below |
| Native deletion, path/ABI and existing Windows file groups |3/3 passed | [delete-final.json](delete-final.json) |
| DETS both primary and selected upstream groups |4/4 passed | [dets.json](dets.json) |
| Portable standalone runner build/run |0 warnings/errors; pass on Windows | [portable-probe.log](portable-probe.log) |
| SDK affected-source layout |0 files requiring changes | Isolated source root command below |

The primary deletion comparison has six whole outcomes: existing, repeated,
missing, directory, BMP Unicode and readonly. Additional actual OS checks cover
32 concurrent deletion pairs, final absence, shared-open file deletion, redundant
native NULs and a non-directory ancestor. Unix-only live symlink checks execute
when the runner is run on Unix. Cross-ABI unit checks on Windows cover Linux and
Darwin differences, raw non-UTF8 bytes, malformed paths and cancellation; these
are not claimed as Unix syscall execution.

The pinned native Windows race capture contains27 [ok,ok],4 [ok,{error,eacces}]
and1 [ok,{error,enoent}] pairs. Compare each runtime pair against those observed
patterns after exchanging the two unnamed worker positions, and require final
file absence. Frequency and relative worker scheduling are not compared. Unix
keeps exactly one ok and one ENOENT. Preserve raw [native pairs](native-deletion-races.etf)
and [runtime observations](observations/); no error atom is erased.

The initial new test incorrectly required one Windows winner and failed with
[ok,ok]. The actual unchanged OTP oracle disproved that assumption. Its failed
[report](delete.json) and raw observations remain separate from the passing
report. The initial observer also tried a supplementary Windows filename that
OTP internal_name2native rejects; use a BMP Unicode name for the accepted native
file-operation comparison. The [initial failure](initial-observer-setup-failure.log)
is preserved. No previously accepted expected result or DETS fixture was changed.
A temporary comparison-delegate compiler diagnostic and a locked output copy were
corrected before the final clean builds; no failed build is labelled successful.

## Reproduction

From the repository root, with the admitted official Windows reference available:

```powershell
pwsh -File tools/validate-dets-original.ps1 -Delete -Output artifacts/fresh-delete-native
dotnet build tests/Erlang.Tests/Erlang.Tests.csproj -m:1 --artifacts-path artifacts/prim-file-platform-build
dotnet artifacts/prim-file-platform-build/bin/Erlang.Tests/debug/Erlang.Tests.dll --case runtime/prim-file/delete-platform-contracts --case runtime/prim-file/delete-errno-and-path-contracts --case HOST/prim-file410/windows-native-files-and-validation --report artifacts/prim-file-platform-validation/delete-final.json
dotnet artifacts/prim-file-platform-build/bin/Erlang.Tests/debug/Erlang.Tests.dll --case runtime/dets/managed-public-otp-contracts --case runtime/dets/original-public-otp-contracts --case runtime/dets/managed-upstream-cases --case runtime/dets/original-upstream-cases --report artifacts/prim-file-platform-validation/dets.json
```

The capture checks the manifest, executable and original prim_file/DETS input
hashes before and after execution. [Native provenance](native-provenance.json),
[changed source identities](source-inputs.json) and [test/tool assemblies](assemblies.json)
are recorded separately. SDK layout used `Erlang.Style --write` then `--check`
on an isolated root containing the changed C# sources; the final check returned0.

The [standalone portable runner](../../../tools/Validation/Erlang.PrimFilePlatform/README.md)
links these same permanent checks and runs without kernel/regex/ERTS admission:

```sh
dotnet build tools/Validation/Erlang.PrimFilePlatform/Erlang.PrimFilePlatform.csproj -m:1 --artifacts-path artifacts/prim-file-platform-probe
dotnet artifacts/prim-file-platform-probe/bin/Erlang.PrimFilePlatform/debug/Erlang.PrimFilePlatform.dll
```

## Explicit remaining platform scope

Live execution here is Windows only. WSL is unavailable on this host; neither
Linux nor macOS native unlink/OTP differential execution was performed. The
portable runner is ready for those host checks; Unix expectations are explicitly
source contracts until receipts exist. The existing full bootstrap filesystem
registration still requires Windows, and other path/info/raw-file/kernel/native
admission gates are not changed by this deletion work. Do not infer complete
Linux/macOS DETS startup from one POSIX operation. No full regression, validate.ps1,
global readiness refresh, CI rewrite/manual dispatch, merge or review resolution
was performed.
