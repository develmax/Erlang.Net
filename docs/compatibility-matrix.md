# Compatibility matrix

Evidence identifiers refer to permanent cases in `tests/Erlang.Tests/Program.cs`. Full source inventory: `otp-inventory.json`; actual registered MFAs and their direct tests: `supported-mfas.json`. Latest executable results live under artifacts and are copied to `docs/validation` at checkpoints. Nothing is globally Verified compatible before exact-version oracle evidence.

| Feature | Status | Evidence and remaining boundary |
|---|---|---|
| Atoms/integers/floats | Partially compatible | terms/exact-numeric-equality, large-integer-double-comparison, signed-zero-otp29, atom-codepoint-order; limits/full formatting pending |
| Tuples/lists/improper lists/maps | Partially compatible | terms/tuple-arity-first, improper-list, map-exact-keys, map-integer-before-all-floats, structural-hash |
| Binaries/bitstrings | Partially compatible | terms/immutable-binary, etf/roundtrip-all-supported-kinds; full bit operations/source syntax pending |
| PID/reference/port/fun values | Partially compatible | terms/type-order, ETF roundtrip, compiler/list-fun-map; exact distributed ordering/wide refs/external funs pending |
| Integer-to-float coercion | Partially compatible | terms/integer-double-*, compiler/numeric-*; nearest-even/finite operand conversion in runtime and float segments; six selected oracle cases matched within 160/160; full arithmetic scope pending |
| Pattern bindings | Partially compatible | patterns/repeated-variable, already-bound, list-tail; compiler binding/single-assignment/unsafe-variable tests |
| Selective receive | Partially compatible | mailbox/later-match-preserves-order, zero-timeout-retains, timeout, concurrent-arrival, cancel, sender-order-stress; compiler clause/guard tests |
| Local processes/links/monitors/exits | Partially compatible | runtime/registration-cleanup, monitor-down, monitor-noproc-and-flush, link-trap-exit, link-propagation, normal-link-exit-ignored, kill-untrappable |
| Scheduling | Implemented | runtime/1000-waiting-processes, compiler/tail-recursion-50000; thread-pool continuation/reduction foundation, no BEAM fairness claim |
| ETF subset | Partially compatible | etf/reference-vectors, roundtrip-all-supported-kinds, malformed-input-limits, random-integer-property; fun tags/wide reference vectors pending |
| Erlang parser/analyzer/emitter | Partially compatible | compiler/*; lexer/Pratt AST, scopes/guards/module exports, generated AST C#; full grammar/IR/native lowering pending |
| Map source construction/updates/patterns | Partially compatible | compiler/map-*, patterns/map-rollback, hybrid/map-*, generated map_source.erl and PackageReference integration; full guard/BIF set/comprehensions/oracle pending |
| is_map/map_size/map_get/is_map_key | Partially compatible | Four direct MFA cases plus compiler map-BIF exact-key/error/guard/pattern regressions; oracle and complete error stacks pending |
| Bitstring source construction | Partially compatible | compiler/bits-* plus .erl/hybrid/PackageReference generation; integer/binary/float16/32/64 segments, direct rounding and integer coercion; UTF8/16/32 and UTF strings now exist; integer/float literal-string modifiers and empty validation now exist; 183 selected oracle cases match at 3990623; full suites/ordering/resources pending |
| Float bit segments | Partially compatible | compiler/bits-float-*; 27 new tests including 63,488 finite binary16 roundtrips and generated .erl/hybrid/consumer checks; 123/123 matched at 19774f9; full applicable inputs/reference suites pending |
| UTF bit segments and strings | Partially compatible | compiler/bits-utf-*; malformed/scalar/endian/unaligned/string/rollback checks and 1,280 deterministic roundtrips; 31 selected UTF cases matched within complete 160/160 at 666c4b5; full applicable suite pending |
| Bitstring source patterns | Partially compatible | compiler/bits-pattern-* plus generated unpack/1 and hybrid/PackageReference checks; signed/unsigned integer extraction, binary slices/rest, guard sizes and prior-segment bindings; full scope/segment/oracle evidence pending |
| is_bitstring/bit_size/byte_size | Partially compatible | Three direct MFA cases plus compiler/bit-size-byte-rounding, predicate/size/error/guard/pattern-key regressions; oracle/resource-limit fidelity pending |
| Core BIFs/lists/maps/io | Partially compatible | mfa/MODULE:FUNCTION/ARITY for every registered function; full errors/options/module coverage pending |
| gen_server | Partially compatible | otp/gen-server-call-cast-info-stop, crash-monitor, timeout, init-error; aliases/sys/full callback protocol pending |
| supervisor | Partially compatible | otp/supervisor-OneForOne, OneForAll, RestForOne, transient-temporary-normal, temporary-abnormal-no-restart, restart-intensity; dynamic children/full childspecs pending |
| Mixed C#/Erlang | Partially compatible | hybrid/* and HelloHybrid integration; async receive/case/fun blocks at selected boundaries; arbitrary inline syntax/sync contexts pending |
| MSBuild/local package | Partially compatible | tools/validate.ps1: output, incrementality, negative preprocessing, clean/rebuild, local consumer/tool; SDK/TFM/design-time expansion pending |
| Source diagnostics/maps | Partially compatible | ERL001-008 parser/semantic errors, #line and line preservation; AST columns/full debugger/IDE mapping pending |
| Differential verification | Implemented | OTP-29.1.1 run 37999487360 at 3990623 matched 183/183 selected values/class/reason cases; original report/head/ZIP metadata in validation/part-026. 160 historical cases preserved plus 23 literal-string additions. Compiled-reference-module/optimizer/stacks/signals/side effects/full reference suites remain unverified |
| Full expanded source inventory | Investigated | 1,289 source modules / 38,622 explicit exports / 524 BIF entries; expansion/contracts incomplete |
| gen_statem/gen_event/application/SASL | Not started | Not implemented |
| Distribution/remote links/monitors/RPC | Not started | No real-node interop claim |
| ETS/DETS/hot code/ports/NIF/release services | Not started | No runtime implementation |
| Full OTP suite compatibility | Not started | Reference suites have not run |
| Performance measurements | Implemented | Exploratory benchmark harness, separate report; no BEAM comparison or general throughput claim |
| Literal-string bit construction | Partially compatible | compiler/bits-string-*: independent integer/float bytes, one size evaluation, empty validation and pattern diagnostics; 314 local/same-head CI tests and full integration; static all rejected for non-binary segments; corrected 183/183 selected oracle cases match at 3990623; initial abort preserved in part-025; full ordering/resources/stacks/reference suites pending |
| lists:nth/2, nthtail/2, seq/2,3 | Partially compatible | Four direct MFA tests, eleven boundary regressions, BigInteger/improper prefixes/step errors; 36 selected cases match within complete 219/219 at 6f34dc9; seq length int.MaxValue local limit |
| Module readiness CLI | Implemented local tooling | Actual registry/pinned export+BIF union/current passed tests; wrong evidence and stale reports fail, stable JSON and packaged CLI pass; API declaration ratio is not semantic completion |
| lists:keyfind/3,keymember/3,keysearch/3 and reverse error selection | Partially compatible | Three direct MFA and twelve boundary tests; first matching tuple/improper tails/position/small numeric fast path; thirty selected cases match within complete 249/249 at 85bd19f; pinned 64-bit position limit, complete stacks/resources unverified |
| Compiled-reference-module track | Partially compatible | Fourteen generated C# Release assembly fixtures and three infrastructure regressions; reference compile:forms/code:load_binary/default optimizations path added, fourteen cases now match within complete 249+14 at b13b56a; no full optimizer/stacks/resources/reference-suite claim |
