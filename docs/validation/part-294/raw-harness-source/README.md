# Supplied pressure collector raw-capture controls

Source-only artifact prepared by the lifecycle worker. No build, run, Main edit, Git mutation, native OTP execution or network call was performed. Root must run both controls before treating this as validation.

The harness loads the exact supplied standalone collector DLL, records its path, SHA256, MVID and reflected `NodeIdentityContrasts.NodeProcess.Run` method, and invokes that internal method. It does not link a Runtime project, call the collector entrypoint or substitute a local copy of NodeProcess. The supplied DLL must remain byte-identical through the controls.

The first Python fixture writes exact non-UTF8/binary stdout and stderr and exits 3. The harness requires exact bytes, actual exit3, TimedOut=false and RawComplete=true, including the raw `.exit` and `.raw-complete` files.

The second fixture exits its parent successfully while a directly owned Python child retains both redirected pipe handles for 50 seconds. It requires exit0, TimedOut=false and RawComplete=false, exact retained binary bytes, and bounded elapsed time. The capture allowance is 12 seconds and the owned cleanup join is 3 seconds; the complete inherited-pipe control must finish within 15 seconds. These deadlines test incomplete stream capture, not the 45-second child-process timeout.

Cleanup always runs in the inherited control's finally. The controlled Python parent records its child's PID, exact Windows process creation FILETIME, executable and supplied nonce before exiting. The C# cleanup verifies the live process against that identity and kills only that PID, never a discovered process name or unverified process tree. It records a separate cleanup receipt even when cleanup or capture join fails. The Python parent kills its own directly held child handle if identity-record creation fails. These identity-bound controls are explicitly Windows-only.

Run from repository root, serially with other heavy work:

```powershell
$env:DOTNET_PROCESSOR_COUNT = '2'
dotnet build artifacts/root-pressure294-raw-focus/PressureRawFocus.csproj --disable-build-servers -m:1 -nr:false -p:UseSharedCompilation=false -p:RestoreDisableParallel=true
dotnet artifacts/root-pressure294-raw-focus/bin/Debug/net10.0/PressureRawFocus.dll tools/Erlang.DistributionPressureObservations/bin/Debug/net10.0/Erlang.DistributionPressureObservations.dll py artifacts/root-part294-raw-controls
```

The exact argument order is `collector-dll python-executable-or-py-launcher new-output-directory`. `py` automatically receives `-3`; an absolute Python executable receives the scripts directly. The output directory must not already exist. The build copies the three fixture scripts beneath the harness output's `controls` directory and the receipt hashes the actual copies executed.

Expected proof closure: assembly-binding.json, raw stdout/stderr/exit/completeness files for both cases, owned-child.json, owned-child-cleanup.json, and receipt.json. PASS requires the exact supplied assembly and both real synthetic subprocess observations. It is not original OTP, pressure-protocol parity, a Main Runtime assembly result, or a completed erlang implementation claim. Preserve failures and raw files as new evidence; do not overwrite a prior output directory.
