# Part340 source and verification notes

These are preparation notes, not publication or full-suite authority.

## Implementation

- Add six node-qualified proc_lib exports: spawn/2,4; spawn_link/2,4; spawn_opt/3,5. Reuse actual proc_lib initialization, ancestry, initial-call metadata, caller guards and connected erlang spawning.
- Add the five-export RPC dependency: rpc:call/4,5 and erpc:call/4,5,execute_call/4. Preserve local infinite caller execution, finite/always-spawn monitored workers, correlated result DOWN, exception mapping, and abandon/demonitor/queued-result timeout races.
- Make remote proc_lib introspection and labels query real visible nodes through RPC; preserve hidden-node refusal, arbitrary label terms, death, retirement and current connection generation.
- Implement actual connected request/reply, trace-token propagation, atomic monitor/link options, priority and custom tags, foreign aliases and alias33/34 packet delivery on owned distribution queues. Controller transport evidence does not establish physical TCP interoperability.
- Repair nodes/0,1,2 to enumerate actual visible/hidden/pending/known node entries and connection information. Preserve selector unions, deduplication, strict option validation and undefined connection ID for this node.
- Repair apply/3 argument traversal using native MAX_REG-1=1023, preserving function/module checks, improper-tail errors and system_limit at the 1024th cons. Do not conflate this with export MAX_ARG=255. apply/2 is a separate unchanged boundary gap.

## Development history retained

- Connected alias focus initially passed20/21. Native process exit walks origin distributed monitors before links; emit the spawn-monitor DEMONITOR before linked EXIT so a child's reply cannot target the dead owner. Corrected exact-source focus passes21/21.
- RPC actual2 passed6/10: external-fun HOST decoders lacked their real factory, and nodes returned no live connections. Correct those actual dependencies rather than replace remote behavior with a local answer.
- RPC actual3 passed10/11: the target HOST actor waited only on a CLR channel and did not process Erlang signal requests. Change only the test target to a real spawned ReceiveAsync process, retain all assertions and explicitly join it. Actual4 passes11/11, with unchanged production bytes.
- Preserve failed builds and fixtures. Materialize the declared module atom in both independent source fixture and generated catalog because this parser path has no macro preprocessing. Preserve the initial macro proposal; no new macro support is claimed.

## Evidence already completed

- Independent original339 collector:12 actual cleaned scenarios, typed request/reply/TT/alias checks and17 passive positive/refusal controls; guard3096F324C1FB942B2938477E8B2DF97014E08AED5C32F12A01531044896B7829. Installed runtime binaries are receipt-bound, not independently archived/rehashed.
- Published338 Linux CI7105 tests/11 integrations; published339 unchanged CI7105/11. The separate local338 missing-readiness-input failure and successful suffix remain unchanged historical facts.
- Permanent44 fixtures and11 direct MFA selections independently source-reviewed. Root catalog guard2BE9EEFB12F9C16B43C02BB8DB3815BE42B758D79A0063090A41DD31DF732FEA and peerE37A65655E962959F4574B3A4ACCEF0624DDCF05473D73A264D7BE99DFA72BED are source-only evidence.

## Required delivery gates

Parse all44 before guest execution, pass focused44 compiled/11 direct contracts, freeze exact339-based source/package/readiness inputs, and run one full canonical pipeline. Preserve old7105 ordered names and576 registry rows/custom Limits. Expected587 APIs/proc_lib39/modules14 is arithmetic until actual generation and verification. Current-head original comparison must be completed separately; API presence does not establish unrestricted tuning, native physical/scheduler values, full logger/RPC modules or performance parity.
