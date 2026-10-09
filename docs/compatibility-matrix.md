# Compatibility matrix

Evidence identifiers refer to permanent cases in `tests/Erlang.Tests/Program.cs`. Full source inventory: `otp-inventory.json`; actual registered MFAs and their direct tests: `supported-mfas.json`. Latest executable results live under artifacts and are copied to `docs/validation` at checkpoints. Nothing is globally Verified compatible before exact-version oracle evidence.

| Feature | Status | Evidence and remaining boundary |
|---|---|---|
| Atoms/integers/floats | Partially compatible | terms/exact-numeric-equality, large-integer-double-comparison, signed-zero-otp29, atom-codepoint-order; limits/full formatting pending |
| Tuples/lists/improper lists/maps | Partially compatible | terms/tuple-arity-first, improper-list, map-exact-keys, map-integer-before-all-floats, structural-hash |
| Binaries/bitstrings | Partially compatible | terms/immutable-binary, etf/roundtrip-all-supported-kinds; full bit operations/source syntax pending |
| PID/reference/port/fun values | Partially compatible | terms/type-order, ETF roundtrip, compiler/list-fun-map; exact distributed ordering/wide refs/external funs pending |
| Pattern bindings | Partially compatible | patterns/repeated-variable, already-bound, list-tail; compiler binding/single-assignment/unsafe-variable tests |
| Selective receive | Partially compatible | mailbox/later-match-preserves-order, zero-timeout-retains, timeout, concurrent-arrival, cancel, sender-order-stress; compiler clause/guard tests |
| Local processes/links/monitors/exits | Partially compatible | runtime/registration-cleanup, monitor-down, monitor-noproc-and-flush, link-trap-exit, link-propagation, normal-link-exit-ignored, kill-untrappable |
| Scheduling | Implemented | runtime/1000-waiting-processes, compiler/tail-recursion-50000; thread-pool continuation/reduction foundation, no BEAM fairness claim |
| ETF subset | Partially compatible | etf/reference-vectors, roundtrip-all-supported-kinds, malformed-input-limits, random-integer-property; fun tags/wide reference vectors pending |
| Erlang parser/analyzer/emitter | Partially compatible | compiler/*; lexer/Pratt AST, scopes/guards/module exports, generated AST C#; full grammar/IR/native lowering pending |
| Map source construction/updates/patterns | Partially compatible | compiler/map-*, patterns/map-rollback, hybrid/map-*, generated map_source.erl and PackageReference integration; full guard/BIF set/comprehensions/oracle pending |
| is_map/map_size/map_get/is_map_key | Partially compatible | Four direct MFA cases plus compiler map-BIF exact-key/error/guard/pattern regressions; oracle and complete error stacks pending |
| Bitstring source construction | Partially compatible | compiler/bits-* plus .erl/hybrid/PackageReference generation; integer/binary segments only, patterns/float/UTF/string modifiers/oracle pending |
| Core BIFs/lists/maps/io | Partially compatible | mfa/MODULE:FUNCTION/ARITY for every registered function; full errors/options/module coverage pending |
| gen_server | Partially compatible | otp/gen-server-call-cast-info-stop, crash-monitor, timeout, init-error; aliases/sys/full callback protocol pending |
| supervisor | Partially compatible | otp/supervisor-OneForOne, OneForAll, RestForOne, transient-temporary-normal, temporary-abnormal-no-restart, restart-intensity; dynamic children/full childspecs pending |
| Mixed C#/Erlang | Partially compatible | hybrid/* and HelloHybrid integration; async receive/case/fun blocks at selected boundaries; arbitrary inline syntax/sync contexts pending |
| MSBuild/local package | Partially compatible | tools/validate.ps1: output, incrementality, negative preprocessing, clean/rebuild, local consumer/tool; SDK/TFM/design-time expansion pending |
| Source diagnostics/maps | Partially compatible | ERL001-008 parser/semantic errors, #line and line preservation; AST columns/full debugger/IDE mapping pending |
| Differential verification | Blocked | Runner implemented, exact OTP 29.1.1 executable absent; no live oracle results claimed |
| Full expanded source inventory | Investigated | 1,289 source modules / 38,622 explicit exports / 524 BIF entries; expansion/contracts incomplete |
| gen_statem/gen_event/application/SASL | Not started | Not implemented |
| Distribution/remote links/monitors/RPC | Not started | No real-node interop claim |
| ETS/DETS/hot code/ports/NIF/release services | Not started | No runtime implementation |
| Full OTP suite compatibility | Not started | Reference suites have not run |
| Performance measurements | Implemented | Exploratory benchmark harness, separate report; no BEAM comparison or general throughput claim |
