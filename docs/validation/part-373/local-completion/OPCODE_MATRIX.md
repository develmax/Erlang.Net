# Twenty measured queue opcode gaps

Authority: OTP-29.1.1 / ad05823719d77c8faee87348ea39513d4e2f99c5. Original queue image SHA-256: 759B1FE208F8A0DE292147EB00FFCE98F929263ADCFCD4AABB485E3B3EB4538D.

All twenty names now have operand/link roles, export/control-flow/frame validation and interpreter semantics. Preparation validates all forty exports, including generated module_info/0,1. This is implementation/admission coverage, not a percentage of all BEAM instructions or exhaustive proof of each opcode. No instruction-hit trace was collected; passing module cases alone does not prove every failure/alias/control-flow partition. Synthetic instruction images and the unchanged original queue image are separate evidence.

| Opcode | Implemented semantics | Executed checks and practical limit |
| --- | --- | --- |
| is_nil | Exact Nil predicate and failure branch | Empty/nonempty queue cases and original invalid/empty partitions; no arbitrary-bytecode exhaustiveness claim. |
| is_nonempty_list | Cons predicate, including improper cell | Queue cases plus synthetic get_list guard with improper cell. |
| is_list | Cons or Nil, without walking tail | All38 queue partitions include raw/improper list shapes; unary native is_list binding is distinct. |
| is_tuple | Tuple predicate | Original queue representation and invalid raw shapes. |
| test_arity | Exact tuple arity | Queue representation/invalid tuple partitions; defensive validation is separate from a full OTP bytecode type verifier. |
| get_list | Snapshot head and tail before writes | Two synthetic source/destination aliases with tuple head and improper tail, plus actual queue execution. |
| get_hd | Read Cons head | Original queue end operations; no dedicated exhaustive register-alias fixture. |
| get_tl | Read arbitrary Cons tail | Original queue end/improper operations; no dedicated exhaustive register-alias fixture. |
| gc_bif1 | Live roots, unary primitive and body/guard failure distinction | Actual length paths; synthetic proper/improper length preserves failure-edge Y destination. Existing length progress/cancellation checks remain in full regression. |
| bif1 | Unary primitive and failure edge | Actual native is_list path. Additional unary BIF names remain deliberately unsupported until separately admitted. |
| jump | Unconditional in-function transfer, no fallthrough | Original full queue image/cases; no standalone jump failure-zero fixture. |
| trim | Drop low Y slots, preserve survivors and CP, including zero slots | Synthetic normal call -> trim(2,0) -> call_last -> caller result wrapper; actual queue frames. |
| swap | Snapshot both registers, then exchange | Present in unchanged image with passing full queue cases; same-register/X-Y/Y-Y partitions are source-reviewed but not all independently fixture-tested. |
| is_function2 | Fun with exact arity; invalid operands fail predicate | Valid callbacks and invalid callback partitions in original queue packet; arbitrary function identity/arity universe is not exhausted. |
| is_atom | Atom predicate | Original callback/control-flow image and cases; no dedicated standalone type-rank fixture. |
| select_val | Exact homogeneous atom/integer/float choice transfer | Queue true/false/unexpected callback cases; synthetic nil and mixed-class tables rejected. Arbitrary table ordering/size is not exhaustively tested. |
| case_end | error:{case_clause,Value}, terminal | Original unexpected callback-result errors, including full source-stack comparison. |
| is_ge | Arithmetic whole-term ordering | Actual split/index cases; mixed numeric/nested map ordering has separate retained runtime tests, not a complete opcode-specific fixture matrix. |
| is_lt | Arithmetic whole-term ordering | Actual split/index cases; same scope limit as is_ge. |
| call_last | Deallocate matching trimmed frame, restore CP, tail local dispatch | Dedicated zero-slot/tail-continuation fixture and original queue helper/error paths. |

Additional reached shared requirements: native div, explicit selected registry dependencies, dynamic FUN calls, source Line chunk locations, helper failure capture before unwind, true external tail frames, pending-CP coalescing and source-owned cancellation cleanup. Existing C# libraries are preserved; native primitives are C# runtime services. The source/AST worker path used by comparison fixtures is not mislabeled as BEAM execution: the queue library itself is the unchanged BEAM image.

Original comparison counts are reported as strict raw equality, source-bound function/core identities and source-bound saved native entry state. Saved-state arguments may differ between VMs; every original suffix/accumulator and every other field is checked, and raw differences remain preserved. This is not raw-byte equality or proof that the entire OTP runtime is complete.
