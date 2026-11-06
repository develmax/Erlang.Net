# Focused rpc acceptance

Complete the implementation before invoking this runner. It compares both rpc
backends against the same pinned original-OTP corpus, verifies all 35 exports
and their source ownership, and records source/corpus/assembly hashes.

```powershell
pwsh -NoProfile -File tools/validate-rpc-original.ps1 -Output artifacts/rpc-new
```

Use a new output directory for each capture. `-CaptureOnly` writes the oracle
without building/running .NET. `-RunnerDll` selects a previously built runner;
the final receipt identifies its assemblies. `-BacktraceDepth` controls both
VMs. Depth8 also runs the connected checks. Other depths compare the same
library contracts without repeating that unrelated dimension.

The script validates the admitted OTP29.1.1 provider and the unchanged upstream
rpc_SUITE source. Native cast/group-leader failure reports are expected test
effects; process exit status and the comparison JSON determine success.
The original capture temporarily unregisters the supervised rex and runs an
unsupervised test rex, then restores the original registration. It never stops
the native kernel supervisor's child. The proxy test restores the original user
registration.

The reference suite is unchanged; `off_heap/1` runs directly. Most other upstream
rpc tests need peer controllers, multiple releases or native ERTS debug controls
and are not represented as passed. Connected checks use two actual .NET runtimes,
monitors and distribution packet codecs/adapters, with explicit bidirectional
packet forwarding. They do not certify external socket/ERTS interoperability.

The observer preserves complete errors and stacks. It projects only opaque
request-reference identity, the implementation identity of one captured rpc
callback (module/name/arity/environment remain checked), and the observer PID
inside C-node error-state data. See `docs/rpc-backends.md` for scope and gates.
