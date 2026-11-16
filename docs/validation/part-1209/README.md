# Part 1209 — forward the computed module into dynamic apply

Keep the computed module in X0 until the independent move scheduler places it in the apply module register. The fast path requires that the function name and arguments are bound registers or constants.

In error_handler:breakpoint/3 this matches the reference three-slot frame and removes the duplicate line instruction. The Y register numbering and function labels still differ. Three first frame sizes moved closer in the corpus. Code size moved closer in three modules and farther in four.

The full run generated all 1,366 images, with 357 Equal and 1,009 Different. ELDAPv3 stayed Equal. OTP accepted all differing pairs. Three focused tests and the computed module execution probe passed.

- [Full corpus](otp-corpus.json)
- [Corpus delta](delta.json)
- [Breakpoint disassembly](breakpoint.txt)
- [Allocation delta](frame.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
- [Probe](probe.txt)
- [Opcode ranking](ranking.txt)
