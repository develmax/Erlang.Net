# Portable prim_file deletion probe

Run the actual production platform adapter without the existing Windows-only
kernel/bootstrap filesystem profile. This executable links the permanent focused
checks from Erlang.Tests; it does not duplicate the deletion implementation.

```sh
dotnet build tools/Validation/Erlang.PrimFilePlatform/Erlang.PrimFilePlatform.csproj -m:1 --artifacts-path artifacts/prim-file-platform-probe
dotnet artifacts/prim-file-platform-probe/bin/Erlang.PrimFilePlatform/debug/Erlang.PrimFilePlatform.dll
```

Run from the repository checkout on Windows, Linux or macOS with the pinned .NET
SDK. The probe locates committed fixture/provenance files through the repository
root; it does not require an installed ERTS or the Windows admission archive.
Windows outcomes and admissible race pairs come from the pinned OTP capture.
Unix expectations come from pinned unix_prim_file.c, with exactly one unlink
winner, raw filename bytes, redundant NUL termination and symlink behavior.
Each successful host run saves raw ETF outcomes and an OS/runtime identity receipt
under artifacts/prim-file-platform-delete. It reports native OTP differential
acceptance only on the platform for which a native oracle was actually captured.
The Linux/Darwin errno/path checks can also execute independently on Windows;
they do not claim that Unix syscalls ran there.

Refresh the Windows reference with tools/validate-dets-original.ps1 -Delete and a
fresh Output directory, then copy the observer BEAM, corpus, races and provenance
into Resources/PrimFileDelete. The capture verifies executable/prim_file/DETS
inputs before and after execution. Windows races are compared as an observed
set of result pairs, preserving their individual raw results and final file
absence. Relative frequency/order is scheduler-dependent. No global regression,
bootstrap admission expansion or full cross-platform DETS claim follows.
