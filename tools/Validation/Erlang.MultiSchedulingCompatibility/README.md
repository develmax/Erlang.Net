# Physical multi-scheduling acceptance

This permanent validator exercises actual normal actors and dirty CPU/shared-IO
workers. It uses reflection to invoke the runtime's real native scheduling entry
with held native bodies; it does not replace the library or scheduler with mocks.

```powershell
dotnet build tools/Validation/Erlang.MultiSchedulingCompatibility/Erlang.MultiSchedulingCompatibility.csproj -m:1
dotnet run --project tools/Validation/Erlang.MultiSchedulingCompatibility --no-build -- artifacts/new-multi-scheduling-report.json
```

The report path must not exist. Ten groups cover physical single-lane execution,
waiting for an already running normal turn, lazy dirty-pool suspension, normal-only
blocking, active CPU/IO drain, cancelled pending blockers, queued/active shutdown,
a single normal lane, and repeated queue wakeup restoration. Held work is released
in finally paths so a failed assertion does not intentionally strand the runtime.

The matching original OTP contract corpus lives beside
`Erlang.ErtsInternalCompatibility`. It checks the four operations, nested blocker
lists, strong pinning, online-count preservation, owner death, exception cleanup,
and complete helper stacks with the original erts_debug image loaded.

Run only this validator and the connected selection for this task; it does not
request the repository's full regression or generate canonical readiness.
