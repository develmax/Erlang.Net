# Earlier build and fixture diagnostics

These are summaries of observed tool outputs, not reconstructed raw build logs.

Early ordinary builds of the rpc validation project failed in unrelated live
drafts: duplicate/scoped variables and BigInteger conversions in Supervisor,
duplicate generated constants in Supervisor/GenServer, an incomplete
ErrorLoggerSourceFields extraction, and temporary Sys syntax errors. A
no-dependencies Kernel build also encountered the concurrent erpc captured-stack
friend-access change and Logger IReadOnlyList Length usages. These files were
not repaired or reverted by the rpc task.

Temporary `artifacts/rpc-focused-build` projects compiled the owned rpc sources
over a frozen HEAD Kernel shell and prebuilt dependencies to continue diagnosis.
Those preliminary assemblies are not the final acceptance binaries. Shared
output contention also caused copy retries/missing output during early builds.

The final normal rpc project build succeeded without exclusions, warnings or
errors. Its complete output was copied to `artifacts/rpc-final-bin-20261105` and
used unchanged for all final backtrace profiles and connected checks.

The first observer fixture omitted net_kernel, causing the observer scenario to
wait for an already-failed child. The complete original dependency declaration
then needed erlang, rpc_SUITE and the ordinary crypto module name imported by
unused net_kernel functions. Final checks execute the real net_kernel node
monitor helper; no monitor stub was introduced. All final scenarios finish.
