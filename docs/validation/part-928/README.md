# Part 928 guarded fold callbacks

The general BEAM emitter lifts guarded or multi-clause anonymous callbacks and retains the lists:foldl/3 import. Nonexhaustive captured callbacks use a function_clause helper carrying the declared arguments. The test binds the existing pinned lists image explicitly and verifies successful branches, empty input and the first failure frame. The caller uses the general call layout, while OTP emits a tail call in this probe. [Pinned OTP](reference.txt).

The three focused tests passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 893 Different, 105 SourceRejected and 147 Unsupported. Twenty modules moved from Unsupported to Different. Fifty sources first fail on include resolution, including both source-tree headers and generated ASN.1 headers. [Corpus report](otp-corpus.json).
