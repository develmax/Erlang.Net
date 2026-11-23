# Forward an incoming argument through an uncaptured local closure

Oracle OTP-29.1.1 at ad05823719d77c8faee87348ea39513d4e2f99c5. The disassembled baseline is the actual final candidate from the Part1981 all-final-label-paths.tsv and the installed Windows release image. The source snapshot for the proposal is source-1982-combined, limited to the two entries recorded in the private patch preimage.

## Pinned lowering

- lib/common_test/src/ct_config_xml.erl:102–103 defines a one-clause arity-one wrapper with lists:map(fun transform_entity/1, EntityList).
- lib/compiler/src/beam_ssa_pre_codegen.erl:2959–2997 and 3060–3084 reserve registers against liveness and prune around GC; lib/compiler/src/beam_ssa_codegen.erl:389 classifies the heap need of make_fun and 1812–1817 emits its assigned destination.
- lib/compiler/src/beam_ssa_codegen.erl:2688–2755 generates outgoing moves and breaks the two-register cycle into swap X0/X1.
- The released wrapper emits test_heap for one fun with live count 1, make_fun3 into X1 with no captures, swap X0/X1, original call line and call_ext_only. Part1981 emitted allocate_heap with one Y slot and used X0 for the new fun before loading X1 from Y0.

## Scope

The raw opcode scan found 118 functions in 82 modules with the candidate frame, save, empty-capture make_fun3, restore and terminal call. Its mechanically modeled replacement matches 41 complete disassembled bodies in 29 modules. The model normalizes function-local physical labels and the FunT old_uniq displayed by beam_disasm alongside make_fun3. It retains instruction ordering, line operations, exact local/import target, FunT index, operand register, heap roots, capture list, and tail-call form.

Pinned annotated abstract forms narrowed the scan to 20 declared local callback reference bodies in 18 modules with an unconditional original argument and static ordinary target outside erlang. The model matches eight such bodies in seven modules. The controls are in scope-declared.terms. These are comparative body counts from a replacement model. A newly compiled cohort is needed for emitted bytes and chunk/whole-image gains.

## Proposal

ReadEntryCall owns static call and reference recognition. The existing closure allocation and declaration emitters preserve one rooted incoming X0 and track the same declared function identity. The existing terminal-register move ordering emits the swap. The emission admits one-variable unconditional original entry and a terminal call of exactly the shape callee(local declared fun, incoming). It leaves erlang imports to the existing path because trapping register-call restrictions still apply. Dynamic external callback references, anonymous funs, guards, additional evaluation, optional call contexts and SSA-disabled modules stay outside this proposal.

The existing permanent terminal-closure case gains local/external swapped forms, exact operand checks, and a local managed invocation. Its established captured, retained and trapping cases were kept. Private forward and reverse patch checks and git diff --check succeeded. No SDK build or fixture execution was scheduled in this audit.
