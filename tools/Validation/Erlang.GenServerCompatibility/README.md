# Focused gen_server differential validation

Implementations are complete before these tests execute. The pinned source is OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5. Original Erlang sources and the full gen_server_SUITE reference remain unchanged.

From the repository root:

```powershell
pwsh -NoProfile -File tools/validate-gen-server-original.ps1 -Output artifacts/gen-server/oracle-new
dotnet build tools/Validation/Erlang.GenServerCompatibility/Erlang.GenServerCompatibility.csproj -m:1 --artifacts-path artifacts/gen-server/build
dotnet artifacts/gen-server/build/bin/Erlang.GenServerCompatibility/debug/Erlang.GenServerCompatibility.dll . artifacts/gen-server/oracle-new Managed
dotnet artifacts/gen-server/build/bin/Erlang.GenServerCompatibility/debug/Erlang.GenServerCompatibility.dll . artifacts/gen-server/oracle-new Beam
```

The capture tool requires the existing admitted official OTP provider and checks its complete helper/runtime closure before and after capture. Use a new output directory. Admission binds observer/companion/upstream-source/BEAM, corpus, effects, feature bootstrap and the retained original module. The export set must equal the actual original module exports. Reports compare full terms/stacks and record actual backend owners, assemblies, source hashes and helper images. Optional fourth argument selects one scenario for diagnosis and yields an explicitly incomplete report.

The fixture isolates effect cases in fresh processes, cleans up linked children and uses distinct known owner/server PID markers where needed. An actual existing runtime I/O device serves the group leader. Original gen/proc_lib/sys/io/net_kernel execute on .NET; no library or CT-return substitution is used. A real existing code-server boot fixture is included. Logger primary delivery is disabled, so actual handler effects remain unverified.

The four selected upstream tests run from the unchanged full suite image. Full upstream/platform/resource/distribution/metadata acceptance, broad regression and global readiness are separate. Do not infer universal compatibility from finite counts.
