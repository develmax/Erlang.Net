# Original two-node public connected send observation

Source only: this new collector/probe/driver have not been compiled or executed. After review and publication, the additive distribution workflow step shares its existing source-built OTP29.1.1/ad05823719d77c8faee87348ea39513d4e2f99c5 build and evidence. The published318 collector remains unchanged.

The original probe source is byte exact to Root's guest_connected_send319.erl. Its seven checks observe five public connected send forms (send/2, !, send/3 [noconnect], send_nosuspend/2 and send_nosuspend/3 [noconnect]), their same-sender delivery order, and group_leader/2 followed by public send/2. The two real original nodes retain raw PIDs, reference, delivered history, observed leader, primary exception class/reason/stack and independent worker/server cleanup. No priority, trace-token, distribution-pressure, wire-byte equality, SDK live-peer comparison or general distribution completion is claimed.

This adapts the reviewed318 collector and driver. Its owned_process.py, process_capture.py and passive_etf.py are byte exact reused helpers. The source plan checks every executable helper before import. It binds six pinned OTP files, clean tracked source commit, installed root/version, build receipt/log, launcher/emulator hashes, and both actual nodes' exact compiled probe/driver MD5 identities. SHA256 separately binds copied source and compiled BEAM bytes in the output manifest; MD5 is only the two-node image identity check. Source guidance: pinned bif.c send_3 around2697/send_2 around2801, erlang.erl send_nosuspend/2 around11814 and /3 around11848, and existing group_leader/distribution/epmd references. No system Erlang fallback or download is performed.

Run only after source review and publication:

```text
python3 -B tools/Distribution/Erlang.GuestConnectedSendObservations/collect.py --otp-source /absolute/clean-pinned-source --otp-root /absolute/source-built/lib/erlang --build-evidence /absolute/beam-source-build-evidence.json --approved-source-plan-sha256 <reviewed-exact-LF-plan-sha256> --output /absolute/fresh-output
```

A private foreground epmd, unique loopback node names, explicit nonsecret cookie and disabled epmd daemonization preserve ownership. Startup10s, whole session35s, server30s and receives5s are bounded; single compiler/git captures retain10/30s limits. Complete stdout/stderr files are capped at4MiB. Cancellation is checked during startup/waits, all owned process groups close in reverse order, TERM/KILL remain bounded and every parent is reaped. Cleanup does not enumerate/kill unrelated processes and does not claim all arbitrary descendants are globally absent.

The primary probe outcome is captured before fresh-monitor worker kill/join; a primary failure wins over cleanup failure. Successful worker observation requires cleanup reason killed, then the server/client bounded stop/finish handshake. Failure stdout/stderr/ETF/process receipts and report remain retained. The workflow uploads on failure too. Original validation can be claimed only after actual successful execution and typed replay; this source candidate has neither.

Revision3 exports bang/2 and the same public bang-return/delivery contract calls that helper. A future increment may reuse its actual original-compiled BEAM for connected SDK HOST testing; this collector does not execute SDK BEAM, claim a particular opcode shape, or claim that future test has passed.
