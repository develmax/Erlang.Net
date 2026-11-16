# Part 1239 — preserve abstract forms for functions defined in headers

Emit abstract function forms from included headers using the preprocessor's source coordinates. When returning from an include, follow OTP's single-character consumption after the directive terminator. With CRLF, the return directive still uses the include line and advances one column for the carriage return. With LF, it uses the next line.

Compare the full decoded abstract forms with OTP after normalizing file paths. Four probes cover LF and CRLF, both with and without another function after the include.

The first full run after the line-ending correction exposed CRLF output from the C# ASN.1 generator. Emit LF in both generated files to preserve ELDAPv3 release coordinates. The standalone SHA-256 check and final corpus both confirm ELDAPv3 parity.

The final Release run generated all 1,366 images, with 357 Equal, 1,009 Different and zero rejected. The only changed image is inet_dns. Its restored Dbgi is 39,135 bytes and contains the same number of forms as the release. The decoded forms still differ in 35 positions. OTP accepted all differing image pairs. Thirteen remaining abstract-code failures originate at the included-attribute check.

- [Abstract form comparison](probe.txt)
- [Reference include coordinates](include-line-endings.txt)
- [Probe source](debug_include_probe.erl)
- [Included functions](debug_include_probe.hrl)
- [ELDAPv3 byte comparison](eldap-parity.txt)
- [Full corpus](otp-corpus.json)
- [Chunk delta](debug-delta.txt)
- [Remaining inet_dns abstract differences](inet-dns-debug-difference.txt)
- [Strict diagnostics](strict.json)
- [OTP loader](loader.txt)
