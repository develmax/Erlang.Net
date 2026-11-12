# Part 1373 — Give literal clauses their own stack frames

The entry planner checks literals and guards in incoming registers. A branch can build and return a tuple there or allocate its own frame for a sequence, case, conditional, try or computed call. Other branches can return without a frame. Branch bodies reuse dead binding slots using the existing continuation analysis.

For a literal branch that saves a call result, the planner gives that result the lowest Y slot and places surviving inputs above it. An initial call with a discarded result can precede an ordinary call chain. The scheduler trims slots once their last use has passed.

The focused oracle checks selected and unmatched literals, a guarded clause, a catch clause and call order. Dispatch and recycle match OTP after label normalization. Three functions reduce the peak frame to OTP's size. The protected branch retains a six-slot frame where OTP uses one. Ten earlier probes, two selected compiler tests and the direct whole-module test passed.

The Windows OTP 29.1.1 corpus generated all 1,366 images with 369 Equal, 997 Different and zero SourceRejected. ELDAPv3 remains Equal. OTP accepted all 997 differing images for loading. Of 490 changed images, 389 narrowed the Code size gap and 94 widened it. The net reduction was 88,521 bytes. The normalized comparison gained 24 function matches and lost none.

Peak frame size moved closer to OTP in 1,347 functions and farther in one. A total of 177 changed frames reach the OTP size. Of the 47 frame regressions in part 1372, 45 return to or improve upon the preceding distance from OTP. erl_bits:set_bit_1/2 improves from four slots to three and still needs two. snmpc_tok:get_name/3 retains four slots where OTP uses five. The new farther frame is reltool_sys_win:restart_server_safe_config/3, which drops from six slots to five where OTP uses six.

The exact-equality versus allocation group falls from 2,835 functions in 591 modules to 1,432 functions in 484 modules. is_default_1/1 and mk_object_val/3 in AttributeCertificateVersion1-2009 now test X0 before frame allocation. The try branch in is_default_1/1 still has seven slots and OTP uses one. The nonempty-list versus allocation group is largest by module count at 579.

The full runtime suite has not run on this build. Canonical readiness and publication remain pending.

- [Full corpus](otp-corpus.json)
- [Code size changes](code-delta.json)
- [Normalized function comparison](normalized-functions.txt)
- [Peak frame comparison](peak-slots.txt)
- [OTP loader check](loader.txt)
- [Frame audit](frame-shapes.txt)
- [Remaining function and chunk ranking](remaining-functions.txt)
- [Fixture source](literal_body_probe.erl)
- [Focused check](check-literal-body.ps1)
- [Focused output](probe.txt)
- [Fixture instructions](instructions.txt)
- [Release instructions](release-functions.txt)
- [Compiler tests](focused.json)
- [Whole-module test](whole-module.txt)
