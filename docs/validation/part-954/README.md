# Part 954 comprehension debug forms

Write list, binary and map comprehensions with ordered qualifiers. Preserve strict generator tags and the two zip annotation rules from the pinned erl_parse grammar. Reuse map field writing for ordinary maps, comprehension results and map generators.

All three focused checks passed. The fixture compares five decoded function forms against OTP 29.1.1, including nested list generators, zip groups, strict binary generators, binary results and strict map generators. [Reference forms](reference.txt) and [focused checks](focused.json).

The Windows corpus has 223 Equal, 1101 Different, 35 Unsupported and 7 SourceRejected. Dialyzer_cplt and dialyzer_iplt generate BEAM. Beam_asm proceeds to build_beam_debug_info_1/1. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
