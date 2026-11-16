# Part 1436 — Dispatch register case arms by tuple tag

The register case emitter selects a tuple shape before dispatching on its first field. It emits one select_val table for distinct atom tags at the same arity. Each tag has a branch that retains source order for its own clauses. Rejection after those clauses goes to the ungrouped case suffix or the original case failure. A single arity uses the existing tuple check. The original value remains in x0 until a branch accepts it.

The fixture interleaves two clauses for one tag with a different tag. The OTP oracle verifies the table, overlap priority, both tag routes and fallbacks, including case_clause. The .NET regression exercises the tag dispatch and fallback. The pinned OTP implementation emits select_val for the SSA value switch in beam_ssa_codegen:cg_switch/3.

- [OTP and C# fixture](probe.txt), [compile_proc instructions](compile-proc.txt), and [focused .NET tests](focused.json)

The full Windows OTP 29.1.1 run generated 1,366 images. There were 382 Equal, including ELDAPv3, and 984 Different. Both runtimes accepted the 984 differing images. The rule changed 312 images and 684 functions. The number of functions with fewer tuple-tag selections than OTP fell from 1,284 across 415 images to 1,055 across 367 images. Whole-image and normalized function equality were unchanged. The aggregate absolute Code size gap grew by 10,913 bytes.

Each of the 984 differing modules has a Code difference. The normalized instruction scan found 45 with at most three mismatched function bodies. In compile_proc/3, both dispatch tables now match by key and branch order. The frame still differs and the C# error branch still projects an unused alias field. The consecutive projection, heap check and tuple-construction pattern occurs in 38 functions across 35 images.

- [Corpus](otp-corpus.json), [delta](summary.txt), [normalized functions](normalized-functions.txt), [tag selection before](tag-selection-before.txt) and [after](tag-selection-after.txt)
- [Near-image ranking](near-images-after.txt), [remaining sections](near-image-chunks.txt), and [unused field projection scan](dead-projections.txt)
