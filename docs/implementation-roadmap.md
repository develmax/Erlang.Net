# Executable roadmap

The dependency-aware phases in the assignment remain the target. The initial vertical slice crosses terms/runtime/compiler/OTP/hybrid/build while keeping coverage explicitly partial.

| Phase | State | Next acceptance evidence |
|---|---|---|
| 0 Discovery | Partially compatible | Expand conditional exports, callback signatures/types, generated/NIF modules and OS contracts; finish candidate audits. |
| 1 Terms | Partially compatible | Live OTP oracle for equality/order/ETF, wider refs/external funs, complete formatting/bit operations and limits. |
| 2 Runtime | Partially compatible | Signal queue/race modelling, timers/flags/options, shutdown/cancellation edge cases and scheduler measurements. |
| 3 Compiler | Partially compatible | Add maps and bit syntax, macros/includes/records, try/if/comprehensions, precise positions and optimized IR. |
| 4 BIFs/standard modules | Partially compatible | Extend MFA contracts from inventory, with permanent error/side-effect tests for every function. |
| 5 OTP | Partially compatible | Dynamic supervision, Erlang callback modules, gen_server sys/alias/timeouts, then gen_statem/gen_event/application. |
| 6 Distribution/advanced runtime | Not started | Audit transport candidates, implement pinned node handshake/ETF contracts, exercise two real nodes. |
| 7 Hybrid syntax | Partially compatible | Complete expression-context rules, C# variable boundary and cross-block Erlang scopes; sync diagnostics. |
| 8 Build/IDE | Partially compatible | Independent package versioning, design-time semantics, full source maps/LSP/debugger mapping and multi-target checks. |
| 9 Full hardening | Not started | Applicable OTP suites, differential corpus, property/race fuzzing, comparable performance measurements. |

No phase is declared complete merely because Hello World succeeds. See NEXT_STEPS for the next dependency-ready unit.
