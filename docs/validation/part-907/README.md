# Part907 Wx object command wrappers

The BEAM emitter recognizes the single-object queue wrapper with an optional tail receive. It emits the tagged object check before allocation, keeps one stack slot for the object, emits the class test and dynamic parent_class call, reads wx_env with a BIF and trims the frame before queue_cmd. The unknown-environment branch preserves the original frame and line information.

Four pinned release images matched byte for byte in the targeted run. The wxGridCellRenderer methods still use the general emitter because they check several records and additional arguments.

The separate 241-image wx ebin scan found 24 more modules whose Code already matched. All 24 became byte-identical after using the existing generated wx profile for their build options and include paths. The release regression covers all 28 new matches.

All three focused compiler checks passed with the expanded release regression. [Focused report](focused.json).

The full Windows OTP 29.1.1 corpus reported 169 Equal, 786 Different, 105 SourceRejected and 306 Unsupported. All 28 status changes were Different to Equal. [Corpus report](otp-corpus.json).
