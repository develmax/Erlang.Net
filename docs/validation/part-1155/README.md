# Part 1155 — build tail arguments after entry pattern extraction

The baseline audit counted 215 modules with an OTP nonempty-list entry test, tuple construction and a frameless tail call in a function where our compiler allocated a frame. The corresponding list construction group contains 200 modules. The categories overlap and include functions with other control flow.

The nonempty-list and grouped tuple entry emitters now pass their extracted X bindings to the same constructor planner used for variable parameters. The probes verify improper list matching, the empty-list branch and two tuple arities. The generated success branches omit stack allocation. OTP reserves heap space before extraction and reuses the input register for the final field. These differences remain in the probes.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. ELDAPv3 remains Equal. All 35 changed images have smaller Code size gaps. The comparison found 287 functions with instruction changes, with zero matches gained or lost. OTP prepared all differing images for loading. Three focused compiler tests passed.

- [Baseline pattern audit](pattern-audit.txt)
- [Audit program](pattern-audit.eval)
- [Probe disassembly](disassembly.txt)
- [Probe assertions](run.eval)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
