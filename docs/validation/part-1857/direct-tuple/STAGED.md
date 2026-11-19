# Direct post-call tuple stores

## Cause and scope

Windows OTP 29.1.1's precodegen selects the stack destination for the ordinary call's tuple result in `mnesia_kernel_sup:init/1`: `y8/Flags = put_tuple one_for_all, 0, x0/_2`. `beam_ssa_codegen:cg_instr(put_tuple, Elements, Dst)` emits the supplied destination directly. The generated image from the independently validated outgoing-argument patch instead writes X0 and immediately copies it to Y8. The next argument move overwrites X0. The released `init/1` and outgoing-pilot versions have this single function-disassembly difference.

An unguarded put-tuple copy coalescence is too broad for byte parity. A read-only census over 1,321 conventional ebin images found 15 pinned windows where the X copy was immediately provably dead by a simple move/call scan. The reference retained them around phi transports, especially in ASN.1-generated code. The final proposal requires an ordinary call, heap check with the lone X0 root, a tuple consuming that root, the stack store and an immediate overwrite of X0 with a different source. It rejects a destination Y slot that is also a tuple input. The shared existing branch liveness rejects any remaining reads and preserves GC live prefixes.

| Read-only scope | Pinned windows | Saved generated windows | Exact destination and operand correspondence |
|---|---:|---:|---:|
| Full profile of 1,366, exact proposed rule | 0 | 365 | 5 |
| Outgoing-patch pilot, 12 images | 0 | 2 | 1 |

The full profile comparisons read supplied Part 1853 generated images only where they exist. The 365 observed windows span 187 functions in 107 modules. Five direct correspondences occur in four modules. The source diagnostics, raw records, broader counterexamples and physical-slot register context are retained. The broader post-call pattern with simple later clobber has 560 saved generated windows and ten exact correspondences, and is outside the staged proof.

## Isolated deliverables

Patch `direct-post-call-tuple-store.patch`, SHA-256 `BDA9A9A1C27A182702B37FC960D2EE712B804F3EC6D7445A857B8A14C9406937`. Forward and reverse `git apply --check --whitespace=error-all` pass. The permanent fixture proves the exact direct destination and unchanged heap check operand. It retains copies for intervening read or live root, aliased tuple operand and ordinary construction without a preceding call.

Scope for a scheduled build includes `mnesia_kernel_sup`, `asn1ct_gen_ber_bin_v2`, `asn1ct_imm`, `edoc_layout`, pinned counterexamples `ct_groups` and `yecc`, and five already byte-equal controls. Native testing should assert the existing `mnesia-execution.eval` input-values scenario across the frozen previous compiler, patched compiler and reference, with exact raw body comparison independent of term behavior. The patch deliberately leaves `worker_spec/3` to a separate change. Its pinned precodegen retains two initial Y slots, uses X0 for the call result and schedules the pure list and nested tuple into distinct X registers after one heap check. The latest saved candidate reserves four Y slots and spills all three constructed values.

No managed process was started during this investigation. The candidate build, regression execution, output and native verification remain to be run. No whole-file or verified function gain is attributed to the staged patch yet.
