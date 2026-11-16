# Part 935 selective receive guards

Extend selective receive guard emission for alternatives, negation, literal list comparisons and list, port and map-key predicates. The matcher evaluates accepted BIFs without entering guest code. The regression covers invalid maps and retained messages. [Pinned disassembly and values](reference.txt).

Three focused checks passed. The deterministic probe still differs in the guard instruction layout and metadata. The full runtime suite and canonical readiness regeneration were outside this run. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 978 Different, 56 SourceRejected and 109 Unsupported. Eleven modules now generate BEAM and test_server reached a later blocker. The supervisor receive guard remains unsupported. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
