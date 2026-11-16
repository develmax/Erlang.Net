# Part 1051 rewrite tuple arguments in X registers

Eligible tail-call clauses can match a pair and rewrite it as a tagged tuple containing its first field and a singleton list of its second field. The shape tests run before any input register is overwritten. Other arguments are forwarded unchanged. The list and tuple share one heap test.

The generated diameter_transport:start/3 decoded instructions match the release exactly. Both the tuple rewrite and the fallback execute in OTP. The generated diameter_transport image is 6296 bytes, compared with 6340 before and 6280 in the release. The only differing decoded function body is select/3. Code and Attr still differ at file level.

The full corpus admitted and generated 1363 of 1366 modules. All per-file statuses stayed the same at 342 Equal, 1021 Different and 3 SourceRejected. ELDAPv3 remains Equal. All 1021 differing images prepared successfully in OTP. The frame analysis found one fewer paired function with an allocation only in generated code.

- [Full corpus](otp-corpus.json)
- [Focused checks](focused.json)
- [Loader results](loader.txt)
- [Frame counts](frames.txt)
- [OTP execution and instruction check](result.txt)
- [Remaining select/3 differences](remaining.txt)
