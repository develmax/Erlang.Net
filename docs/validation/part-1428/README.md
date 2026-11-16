# Part 1428 — Preserve throws across handler resume edges

The throw analysis stops at the caller's first landing pad. It retains throw/1 when the handler can resume the original exception, even when another handler outside it catches the reason. It also retains throw/1 for a handler that switches between multiple tuple arities. The OTP handler analysis follows the default resume edge of that switch.

The OTP instruction and execution oracle now agrees on the two controls that previously emitted raise/3 in C#. The focused .NET tests cover the tuple handler and the existing stack observer. The initial .NET attempt to invoke the nested handler was rejected by frame validation. Both the previous and current C# compiler use increasing handler slots in the nested fixture, whereas OTP uses decreasing slots. Both generated images execute the fixture on OTP.

The baseline first-instruction census ranks list test before allocation and allocation operands in variable-head sequences at 536 modules each. Within the list group with debug forms, an unguarded variable-head cons clause ending in a direct call appears in 192 functions across 130 modules. Groups can overlap.

- [OTP oracle](check-handler-resume.ps1), [current](probe.txt) and [previous](previous-probe.txt) results
- [Tuple switch SSA](uri-handler-ssa.txt), [nested resume SSA](nested-handler-ssa.txt), [nested slots](nested-frame.txt)
- [Focused tests](focused.json), [initial runtime failure](initial-focused.json), [layout](layout.txt)
- [Frame ranking](baseline-frame-shapes.txt), [list branch ranking](baseline-list-branches.txt), [release examples](list-entry-examples.txt)

All 1,366 Windows OTP 29.1.1 images generate successfully. There are 382 Equal and 984 Different, including byte equality for ELDAPv3 from C#-generated Erlang. OTP prepares all 984 differing images for loading.

Four images change. In 13 of 15 changed functions the throw and raise call counts now match OTP. One function keeps the same count distance and parse_hier/2 moves away by retaining throw where OTP uses raise. Across the two parts the number of changed functions with excess raises drops from 53 to 39. The aggregate Code size gap shrinks by 103 bytes. No whole-image or normalized function status changes.

- [Corpus](otp-corpus.json), [loader and Code sizes](loader-and-code.txt), [throw audit](throw-delta.txt), and [cumulative audit](cumulative-throw-delta.txt)
