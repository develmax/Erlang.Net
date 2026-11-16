# Part 949 case branch bindings

Detect bindings common to all case clauses that are read later in the expression sequence. Reserve one shared slot per binding and move each branch result into those slots before continuing. This covers discarded case results in ordinary expression sequences and before tail calls.

The runtime fixture exercises both tuple patterns with opposite field ordering. The managed runtime and pinned OTP return the same values. The fixture still differs at the byte level. OTP extracts tuple fields into X registers and shares the tag dispatch before the tail call. The general emitter uses Y slots and branch-local matching. [Reference instructions and values](reference.txt), [probe](probe.json) and [focused checks](focused.json).

The full Windows corpus has 223 Equal, 1052 Different, 84 Unsupported and 7 SourceRejected. Twelve additional modules generate BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).

The most frequent remaining emission failure is the restricted EmitBody fallback at 28 modules. Inspect the patterned init clauses, starting with logger_sup:init/1.
