# API compatibility

Generated from the baseline-bound compatibility ledger. Edit the ledger, then regenerate. Rendering this snapshot does not revalidate current implementation bindings, refresh runtime registration counts or grant compatibility credit.

Compatibility is tracked per command in an explicit, baseline-bound audited ledger. Direct registry tests establish API presence only. Verified requires an audit of the command's complete observable semantic contract on the supported Erlang term model, actual original OTP comparisons, and no known or unresolved semantic gaps. Selected-case or subcontract-only evidence remains Needs verification. Verification does not claim identical internal VM layout, timings or performance; APIs exposing resource values or options still require their complete observable contracts.

Pinned baseline: OTP-29.1.1 (`ad05823719d77c8faee87348ea39513d4e2f99c5`).

[Source ledger](mfa-compatibility.json) retains every evidence path, SHA-256, scope and limit. Identical evidence sequences share a linked record; their order and full contents are preserved.

[Generation and verification](api/README.md).

## Modules

| Module | Commands | 100% compatible | Known differences | Needs verification |
| --- | ---: | ---: | ---: | ---: |
| [application](api/application/index.md) | 4 | 0 | 0 | 4 |
| [binary](api/binary/index.md) | 31 | 31 | 0 | 0 |
| [erlang](api/erlang/index.md) | 351 | 83 | 13 | 255 |
| [erpc](api/erpc/index.md) | 3 | 0 | 0 | 3 |
| [error_logger](api/error_logger/index.md) | 2 | 0 | 0 | 2 |
| [erts_internal](api/erts_internal/index.md) | 7 | 0 | 0 | 7 |
| [gen_server](api/gen_server/index.md) | 10 | 0 | 0 | 10 |
| [io](api/io/index.md) | 1 | 0 | 0 | 1 |
| [lists](api/lists/index.md) | 91 | 0 | 0 | 91 |
| [logger](api/logger/index.md) | 26 | 0 | 0 | 26 |
| [maps](api/maps/index.md) | 34 | 0 | 0 | 34 |
| [math](api/math/index.md) | 26 | 0 | 0 | 26 |
| [ordsets](api/ordsets/index.md) | 21 | 6 | 15 | 0 |
| [proc_lib](api/proc_lib/index.md) | 39 | 0 | 11 | 28 |
| [proplists](api/proplists/index.md) | 22 | 22 | 0 | 0 |
| [queue](api/queue/index.md) | 38 | 38 | 0 | 0 |
| [records](api/records/index.md) | 8 | 0 | 0 | 8 |
| [rpc](api/rpc/index.md) | 2 | 0 | 0 | 2 |
| [string](api/string/index.md) | 1 | 0 | 0 | 1 |
| [supervisor](api/supervisor/index.md) | 14 | 0 | 0 | 14 |
| [sys](api/sys/index.md) | 2 | 0 | 0 | 2 |

## 100% compatible

<a id="erlang"></a>

- [erlang](api/erlang/index.md): 83 commands.
<a id="proplists"></a>

- [proplists](api/proplists/index.md): 22 commands.
<a id="queue"></a>

- [queue](api/queue/index.md): 38 commands.
<a id="binary"></a>

- [binary](api/binary/index.md): 31 commands.
<a id="ordsets"></a>

- [ordsets](api/ordsets/index.md): 6 commands.

## Known differences

<a id="erlang-1"></a>

- [erlang](api/erlang/index.md): 13 commands.
<a id="proc_lib"></a>

- [proc_lib](api/proc_lib/index.md): 11 commands.
<a id="ordsets-1"></a>

- [ordsets](api/ordsets/index.md): 15 commands.

## Needs verification for 100% compatibility

<a id="erlang-2"></a>

- [erlang](api/erlang/index.md): 255 commands.
<a id="lists"></a>

- [lists](api/lists/index.md): 91 commands.
<a id="maps"></a>

- [maps](api/maps/index.md): 34 commands.
<a id="records"></a>

- [records](api/records/index.md): 8 commands.
<a id="io"></a>

- [io](api/io/index.md): 1 commands.
<a id="gen_server"></a>

- [gen_server](api/gen_server/index.md): 10 commands.
<a id="supervisor"></a>

- [supervisor](api/supervisor/index.md): 14 commands.
<a id="logger"></a>

- [logger](api/logger/index.md): 26 commands.
<a id="application"></a>

- [application](api/application/index.md): 4 commands.
<a id="error_logger"></a>

- [error_logger](api/error_logger/index.md): 2 commands.
<a id="proc_lib-1"></a>

- [proc_lib](api/proc_lib/index.md): 28 commands.
<a id="sys"></a>

- [sys](api/sys/index.md): 2 commands.
<a id="erpc"></a>

- [erpc](api/erpc/index.md): 3 commands.
<a id="rpc"></a>

- [rpc](api/rpc/index.md): 2 commands.
<a id="erts_internal"></a>

- [erts_internal](api/erts_internal/index.md): 7 commands.
<a id="math"></a>

- [math](api/math/index.md): 26 commands.
<a id="string"></a>

- [string](api/string/index.md): 1 commands.
