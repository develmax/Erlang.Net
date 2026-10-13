# Local sequential SEND destination repair

Reference: OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5. Read-only source audit is retained at ../../part228-seq-destination-failure-audit.md.

bif.c do_send (2459,2506,2614,2676) resolves local PID/name/name-node delivery. erl_message.c erts_send_message (698,754-755) emits sequential SEND with receiver->common.id. Ordinary trace_send keeps its original symbolic operand. Alias delivery erl_proc_sig_queue.c 2131 keeps the alias reference. Genuine remote bif.c 2381 / dist.c 1616 keeps its full remote destination and remains outside this repair.

The local ProcessRuntime.SendTraced call now supplies target to SequenceTracing.Sent. Its destination argument remains available for later ordinary tracing composition. Alias and system reply paths are unchanged. The one published fixture assertion changes its first SEND recipient from Target to P; the actual send operand and independent {ok,done} outcome are preserved. Raw failed original228 row, exact old/new source and SHA256 are retained in artifacts/sequence-destination-correction-ledger.json and its raw-row sibling.

Eight new independent source cases cover direct PID, registered atom, local tuple, send/3 nosuspend, alias reference, missing tuple silent drop, missing atom badarg and invalid tuples. No new MFA, host test, exclusions or harness relaxation. Append SequenceDestinationCases.Modules after MonitorContextCases and before the spawn repair provider as Root directs.

No build or original execution has run for this candidate. Root owns style, full validation and original comparison. All other inherited source contracts remain unchanged.

Reuse decision: no dependency added; immutable existing terms, actual runtime send and collector infrastructure reused. The producer branch was ported from Ericsson OTP Apache-2.0 source; consolidated notices for bif.c, erl_message.c, erl_trace.c and erl_proc_sig_queue.c remain in docs/OTP-PORTS.md, with no copied source banners or private license file.
