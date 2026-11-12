# Part 923 targeted inlining through record actions

The inliner now traverses explicit record fields and the wildcard initializer, and rewrites the record receiver and fields in updates. It preserves record definitions and field indices. This allows substitution through the record expressions used by generated parser actions.

The pinned OTP probe reduces a stack into a record, fills two fields from a wildcard initializer and then updates the first field. Both targeted private definitions disappear in the reference disassembly. [Reference result and instructions](reference.txt).

The existing restrictions on targeted function heads and unsupported control-flow templates still apply.

All three focused compiler tests passed, including the record action probe through the managed BEAM loader and runtime. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 827 Different, 105 SourceRejected and 213 Unsupported across 1,366 files. snmpc_mib_gram moved from Unsupported to Different. No status regressed. Four Megaco text parsers passed targeted inlining and now fail in BeamDocumentationWriter on included forms. The same writer gate is the first failure for twelve modules. [Corpus report](otp-corpus.json).
