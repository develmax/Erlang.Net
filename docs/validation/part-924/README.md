# Part 924 Docs annotations for included forms

The Docs writer now accepts included function and attribute forms. Included module documentation, callbacks and exported functions carry the OTP file and location annotation. Their source_anno metadata still uses the plain line and column. The recorded include path takes precedence when a release profile supplies it. Explicit file directives retain their mapped names.

A deterministic OTP fixture places module documentation, a documented callback, a documented function and an undocumented exported function in a header. The regression compares the complete Docs payload. [Pinned reference term](reference.txt).

All three selected compiler checks passed, including the included-form Docs regression. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 839 Different, 105 SourceRejected and 201 Unsupported across 1,366 files. All twelve modules stopped by the include gate now emit BEAM. No status regressed. [Corpus report](otp-corpus.json).
