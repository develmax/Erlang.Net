# Part 1184 — allocate entry frames for call results per clause

For entry clauses with literal or unary type tests and a tuple containing one ordinary call result, test the clause before allocating. Save only the arguments read by the tuple after the call. The deterministic fixture matches OTP 29.1.1 byte for byte and exercises frames of one and zero slots.

A broader candidate applied separate allocation to arbitrary clause bodies. It preserved comparison statuses but widened the Code length gap in 447 images and narrowed it in 170. That broad path was removed.

The retained path produced the same images and statuses as Part 1183 on all 1,366 release files. It generated 1,363 images, with 356 Equal, 1,007 Different and 3 SourceRejected. ELDAPv3 remained Equal. Three focused tests passed.

- [Full corpus](otp-corpus.json)
- [Delta](delta.json)
- [Broad candidate delta](broad-candidate-delta.json)
- [Focused tests](focused.json)
- [Probe source](../../../tests/Erlang.Tests/Resources/Beam/entry_clause_frames_probe.erl)
- [Probe assertions](run.eval)
- [Before](before.txt)
- [After](after.txt)
- [OTP](otp.txt)
