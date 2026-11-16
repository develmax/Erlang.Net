# Part 1231 — emit immediate operands for proven receive timeouts

For a receive with no message clauses and a literal result, emit an immediate positive timeout and a zero-slot frame when the timeout is literal or all known private callers agree. The analysis follows direct parameter forwarding, declines recursive cycles and rejects escaping references and exported entry points. Other receive shapes retain the existing lowering.

The execution probe verifies immediate timeouts for a literal and a private forwarding chain. Exported functions, escaping references and mixed-value calls retain register operands. In the release corpus, httpd_socket:sleep/1 matches the OTP instructions after label normalization.

The Release build generated all 1,366 images with 357 Equal, 1,009 Different and zero rejected. The Code size gap fell by five bytes each in init, httpd_acceptor, httpd_example and httpd_socket. OTP accepted all 1,009 differing image pairs. An initial run supplied an older source overlay and missed seven Markdown files. The final run used the same overlay as part 1229.

- [Probe source](literal_timeout_probe.erl)
- [Execution probe](probe.txt)
- [Release function disassembly](httpd-socket-sleep.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
