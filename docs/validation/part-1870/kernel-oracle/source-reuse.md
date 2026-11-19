
# Windows kernel include provenance

The bootstrap library path appears in the debug information when ordinary EPP lookup fails for a library-qualified include. The pinned Windows build uses the bootstrap Erlang system. Its kernel makefile places output under the bootstrap application and adds the in-tree -I../include option. These inputs explain why two references to the same header bytes can record different source paths.

| Reference at OTP 29.1.1 | Observed behavior |
| --- | --- |
| Makefile.in around lines 218 and 234 | Selects the initial bootstrap tree and prepends its tool directory to PATH. |
| lib/kernel/src/Makefile lines 25 and 194 | Uses the BOOTSTRAP_TOP kernel ebin and adds -I../include. |
| lib/compiler/src/compile.erl around lines 2000 and 3103 | Builds the EPP search options and shortens the source argument relative to the working directory. |
| lib/stdlib/src/epp.erl around lines 1000 and 1510 | Uses the first successful ordinary path lookup, then falls back to the application path for include_lib. |

The tagged bootstrap and in-tree header objects match by Git blob for file.hrl, logger.hrl, inet.hrl, inet_sctp.hrl and stdlib/include/ms_transform.hrl. The adapter maps the logical bootstrap paths to the corresponding installed application inputs. The corpus source-header input is a second physical representation of the logical kernel source directory. It supplies local source socket_int.hrl, which is absent from the Windows installation and needed by gen_tcp_socket and gen_udp_socket. Their initial failed receipts are retained.

The source checkout at OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5 was queried with read-only Git commands. Its OTP compiler, EPP and makefile sources carry Apache-2.0 notices. This change uses the existing C# IncludePathMap and .NET filesystem APIs. It adds no dependencies or copied third-party code. Release BEAM bytes are comparison inputs only.

The isolated cohort reused the sealed Part1869 compiler binary in both lanes. All 104 kernel modules were selected. The only raw differences between the two lanes were 29 Dbgi chunks and all 29 now match the OTP chunks. Both lanes report 11 Equal, 93 Different, zero Unsupported and zero SourceRejected. The permanent fixture verifies ordinary include precedence, source-directory mirror, bootstrap origin and local include-lib shadowing. ERTS 17.1 accepted all 93 generated images at prepare-loading without replacing its own resident modules. The fixture ran as an ordinary loaded module and verified the values of the selected macros in both cases.

