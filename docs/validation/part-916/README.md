# Part 916 map comprehensions and map generators

The C# BEAM emitter constructs the key/value pair list and calls maps:from_list/1, using the same qualifier loop as list comprehensions. Map generators use the pinned OTP compiler helper protocol with mc_iterator/1, three-element cursor entries, mc_refill/1 and the none sentinel. A relaxed pattern mismatch skips the entry, and a strict mismatch reports the key/value pair. The invalid initial cursor reports the original generator input.

The pinned OTP reference records the result map, duplicate-key order, filtering, a relaxed pattern mismatch, a strict pattern mismatch, an invalid generator and a 513-entry map. [Reference run and disassembly](reference.txt).

This path covers map and list generators with filters, including nested supported qualifiers. The general emitter retains its existing list accumulator and reverse, while OTP may construct pairs in a lifted helper and select maps:from_keys/2 for constant values. Zip and binary generators remain separate.

All three selected compiler checks passed. The full runtime suite and canonical readiness regeneration were outside this run. [Focused report](focused.json).

The full Windows OTP 29.1.1 run reported 221 Equal, 769 Different, 105 SourceRejected and 271 Unsupported over 1,366 files. Fifteen modules moved from Unsupported to Different. Eight were among the original 16 MapComprehension failures and seven were previously rejected by the emission planner. No other status changed. [Corpus report](otp-corpus.json).
