# Part 1052 diameter transport image parity

For a local tail call that forwards its incoming prefix and takes a nested remote list call as its final argument, the emitter preserves only the forwarded prefix. It combines the stack and cons allocation and leaves the nested call result in x0 until arranging the outgoing arguments.

The generated diameter_transport.beam now equals the 6280-byte Windows OTP 29.1.1 release image. Its previous status was Different. A permanent release fixture captures both the installed source and reference image. Reference BEAM SHA-256 is 9a5d56509b320e644f105b8c84c0b80a4bb1e926b642d779a1f380d7cba11835. Source SHA-256 is 2912f3f09ce8da7eea8bb990f958df983364dcbcbbc789edfd8d69b6447ca41c5f.

The full corpus admitted and generated 1363 of 1366 files. It has 343 Equal, 1020 Different and 3 SourceRejected. Diameter_transport is the only status change. ELDAPv3 remains Equal. All 1020 differing images prepared successfully in OTP.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Permanent fixture check](fixture.json)
- [Loader results](loader.txt)
