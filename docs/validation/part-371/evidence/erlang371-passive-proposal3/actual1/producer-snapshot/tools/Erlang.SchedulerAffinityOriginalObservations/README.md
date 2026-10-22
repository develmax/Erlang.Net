# Original two-lane scheduler profile

This separate manual workflow builds pinned OTP-29.1.1 commit
`ad05823719d77c8faee87348ea39513d4e2f99c5`, then captures five fresh original VMs
with explicit `+S 2:2`. The ordinary 6316-case corpus and its default flags are unchanged.

The source manifest binds this tool, all five fixtures, exact minor-version-2 ETF goldens,
the workflow and the existing owned-process helper. Before build and capture, admission verifies
the exact subject checkout and manifest SHA256. Build stages configure/make/install run as separate
bounded owned process groups; every raw stream, PID/exit/cleanup receipt and failure is retained.
Successful build admission requires zero exits, no termination or remaining-group kill request,
closed streams and reaped foreground parents. Before/after tracked OTP source identities must match.

The build receipt binds installed erl/erlc, erlexec/beam.smp, six runtime/compiler BEAM modules and
the installed OTP_VERSION marker. The collector rehashes those files, both original source manifests
and the build stage receipts/streams before it starts any fixture. This is source-built-install
provenance under the reviewed workflow, not a signature or independent reproducible-build claim.
`--prefix` is the configure installation prefix. **`--otp-install` is its actual runtime root,
`PREFIX/lib/erlang`, not PREFIX.** The workflow passes these distinct paths explicitly.

Each fresh fixture VM writes its actual `{scheduler_profile,2,2}` at entry before compiling or
loading the fixture. An unexpected configured or online count fails; no one-lane silent skip is
allowed. Inherited ERL_FLAGS/ERL_AFLAGS/ERL_ZFLAGS/ERL_LIBS/ERL_COMPILER_OPTIONS are removed.
The original VM checks the loaded fixture's code MD5 against beam_lib:md5 and retains raw image ETF.
Each case retains raw returned ETF, compiled caller protocol ETF and returned/raised envelope ETF.
Source-derived expected results are `done`, `{ok,done}` and `{returned,done}`; no managed result seeds
an original expectation. All result files are compared exactly with their declared minor-version-2
contract. stdout/stderr remain raw and are never normalized or used as invented semantic authority.

Five ordered cases cover first-instruction lane, migration/previous binding, invalid/duplicate
options, offline-bound resume, and pending guest kill admitted after re-online. The fifth is still
a source-guided hypothesis until this actual original run succeeds. Every case has a 45-second
deadline. A timeout, nonzero VM exit, forced group cleanup, mismatched result or missing provenance
cannot produce completion PASS. Earlier observed cases and the failing raw capture remain partial.

This source-only proposal has not executed OTP, the workflow or a managed interoperation test.
It makes no exact timing, physical CPU placement, dirty scheduler, native epoch or performance claim.
Per-API compatibility statuses do not change automatically after five finite observations.

Revision3 corrects two pre-execution source findings in revision2. OTP tracked mode120000
entries (including directory links) are inventoried from raw readlink payload, not their targets;
regular100644/100755 files retain raw bytes, and unknown modes/gitlinks/unmerged stages are refused.
ERL_ROOTDIR is cleared for build and capture along with the other inherited Erlang overrides.
Each actual fixture VM records both expected and actual code:root_dir in raw runtime-root.etf
and asserts equality before compilation; Python independently checks the exact typed root values.
Revision2 and its source-only9050 review remain historical; neither revision has native execution.
