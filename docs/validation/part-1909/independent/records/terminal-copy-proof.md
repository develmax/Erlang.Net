# Checked tuple projection directly into terminal registers

Pinned Windows OTP 29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. This is a separate additive patch over the verified checked guard motion. Its SHA-256 is D947BCFFEAAD3E46BB10F651D950F4FC7159C8F8DBBD0A287ED0D2A5AF471E72. The parent compiler SHA-256 is E20C67F046F973EB6FD1943D881262025A9BA31D05C2AECBE0A8E10246A50DDE. The candidate compiler SHA-256 is 3286E55D419876ED028DFF0053520B7B06F576F62BCD0767112650533D2081DD. Source anchors are in parent-sha256.json.

Pinned beam_ssa_pre_codegen reserve_xregs provides preferred registers for variables passed to calls when the lifetime permits it. The observed release and synthetic returns similarly use X0 directly. A checked local projection followed by one X-to-X copy can use the terminal register directly when its scratch X is outside the complete implicit argument prefix. The terminal operation must be a Return or an unframed local or external tail call, with only optional source lines after the copy. This preserves successful argument values and every earlier failure edge. A duplicated temporary, typed interval, GC operation, constructor use or different local check sequence declines this rewrite.

The uncached 16-module retake used one worker and took 92.166 s. Results are 5 Equal, 11 Different, 0 Unsupported and 0 SourceRejected. All five exact controls and the preceding four normalized body gains were retained. The eight changed files affected Code and derived Attr/FunT identities only, with zero raw section equality gains or losses. In 162 normalized bodies changed, eight gained exact instruction and source-line-position equality and zero were lost.

| Module | Function with equal normalized instructions and source lines |
| --- | --- |
| dialyzer | pos/2 |
| dialyzer_cl | set_location/2 |
| dialyzer_incremental | set_location/2 |
| diameter_tcp | get_addr/1 |
| dist_ac | dist_find_nodes/2 |
| dtls_gen_connection | send/3 |
| erl_anno | simplify/1 |
| erl_lint | '-value_option/7-fun-0-'/7 |

Native OTP prepared all eight changed release images. Reference, parent and candidate agree on 80 synthetic values/effects, six real Dialyzer formatting cases and four real erl_anno simplification cases. Test compiler/beam/projects-checked-fields-directly-into-terminal-registers passes on the candidate and fails its structural assertion on the exact parent. It includes a nonzero tail destination, a result needed by two outgoing arguments, and an original tuple needed by a constructor. Source layout and isolated git apply check pass. The raw first SDK compile error was preserved and corrected by using the shared BeamWire.UnsignedTag. Full corpus validation and publication are outside this experiment.
