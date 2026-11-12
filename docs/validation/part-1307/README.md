# Part 1307 — Place frames after untagged tuple clause checks

Use the per-clause frame emitter for tuple heads with distinct variable fields, including wildcards. The untagged path checks the tuple and arity before extracting fields and evaluating guards. The original argument remains available to the fallback branch. The pinned OTP release remains the byte-level reference. No dependencies changed.

- [Focused command](check-entry-tuple-frame.ps1)
- [Identical OTP and C# instructions](probe.txt)
- [Opcode comparison for megaco_messenger:maybe_send_message/3](megaco-messenger.txt)
- [Entry versus later opcode differences in part 1306](test-allocation-incidence.txt)
- [Source-form classification of nonempty-list cases](nonempty-list-forms.txt)

The focused test compares all instructions, then executes both images with matching, wrong-arity, non-tuple and guard-failing arguments. The Megaco example now dispatches without allocating a stack frame. Its tuple tests and operand order still differ from OTP.

The remaining nonempty-list tests are most frequent in functions with three or more clauses and a direct cons pattern in at least one head. The audit found such a function in 525 modules, counting each module once for that category. Categories can overlap.

The full corpus generated all 1,366 release modules with 361 Equal, 1,005 Different and zero source rejections. Seven files changed. The Code size gap decreased in six files and increased by three bytes in xmerl_xsd. In count_occur/1, OTP allocates after the tuple checks but before extracting the fields. This case needs to keep the previous ordering.

- [Corpus](otp-corpus.json)
- [Delta](delta.json)
- [OTP loader](loader.txt)
- [xmerl_xsd regression](xmerl-xsd.txt)
