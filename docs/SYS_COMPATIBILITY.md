# System protocol foundation

Reference: OTP29.1.1 ad05823719d77c8faee87348ea39513d4e2f99c5 sys.erl and gen.erl.
Part335 registers terminate/2,3 only. Status: **Partially compatible**.

Requests use the real monitor/reply protocol, finite-call aliases and noconnect.
Timeout cleanup revokes aliases before checking an already queued reply.
Local infinite calls retain the source plain-monitor optimization. Name/via/global
lookup follows source dispatch where those registry modules are available;
the global registry itself is not implemented by this slice.

Gen_server/supervisor send ACK before callbacks or child shutdown. terminate may
return after ACK while the process still lives; proc_lib stop additionally waits
for its matching real DOWN using the remaining original time budget.

Actual6952 tests and11 integrations pass, including29 compiled scenarios,4 direct
API contracts and1 bare-runtime HOST regression. Original335 comparison is pending.
Suspend/resume, state/status/code change/debugging, full gen/sys protocol, connected
remote-peer behavior and physical/scheduler/performance parity remain incomplete.
