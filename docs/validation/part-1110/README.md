# Part 1110 — share extraction before dispatching on tuple tags

The register path for terminal tagged cases now accepts several distinct tags of the same tuple arity. It validates the shape once, extracts the tag and the union of fields used by the branches, then selects the branch with select_val. Each branch uses the existing move scheduler to prepare the tail call. Duplicate tags, patterns requiring extra tests and branches with different arities stay on the general path.

The OTP 29.1.1 probe matched all instructions for same_arity/1. It exercised both branch argument orders, unknown tags, incorrect tuple arity and non-tuple inputs.

- [Probe source](beam_entry_tag_dispatch_probe.erl)
- [Execution and disassembly assertions](run.eval)
- [Instruction comparison](disassembly.txt)

A four-branch execution probe also verified that branches can read different subsets of the shared fields, including a branch with only constant arguments.

- [Disjoint field probe](beam_entry_disjoint_probe.erl)
- [OTP execution outcome](disjoint-outcomes.txt)

The full corpus generated 1,363 images. There were 348 Equal, 1,015 Different and three SourceRejected. No generated release image changed. ELDAPv3 remains Equal. OTP accepted all differing images during loader preparation.

The next candidate is forwarding a nested ordinary call directly into a tail call in the 249-file direct-move group. In substitution/3, OTP uses a zero-slot frame while the generated code saves two arguments used only by the inner call.

- [Focused compiler tests](focused.json)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader preparation](loader.txt)
- [Next release example](substitution.txt)
