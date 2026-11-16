# Part 1342 — Shared mixed cons prefix

The entry planner emits one cons test and extraction for a prefix of list clauses. It retains the original input for non-list and failed-guard fallbacks. Consecutive literal heads use select_val and follow OTP branch order. A nil identity clause folds into a following identity fallback. Labels are assigned in emitted order and call locations account for reordered branches.

The focused fixture matches two decoded instruction sequences after label normalization. It also exercises a literal group at function entry, duplicate literal branches, distinct nil and general fallback results, repeated tuple variables and tuple literal fields. The computed head, repeated binding and nested cons regressions passed.

The final Windows OTP 29.1.1 corpus generated all 1,366 files with 364 Equal, 1,002 Different and zero SourceRejected. Twenty-four images changed and narrowed their Code size gaps by 711 bytes in total. ct_logs:escape_chars/1 and test_server_ctrl:esc_chars_in_list/1 gained instruction matches after label normalization. No prior matches were lost. OTP accepted all differing images. ELDAPv3 and error_handler remain Equal.

The call-shape audit reuses the part 1340 abstract-form scanner over the part 1341 differing image set. It counts source candidates, including functions already matched by the current planner.

- [Focused check](check-mixed-cons-prefix.ps1)
- [Focused output](probe.txt)
- [Computed head regression](computed-regression.txt)
- [Repeated binding regression](repeated-regression.txt)
- [Nested cons regression](nested-regression.txt)
- [Escaping instructions](ct-logs.txt)
- [Corpus report](otp-corpus.json)
- [Changed images](changed-images.txt)
- [Code size differences](code-delta.json)
- [Instruction matches](normalized-functions.txt)
- [OTP loader](loader.txt)
- [Call-shape audit](list-call-audit.txt)
