# Part 1196 — distinguish nested include paths in debug forms

The preprocessor retains the requested include_lib path on abstract-code file transitions, including returns to a parent header. The metadata writer checks that key before the existing basename mapping. The Inets profile records the release path for inets/include/httpd.hrl while the local header keeps its relative name.

The four httpd.hrl transitions in httpd_socket match the Windows OTP 29.1.1 release. The decoded release also records bootstrap paths for file.hrl and inet.hrl, which still differ.

The full corpus generated 1,363 of 1,366 images. Results were 357 Equal, 1,006 Different and 3 SourceRejected. ELDAPv3 remained Equal. Bytes changed in 29 images. All debug chunk comparison statuses stayed the same. OTP prepared all differing images for loading and three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Debug chunk delta](debug-delta.json)
- [httpd_socket path comparison](httpd-paths.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
