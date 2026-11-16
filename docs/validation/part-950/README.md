# Part 950 bodies with an empty list parameter

Extend the general emitter planning probe to single unguarded clauses with an empty list argument. The existing specialized lowering keeps priority. A broader probe caused changes to existing BER output and tried to resolve generated helper labels before they were available. The final scope retains the existing planning rules for other clause patterns.

The runtime probe exercises both a matching empty list and a nonmatching argument. OTP returns the entire success result as a literal after the nil test. The managed emitter generates the call and construction in the body, so the bytes still differ. [Previous probe](probe-before.json), [current probe](probe.json) and [OTP result and instructions](reference.txt).

The full Windows corpus has 223 Equal, 1084 Different, 52 Unsupported and 7 SourceRejected. All 223 prior exact matches remain, including ELDAPv3. Thirty-two additional modules emit BEAM. All three focused checks passed. [Corpus](otp-corpus.json), [changed statuses](changed-status.txt) and [focused checks](focused.json).
