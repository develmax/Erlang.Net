# Disjoint tagged-tuple and atom entry partitions

This isolated C# draft uses the Part 1966 retake compiler `3b4b37280a2e258571aa169b9560300206739448552659e2412aeeb32af76dd1` and the Windows OTP 29.1.1 baseline `ad05823719d77c8faee87348ea39513d4e2f99c5`. The reference and candidate timer disassemblies are bound by the [retake receipt](../saved-tuples-and-trimmed-call-arguments-1966-bundle/representative/receipt.json). The representative module has residual `Code` and `Attr` differences.

| Entry instruction | OTP | Part 1966 retake |
|---|---|---|
| Type partition | `is_tuple X0` fails to the atom clause at label 62 | Absent |
| Tagged shape test | Tuple-typed X0 with tag `apply_after`, arity 2; failure goes to fallback at label 63 | Untyped X0; failure goes through the atom clause at label 62 |
| Atom decision | Exact comparison with `apply_after_infinity`, failure to label 63 | Same |
| Function info | Entry label 60, function header source line 194 | Same |

In pinned `beam_core_to_ssa.erl:1381–1397`, the selector type order places tuples ahead of atoms. The comment and code around `select_cg` at lines 2825–2834 distinguish failure of the type from failure of the value within that type. Tuple selection at line 2878 emits the type test before testing the tuple values. `beam_ssa_opt.erl:930–1019` folds the tuple shape and tag logic, and `beam_ssa_type.erl:674` and `beam_ssa_codegen.erl:1756–1767` produce the typed tagged test used by the pinned image.

The draft uses the existing companion-entry pass's instruction reader, label table and typed-register serializer. It validates three adjacent source clauses with distinct tuple and atom selectors and an unconditional fallback. The selector must be a plain input X register and the other entry bindings must be unconditionally independent. It verifies the emitted atom continuation and direct fallback before rewriting only the leading tagged test. The new `is_tuple` reuses the atom label, and the typed `is_tagged_tuple` reuses the atom comparison's fallback label. Guarded, overlapping and no-fallback shapes retain their prior dispatch in this draft. Clause bodies and their allocation and projection instructions stay outside the patch.

A read-only census of the 17 frozen retake modules found two raw prologues with the same untyped tagged test and atom continuation. Only `megaco_monitor:cancel_apply_after/1` passes the source proof and shows the desired pinned partition. `gen_server:handle_action/2` fails both checks. None of the seven byte-equal controls has the raw shape. The generic tuple type exists in the retake for `handle_info/2`; the target makes a new earlier use, which remains ordered by the existing `OrderTypeOperandsByFirstUse` pass. [Scope census](native/entry-census-final.terms), [tuple-type observations](native/timer-tuple-types.terms).

The permanent test checks the two failure labels and tuple-only type facts at arities one and two, independent context preservation, wrong tuple tag or arity, the atom branch, a guard failure, fallthrough to a later overlapping tuple clause, and `function_clause` at source line 34. The pinned compiler and frozen C# retake compiled the probe. Both BEAMs loaded and returned the same results and stack location under ERTS. The pinned positive disassembly has the intended partition and the retake lacks it. Candidate SDK build, red/green test execution, layout, native comparison and corpus verification remain pending. [Native behavior](native/behavior-final.log), [pinned disassembly](native/pinned.terms), [retake disassembly](native/baseline.terms).

Both patches replayed into exact isolated preimages with `--whitespace=error-all`; all postimage SHA-256 hashes matched. [Production patch](production.patch), [tests](tests.patch), [manifest](patch-manifest.json), [evidence hashes](receipt-hashes.json).
