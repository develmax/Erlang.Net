# Part 1164 — align constructor order with OTP Core

OTP Core binds append before it constructs the enclosing tuple. Early SSA lowers the append to put_list and retains source order for the other nested constructors. The embedded constructor planner now distinguishes rewritten append operands and schedules them first while retaining source order among the remaining fields.

The HTTP start_permanent_worker/5 instructions now match the release including local labels. The expanded probe verifies two nested fields and an improper list tail. Its instructions match after local label normalization and both images return the expected term.

The full run generated 1,363 images and returned 348 Equal, 1,015 Different and three SourceRejected. Only httpd_misc_sup changed. One function gained an instruction match and none lost one. ELDAPv3 remains Equal. OTP prepared all differing images for loading. All three focused tests passed.

- [OTP Core](httpd_misc_sup.core)
- [Early OTP SSA](httpd_misc_sup.ssa)
- [Release instruction comparison](httpd-worker.txt)
- [Probe disassembly](disassembly.txt)
- [Full corpus](otp-corpus.json)
- [Image delta](delta.json)
- [Function comparison](function-comparison.txt)
- [Focused tests](focused.json)
- [Loader](loader.txt)
