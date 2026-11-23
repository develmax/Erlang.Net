# Source-proven tuple result types

The frozen package adds a local atom-or-tuple successful result summary
and repairs the lost constrained keyfind case. Both reuse existing
source and emission provenance. Pinned beam_ssa_type supplies return
joins and beam_ssa_codegen supplies the bare tuple/arity split for a
same-tag pattern. The comparison uses actual generated BEAMs from the
candidate compiler. The previous full corpus has 409 Equal and retains
all 1,366 input profiles.

The selection recompiles 80 release images in 334.88 seconds with
8 Equal, 72 Different, 0 Unsupported and 0 SourceRejected. No file
status changes. All 52 changed images prepare in ERTS. Four of 83
comparable bodies gain normalized equality and none lose it. Three
gains are from the linked Part 1863 frame and receive changes.
erl_syntax_lib:annotate_bindings/1 restores the single Part 1862
normalized loss. dbg_wx_win gains raw Type chunk equality. No raw
section loses equality.

The 65 selected adjacent source/call sites produce 60 operand matches
in 41 modules and leave five conservative shapes untyped. Both
already Equal bare controls in snmp_app_sup stay bare. ELDAPv3 and
seven other equal file controls retain their byte status. Literal
wildcard, unresolved local, no_ssa_opt, dynamic/remote and mixed return
controls remain covered. All seventeen focused tests pass from the
9,814-case catalog. Both native fixture pairs agree on values, errors
and selected raw register types.

The direct assignment cases in edoc_layout_chunks and group_history
are handled by a separate source emitter. They have their own
proof and validation in the next isolated candidate. The complete
managed regression remains pending.

[Source identity](source-freeze.json) · [Focused report](focused.json)
· [Release classification](pilot.json) · [Instruction delta](pilot-summary.terms)
· [Exact type operands](scope-types.terms) · [Native observations](native-fixtures/receipt.json).

