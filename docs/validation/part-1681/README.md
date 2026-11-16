# Part 1681

The comprehension emitter recognizes boolean results from lists:member/2 and lists:keymember/3, including imported calls. It uses the existing move scheduler for literal arguments and the saved head when the first argument is the head and the remaining arguments are constants. The target result types follow beam_call_types in OTP 29.1.1.

The deterministic fixture matches OTP byte for byte. The OTP VM tested both filters, input order, empty input and the bad_generator error. Three focused compiler checks passed. [Fixture](beam_membership_filter.erl) · [SHA-256](fixture-sha256.txt) · [Disassembly](disassembly.txt) · [VM probe](outcomes.txt) · [Tests](focused.txt).

All 1,366 release sources were accepted and compiled, with 398 Equal and 968 Different. ELDAPv3 remained equal. Only sasl_report_file_h changed. Its list-comprehension helper gained an instruction match and its Code size moved 27 bytes closer to OTP. The OTP loader accepted every differing image. [Corpus](otp-corpus.json) · [Release function](sasl-report-filter.txt) · [Loader and code sizes](loader-and-code.txt).

The scan of the preceding 968 differing images found 437 modules where at least one common function first diverges on the normalized failure label of is_nonempty_list and both streams continue with get_list. Labels are normalized by placement within each function. This count can include later changes in block layout and is evidence for investigating the destination blocks. [Rank](rank.txt).

The full runtime suite and readiness regeneration were outside this run.
