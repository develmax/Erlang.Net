# Part 1230 — trace receive timeout operands to private callers

Disassembly of the 1,009 differing release pairs identified 14 matching function names and arities in 12 modules where OTP uses an integer wait_timeout operand and the generated function has no integer wait_timeout operand. This filter identifies candidates and does not establish why every operand differs.

For httpd_socket:sleep/1, the pinned source passes 1000 through close_sleep/2 to sleep/1. The reference uses that literal with a zero-slot frame. The generated function retains the parameter in a Y register across wait_timeout and allocates one slot. The five xmerl candidates share one generated source template and retain the timeout on a recursive edge.

The next compiler change can propagate agreed literal values through private forwarding calls and retain them on recursive edges that forward the same value. Exported functions and functions with escaping references require separate proof before specializing entry parameters.

- [Corpus-wide timeout candidates](receive-timeouts.txt)
- [httpd_socket reference and generated disassembly](httpd-socket-disassembly.txt)
- [Input corpus from part 1229](../part-1229/otp-corpus.json)
