# Restore outgoing tuple projections after retained-tuple lowering

The Part 1858 compiler is `B58F2660DC83BFBC3E18C52E131ECCD7FBAC07DCB3BB2000E7C769561E3781BE`, with source overlay `894B580A8A75727C848B12BBD0590DCD5652024685521E98378E3AA7A0F6F239`. Both source fingerprints came from the exact frozen run. The reference source and Windows release images were read only.

Both lost function matches pass through the retained-tuple source specialization added in Part 1857. Each function has a checked tuple entry, one early field used by its producer call, and at least two other lifetime-disjoint fields read to build the returned tuple. The specialization has two distinct phases. It preserves the whole checked tuple in Y0, then extracts the first-use field back into the original X receiver. `ReadEntryCall` subsequently moves that field to its real outgoing register. Part 1858 thus does a temporary X0 write and copy before replacing X0 anyway. The existing projection-copy writer cannot remove this register change because its shortcut treats the intervening overwrite of the temporary register as a blocker.

| Witness | Pinned and earlier outgoing destination | Part 1858 destination and copy |
|---|---|---|
| `ct_doctest:clean_stacktrace_item/1` | field 3 to X2 | field 3 to X0, then move X0 to X2 |
| `test_server:rewrite_loc_item/1` | field 3 to X1 | field 3 to X0, then move X0 to X1 |

The related exact raw census found both direct-reference correspondences and no other saved Part 1858 functions with the same conservative redundant-copy window. The raw detector covers the explicit overwritten input followed by an ordinary producer call and retains the published per-function evidence. The source proof can also apply to shapes outside this raw signature.

The source emitter now uses the eventual outgoing register for a field appearing exactly once in the producer arguments, provided no other outgoing move needs that register's previous value. It removes only the now-redundant projected operand move. Existing checked receiver shape, saved complete tuple, allocation roots and delayed post-call reads remain in place. It does not infer call types or reorder an aliasing swap.

The permanent retained-tuple fixture adds two outgoing-register positives, including a tuple receiver originally in X1, along with the existing legacy swap and a two-use negative. Runtime assertions check their values. Separate pinned erlc and native execution succeeded on all four shapes. The pinned listing confirms direct X1 and X2 in the positives and the actual swap path when the outgoing slot holds another live argument. For repeated use the pinned compiler may choose a different correct copy order, and the staged C# proof leaves that case to existing lowering.

Patch `retained-tuple-outgoing-projection.patch`, SHA-256 `B62E67BD7747201A62DC79AAB3EFC22DDDEA04DCD549390A9B012954149F6477`, passed forward/reverse application and whitespace checks. It touches three files and retains the existing test ID. Managed compiler and regression builds, the actual hosted candidate outputs, and full function/module acceptance remain pending. No managed worker was started for this investigation.
