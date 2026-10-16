# Physical process-resource implementation remains open

The status correction changes no runtime behavior. process_info/1 omits four pinned physical fields; process_info/2 rejects their otherwise valid keys. The current logical subset and its actual projected native evidence remain useful, but cannot establish a complete resource contract.

Do not invent positive heap/stack counts, use zero as an unmeasured value, or substitute node-wide CLR GC counters for per-Erlang-process data. Returning undefined instead of native values would still be a documented difference; it would not establish OTP parity.

A future implementation needs an explicit process-owned resource/accounting model: what data and continuations belong to a process, units and lifetimes, shared binaries, current/old heaps or their managed counterpart, stack ownership, collection counters and configuration. Values must come from real tracked state under appropriate ownership/snapshot synchronization. Cancellation, process death and concurrent query semantics remain part of the contract.

Exact BEAM physical word counts cannot be inferred from arbitrary CLR object layouts. If the managed model intentionally exposes different values, retain a Known differences classification for that observable resource contract. Implementing logical data alone is not completion of the physical API.

The two previously drafted presence/admission witness sources have not been executed here and are not counted as passing tests. A future implementation must add fresh native/managed observations for those fields, invalid keys, default order and live/dead/other-process domains, then re-audit status with actual evidence. No full-module or performance compatibility follows from this report.
