# Part 918 numeric and map lookup guards

The general BEAM emitter reuses its GC unary guard BIF path for abs/1, bit_size/1 and trunc/1. The map key BIF path also handles map_get/2. Each operation retains the enclosing guard failure label. Numeric guard values do not use the existing size-result bounds.

The pinned OTP compiler emitted gc_bif for the three unary operations and bif2 for map_get/2. The reference result covers floats, negative truncation, a three-bit binary segment, missing map keys and operands of the wrong type. [Pinned OTP run](reference.txt).

All three focused compiler tests passed, including the twelve reference guard outcomes. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 801 Different, 105 SourceRejected and 239 Unsupported across 1,366 files. Nine modules moved from Unsupported to Different, with no other status changes. The previous first failures were abs in three, bit_size in four, map_get in one and trunc in one. [Corpus report](otp-corpus.json).

Named recursive funs are the next expression group. The pinned probe shows the lifted self call and reconstruction of a returned self value. [Named fun reference](next-named-fun.txt).
