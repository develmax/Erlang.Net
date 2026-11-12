# Part 947 automatic Core inlining

Read the automatic inlining compile options and reuse source call substitution for supported single-clause helpers. OTP 29.1.1 runs cerl_inline for the inline atom unless no_inline is present. This implementation measures source expression nodes and limits work per clause. OTP measures its Core transformation and applies additional candidate and budget rules.

The deterministic fixture matches OTP byte for byte. All three focused checks passed. [Probe](probe.json) and [focused checks](focused.json).

The full Windows OTP 29.1.1 corpus has 223 Equal, 1038 Different, 10 SourceRejected and 95 Unsupported. Five additional modules generate BEAM. ELDAPv3 remains Equal. [Corpus](otp-corpus.json) and [changed statuses](changed-status.txt).
