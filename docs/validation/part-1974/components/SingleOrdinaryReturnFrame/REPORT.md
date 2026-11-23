# A single ordinary call with a continuation frame and no Y values

The frozen Part 1971 compiler is 31b7ffed463bde46a24a72d8b6fb23138102805114fb1acd78d5c43791877c88 and the source overlay is 989dba3d8ff17b774381dfde72396806559a30026c5740e2398c3f2c59527210. The Windows OTP 29.1.1 source commit is ad05823719d77c8faee87348ea39513d4e2f99c5. The immutable Part 1971 result is 509 Equal, 857 Different, 0 Unsupported and 0 SourceRejected.

| Paired scope | Functions | Modules |
| --- | ---: | ---: |
| Original one ordinary call and no physical Y use after the call | 241 | 137 |
| Same pinned ordinary call target with frame width zero | 232 | 131 |
| Same pinned ordinary call target with positive pinned frame | 3 | 3 |
| Different ordinary call target | 6 | 5 |
| Any make_fun3 in the 232-frame subset | 112 | 64 |
| Linear move/data/closure prefix and admitted tail or return suffix | 31 | 25 |
| Structurally close zero-slot controls from whole-file Equal images | 11 | 11 |

Module counts can overlap across rows. Candidate and reference disassemblies are retained independently. cohort.terms and pairs.terms contain frequencies, source top-level forms and actual ordinary-call targets. linear-scope.terms is the bounded positive manifest. It provides structural candidates rather than a measured production hit count. The existing leaf planner may reject an individual function on allocation placement, register initialization or interference. exact-controls.terms identifies already exact functions with a returning call and a zero-slot frame, including erl_init:restart/0 and xmerl_xs:value_of/1.

## Pinned rule and witnesses

beam_ssa_pre_codegen.erl 1433-1473 places frames by dominance. Its need_frame_1 at 1566-1597 keeps a frame for a nonterminal static call or an actual trapping form. frame_size_1 at 2756-2772 counts Y registers from definitions in the frame region, so that frame may have width zero. find_yregs at 1938-2042 defeats retention for values dead before the X-clobbering call. The current C# frame often stores scratch to prepare call operands before any value must survive an ordinary call.

| Release function | Frozen P1971 | Pinned comparison |
| --- | --- | --- |
| mod_dir:encode_html_entity/1 | allocate_heap 1, make_fun3 into Y0, load it into X1, local non-tail call, external tail using width 1 | allocate_heap 0, make_fun3 directly into X1, same non-tail local and external tail at width 0 |
| beam_asm:flatten_exports/1 | allocate_heap 1; save argument in Y0, construct closed fun in X0, reload X1 | allocate_heap 0, create closed fun in X1, then swap before lists:map/2 |
| systools_make:create_mandatory_path/3 | allocate_heap 2, lists:map/2, local tail at width 2 | zero-width call frame; separate closure capture and literal propagation differences |
| ct_logs:date2str/1 | frame 6 and no surviving Y values, after checked argument projections | frame 0 and six pre-call projections in X; the initial implementation defers checked entries |

The representative precodegen files were produced with the pinned compiler from released abstract forms. The exact release images are authoritative for helper IDs and raw layout. In mod_dir and beam_asm the pinned listings attach frame_size 0 and empty yregs. Their call arguments, closure and returned value use X registers.

## Source-only implementation

production.patch changes only the verified Part 1971 LeafFrameRegisters preimage. The candidate uses the existing interference graph, register allocator and initialized-heap-root proof for a linear frame containing one static local or external ordinary call. It derives use of X0..arity-1 from that call, skips the callee address when building local CFG edges and treats the incoming X dataflow as clobbered at the call. A subsequent use may depend on the X0 result. A live Y operand after the call, typed Y operand, checked branch, dynamic or second ordinary call, and unfamiliar opcode remain on their existing paths. Existing capture operand accounting applies to make_fun3. The existing root and effect positions are retained. Allocation and deallocation or framed-tail instructions keep their original kinds and an explicit width-zero continuation frame.

The structural fixture proves five red positives on frozen Part 1971. All five have one saved Y slot before the call and a width-one cleanup, compared with width zero on OTP. A retained control consumes Y0 after the call on both. The suite also captures function_clause at checked line 28 and the closure badarith stack at lines 12 and 13. All nine native scenarios pass on both pinned and frozen Part 1971 images, including captured closures and an ordinary call followed by explicit deallocation and return. fixture-structure.terms, fixture.pinned.terms and fixture.part1971.terms contain the evidence.

The production and test patches strictly apply, and Roslyn syntax checks pass. A patched compiler assembly, SDK red/green, local corpus gain, native execution of patched images and equal-control retention are pending. No build was launched. Frame width alone does not establish byte equality, especially for functions with independent swaps, fun identities or captures.
