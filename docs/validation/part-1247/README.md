# Part 1247 — match httpd_socket catch result dispatch

Keep the protected call arguments and catch token in a three-slot frame. Clear the arguments after loading the call registers. Dispatch the caught value in X registers, with a common path for unchanged results and a shared heap check for wrapped values.

The execution probe verifies ok, an existing error tuple, special and generic exit payloads, a throw and other scalar and tuple results. The release close/2 instructions match OTP after label normalization. The standalone release-profile rebuild of the complete httpd_socket image has the same SHA-256 as OTP.

The full corpus generated 1,366 images with 358 Equal, 1,008 Different and zero rejected sources. The only image that changed was httpd_socket, now Equal. OTP accepted all differing image pairs.

- [Source probe](catch_result_dispatch_probe.erl)
- [Execution and dispatch check](probe.txt)
- [Normalized release function](close.txt)
- [Whole-image SHA-256](httpd-socket-hashes.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
