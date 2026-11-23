# Generic map operand types

Baseline is Windows OTP 29.1.1, ad05823719d77c8faee87348ea39513d4e2f99c5. The latest complete uncached base is Part 1875. Released focused images from Parts 1876, 1877, 1878 and 1879 overlay their generated paths. The 957 emitted pairs have 869 raw Type differences. Eight hundred thirty-three tables have missing entries only, 32 have both missing and extra entries, three are pure reorders, and one has only extras. Code also differs in all 869 Type cases.

| Shape | Missing table modules | Representative opcode reach | Inference |
| --- | ---: | --- | --- |
| Cons or nil (0x84) | 468 | is_nil at 1,756 sites in 336 modules, length at 380 sites in 151 modules | Multiple sources of list facts. |
| Atom (0x01) | 386 | Exact equality at 1,167 left operands in 293 modules and select_val at 343 sites in 160 modules | Guard and call-result propagation. |
| Map (0x40) | 283 | get_map_elements input at 3,069 sites in 1,741 MFAs across 261 modules | Direct source proof at generic map-pattern entry. |
| Number (0x28) | 246 | Left operand of addition at 630 sites in 106 modules | Arithmetic and success-branch facts. |

The selected change reuses the existing Map type encoder. Generic EmitMapPattern already emits is_map against its saved Y input. It evaluates keys and extracts fields on the success path without overwriting that saved map value. Pinned beam_ssa_type annotates get_map_element from the concrete map fact after that predicate. The direct source form already writes the annotation; the generic emitter previously omitted it. The existing SSA enablement gate is respected. Native no_ssa_opt omits the type; public managed source preprocessing currently rejects the attribute, so that control is separately retained as oracle evidence.

Untyped guarded accesses occur in 244 inspected emitted modules. Of those, 240 modules have the Map entry missing and expose 2,966 immediately guarded sites across 1,504 functions. A slightly broader straight-block scan detects 3,840 sites in these modules. A typed reference access exists in the same MFA in 1,383 functions across 233 modules. These are scope estimates, and scheduling and optimization can change the number of emitted accesses. The patch is confined to actual lexical map-pattern emission.

Nine modules have only the Map entry missing. An independent compact-byte model produces equal Type chunks for all nine and the synthetic fixture. Code and whole-file equality remain separate. The nine modules are asn1ct_eval_ext, typer, httpd_logger, gen_tcp, gen_udp, tls_client_connection, tls_server_connection, inet_sctp and supervisor_bridge. Among 410 earlier Equal release modules, four have typed map extractions and no untyped immediately guarded occurrences were found. The four controls are prim_tty_sighandler, tls_dyn_connection_sup, dtls_connection_sup and inet_epmd_tls_socket.

Pinned, frozen managed and independently modeled fixture images agree on 14 native value and exception-reason cases. Controls include a non-map, missing key, invalid nested value, external unchecked map access and exact integer/float key and value distinction. Apply check and syntax validation passed. The patch has not been built or run through the complete package gate. The report preserves actual release image paths, audits, raw terms and model scripts.
