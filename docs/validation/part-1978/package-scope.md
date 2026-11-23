# Part 1978 grouped compiler checks

Inputs use Windows OTP 29.1.1 at `ad05823719d77c8faee87348ea39513d4e2f99c5` and the frozen Part 1977 compiler/source receipts. Part 1976 remains the comparison for the 212 known frame-width regressions.

| Production change | Evidence before this build | Acceptance coverage |
| --- | --- | --- |
| Admit a framed continuation after an unframed badmatch | 139 exact ownership candidates | Compare peak frame widths and preserved branch outcomes |
| Reuse a cons tail scratch across one partial trim | 73 exact candidates in nine parser modules | Cover removing and narrowing a trim and rejection for observable scratch values |
| Place arity checks before allocation | Nine qualified release functions | Preserve original X inputs and failure relay |
| Clear argument roots captured into an outgoing list | 409 pinned slot correspondences across 181 release modules | Prove initialization, retain live values, and compare selected release paths |
| Order independent Y-to-X argument loads | 13 exact pinned physical schedules | Preserve final copies, original line and X0 placement |
| Keep a raw nested checked call result through destructive projections | One exact pinned release witness | Prove both successors and retain original failure value |
| Emit a writable helper for terminal fixed one-bit list builders | Source-derived ASN.1 witness | Check proper, empty and improper list inputs plus a different-width control |

Four earlier compiler assertions are tested after correction to the verified pinned fixtures. The full focused scope has 567 tests and the planned release slice has 230 modules, including 25 previously byte-equal controls. Compare all 212 frame width vectors with Part 1976 and compare native outcomes independently. Run a five-worker whole corpus with fresh compilation of all modules. Run the required local general validation from the integration checkout after exact source transfer. The reference checkout and server state are separate from these operations.

