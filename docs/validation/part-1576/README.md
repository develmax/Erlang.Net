# Part 1576 — Reuse a matched case literal in tail arguments

A terminal literal branch already holds its matched value in X0. When its tail call passes the same value as argument zero, the call planner keeps X0 and removes the redundant constant load. Calls with a different argument retain their load. The planner uses exact term equality and applies the rule only to branches with a proven match. The reference is OTP 29.1.1 beam_jump.erl.

The whole-module test includes beam_matched_tail_argument.erl and a deterministic OTP image. Both compilers produced the same 704-byte file. The OTP VM agreed on three matching branches and the unmatched case. The longer terminal case fixture also matches OTP instructions after normalizing file names. Four focused compiler tests passed. [Regression instructions](matched-tail-argument.txt) · [Outcomes](outcomes.txt) · [Terminal case](terminal-case.txt) · [Tests](focused.txt).

The full comparison accepted and generated 1,366 files, with 390 Equal and 976 Different. ELDAPv3 matched from the C# ASN.1 generator. Seven images changed. Two functions gained normalized instruction matches. OTP accepted all 976 differing images for loading. The full runtime suite and canonical readiness were outside this run. [Corpus](otp-corpus.json) · [Functions](normalized-functions.txt) · [Loader](loader-and-code.txt).
