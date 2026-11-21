# Terminal atom identity selectors in Part 1973

The frozen Part 1973 uncached run had 509 Equal, 857 Different, zero Unsupported and zero SourceRejected across 1,366 Windows OTP 29.1.1 images. Corpus elapsed time was 1,039.316 seconds. The actual compiler DLL SHA256 was f685472e21eddb969dc244fa620539ff5d145c8d267b0f8e9486a7f30dad2476 and its exact source freeze was 3a834f1bd054df421f7d563178599b2bd4cf868dfd0c475ee1cb810e42e20cfa. The pinned baseline was ad05823719d77c8faee87348ea39513d4e2f99c5.

| First selector difference | Functions | Modules |
| --- | ---: | ---: |
| Pinned select_val versus generated is_eq_exact | 757 | 321 |
| Same complete set along generated failure edges | 533 | 228 |
| Same set, selector register and prefix operands after dropping label identities and type wrappers | 497 | 209 |
| Same set and prefix, with all arm and fallback endpoints resolved and equal | 90 | 39 |
| Partially overlapping equality chains | 219 | 153 |
| Disjoint equality chains | 2 | 2 |
| Complete reference table at another generated position | 3 | 3 |

The 533 complete chains comprise 489 atom and 44 integer selectors. The last three rows and complete chains partition all 757. The relocated subset identifies a possible order dependency. It does not establish a label-only cause. `first-selectors.eval` compares the first opcode mismatch using the same metadata policy as the repository rank evaluator. It follows only bounded direct labels and jumps. It compares simple block endpoints symbolically using register moves, stack movement, literal returns, static tails and failure terms. Unknown endpoints remain unresolved. `strict-shapes.stdout.log` groups the 90 endpoint-proven cases. Large neighboring groups include generated Diameter constant/tail tables and Megaco encoder mappings with distinct return terms.

## Source gate and pinned pipeline

Twenty-six endpoint-proven cases have a single X0 success return for every exact atom and case_end on X0. Whole-release raw abstract census finds exactly 24 eligible single-clause source definitions. They occur across the three Megaco binary transformers, with nine in v1, eight in v2 and seven in v3. The separate leading asn1_NOVALUE clauses of v3 tr_opt_SignalDirection/2 and tr_opt_SignalType/2 exclude those remaining two and provide controls. No qualifying source was found among 509 already Equal images. Abstract code was available in all 1,366 images. `all-control-source-gates.stdout.log` retains every source witness.

Each eligible source is an unconditional function whose terminal case selects its first unaliased argument and has unique unguarded atom keys, each returning the matched atom. `fixture.core` preserves these literal source clauses. Pinned `beam_core_to_ssa.erl` groups alternatives by type and key in `match_con/4` and `select_val_cg/7` and builds a switch. `beam_ssa_codegen.erl` materializes it via `cg_switch/3`. The saved precodegen shows separate literal returns and original synthetic match_fail annotation. At pinned `beam_jump.erl:186`, the switch seeds a register-value fact along each arm. The X0 result move is redundant and the remaining return blocks share. `beam_jump.erl:649` can replace the Boolean pair on a shared label with is_boolean. A nonzero selector fails that specific move-elimination proof because its return destination is X0. The entire restored failure path keeps the case keyword line if distinct from the entry line.

The source-only patch reuses the existing selector writer, label allocation and case failure emission. It admits a single entry clause whose first argument is explicitly selected and whose arms are a non-Boolean-only atom identity table with no guard or fallback. It yields to typed private-entry facts, disabled SSA/module optimization and other existing entry or closure owners. Guarded, singleton, Boolean-only, explicit-default and X1 forms are controls.

Proof model `identity-release-model-source-gated.eval` reconstructs each table from source keys after requiring the actual generated whole body to be a sequence of exclusive exact-test/return/failure-label blocks. It keeps the original failure annotation and operand. The frozen reference instructions are used only in the final comparison. All 24 derived functions match the whole normalized reference function, preserving line terms. The initial overinclusive model's native error is retained separately and exposed two actual multiclause owners. The source-gated run succeeds.

## Checks and next package scope

The independent fixture passes 15 native values/errors for both pinned OTP and the exact frozen C# output. Five error stack top frames agree after path normalization. Native red evidence shows the exact Part 1973 C# compiler emits a sequential equality chain for the positive terminal identity. The pinned comparison includes distinct behaviors for X1, guards, singleton, Boolean, nonidentity mapping and explicit fallback. `same-line.stdout.log` verifies the pinned entry line is enough when the case keyword shares it. The permanent test checks sorted keys, a single shared return and the preserved or suppressed failure line as appropriate, then tests representative values and case_clause.

Both proposed patches pass git apply --check --whitespace=error against the exact P1973 archived file preimages. They are source-only. Compiler green, focused integration, whole-image equality and current-candidate no-loss checks are pending. Start with the permanent fixture, the three transformer releases and existing tested entry selector controls. Use the two retained v3 multiclause functions as within-image non-target controls. Report normalized function recovery separately from full image equality.

Production patch SHA256 c38093b3390f9ad962a1b5074b295d5a42c458baff02cc51ca93ae918c88766d.
Permanent test patch SHA256 ac6a0947ebc2bd0f4dfa195caf117f961c8c821b18579adf11e0b8f23e5f779d.
Frozen function rank SHA256 a9a4a6ecbec0590c77430b8894278a8a93b8b248a5ee1c488ec2403e0ed2eb69.
Uncached Part 1973 corpus SHA256 3f2ec48096667978a52ed5d174c0ca0f18d50a651631c3504a1370f423eabe32.
No shared source or reference checkout was written.
