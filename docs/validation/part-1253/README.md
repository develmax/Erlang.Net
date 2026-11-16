# Part 1253 — Emit direct local tails in atom dispatch

Use separate terminal arms when a prepared atom case has local calls, literals, or already bound return variables. Save surviving entry arguments in reverse order, emit local `call_last` from each tail arm, and reuse the selector register when an arm returns its own atom key. The previous identity case fold remains available for local calls with simple operands.

The instructions of `inet:getservbyport/2` and `net_adm:longshort/1` match Windows OTP 29.1.1 after label normalization. The execution probe checks three local tail calls, argument order, literal and variable returns, and the unmatched selector reason.

The full run generated 1,366 images with 359 Equal, 1,007 Different and zero rejected sources. All 32 changed images moved closer to the release Code section length, by 684 bytes in aggregate. The same atom selector ranking now finds 875 candidate branches across 217 modules, down from 926 across 224. OTP accepted all differing pairs.

- [Probe source](terminal_atom_tail_probe.erl)
- [Probe command](check-terminal-atom-tails.ps1)
- [Execution result](probe.txt)
- [Identity fold regression probe](identity-probe.txt)
- [Updated tail branch ranking summary](atom-tail-ranking-summary.txt)
- [Normalized function comparison](functions.txt)
- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [OTP loader](loader.txt)
