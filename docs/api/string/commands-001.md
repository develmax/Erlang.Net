# string: commands — 1

Generated from the baseline-bound compatibility ledger. Edit the ledger, then regenerate. Rendering this snapshot does not revalidate current implementation bindings, refresh runtime registration counts or grant compatibility credit.

[Module index](index.md) · [All modules](../../API_COMPATIBILITY.md)

| Command | Compatibility | Audited scope | Evidence | Unresolved limits |
| --- | --- | --- | --- | --- |
| `string:list_to_float/1` | Needs verification for 100% compatibility | Native float-prefix grammar and untouched-tail boundary reviewed against pinned bif.c/sys_float.c; the all71 source audit and targeted exact-rational midpoint corpus record separate backend observations. Returned error tuples are normal values. No universal decimal/platform/resource claim. | [Full evidence](../evidence/4b81a3fa7e5645146d406e79817b52340719ae2f64b7d757c33b5018debb80e2-001.md) | Universal decimal rounding across every input/platform, physical ERTS resource/scheduler behavior and complete metadata equivalence remain unproven. Same-platform grammar, suffix, signed-zero, underflow/overflow and exact-rational midpoint observations are recorded in Part562. |


