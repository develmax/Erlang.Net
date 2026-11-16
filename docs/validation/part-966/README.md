# Part 966 list comprehensions in targeted inline templates

Evaluate each generator source in its incoming scope and then introduce fresh pattern names. Rewrite filters and outputs with the resulting local bindings. The template accepts list generators with simple scoped patterns and ordinary filters.

The probe checks two generators, a shadowed parameter, a filter and a fallback clause. Its values match OTP and its bytes still differ. All three focused checks passed. [Previous probe](probe-before.json), [current probe](probe.json), [reference values](reference.txt) and [focused checks](focused.json).

The full corpus has 223 Equal, 1130 Different, 6 Unsupported and 7 SourceRejected. Gen_udp_socket now emits BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json).
