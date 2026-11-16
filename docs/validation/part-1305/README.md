# Part 1305 — Compare repeated entry arguments without a frame

A repeated variable in two argument positions compiles to an exact equality test on X registers when both clauses return constants and the second clause always matches. The compiler uses the Erlang pattern semantics directly and adds no dependency.

The focused probe compares instructions with OTP 29.1.1 and executes both generated images. It exercises structurally equal tuples, different atoms and an integer compared with a float.

In asn1ct:compare_defs2/2 the instruction sequence now matches OTP after accounting for the module-wide label offset. This removes the stack allocation, two argument moves and two deallocations.

- [Focused command](check-repeated-argument.ps1)
- [Focused output](probe.txt)
- [Release function comparison](asn1ct-compare-defs.txt)
- [Tests preceding stack allocation in the previous corpus](test-allocation-incidence.txt)

The pinned OTP compiler places frames on basic blocks using dominance in beam_ssa_pre_codegen.erl. The current C# path already splits frames for some list clauses in BeamModuleCompiler.EntryClauseFrames.cs. The audit identifies tuple tests in 665 modules and nonempty-list tests in 658 modules at the first differing opcode of at least one function. These groups overlap.

The first full run used the older source-header directory and could not resolve external documentation in seven modules. All seven generated images in a focused check with the pinned source overlay. The full rerun with that overlay generated all 1,366 images and reported 361 Equal, 1,005 Different and zero source rejections. In each of the five changed modules, the Code chunk moved 13 bytes closer to OTP. OTP accepted all 1,005 differing images. [Focused overlay check](overlay-probe.txt).

- [Verified full corpus](otp-corpus.json)
- [Five changed images](delta.json)
- [OTP loader](loader.txt)
