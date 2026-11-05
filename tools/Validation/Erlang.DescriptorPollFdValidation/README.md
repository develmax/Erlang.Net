# Focused integrated descriptor validation1576

This launcher calls the existing public DescriptorIoContracts.RunReference and RunCancellation methods through the existing Differential project. It defines no replacement fixtures, engine, native operations, cancellation outcome or physical-cleanup certificate. Production sources remain authoritative.

Stage this directory as `tools/Validation/Erlang.DescriptorPollFdValidation`. Keep its source-plan.json and every declared input byte-identical to the committed candidate. Run from the actual repository checkout with exact .NET10.0.11 installed:

```bash
python3 tools/Validation/Erlang.DescriptorPollFdValidation/run-ci.py \
  --expected-head "$GITHUB_SHA" \
  --output "$GITHUB_WORKSPACE/artifacts/poll-fd1575"
```

Output must be a new directory: failed prior outputs are retained. Default provider preparation builds original OTP29.1.1 once from exact source ad05823719d77c8faee87348ea39513d4e2f99c5 using the existing beam-fixtures workflow configure/make/install recipe. Build logs, installed-version proof, full installed-byte inventory and build receipt are retained. Original comparisons cannot start until this preparation and a real executable release check succeed. This source build can take much longer than the component tests; allow an appropriate job budget (for example60 minutes).

An already installed provider may instead be admitted using `--provider-prefix ABSOLUTE_PREFIX --provider-receipt RECEIPT_JSON --provider-receipt-sha256 APPROVED_SHA256`. The receipt must have this runner's strict schema: exact source commit/tag, completed source build, version29.1.1, identical original installed prefix and complete unchanged installed-file/symlink inventory. A mere tag, version string, newly recomputed receipt or unapproved hash does not establish provenance. For an existing provider with a different established receipt format, adapt admission after review rather than weakening these checks.

Both Debug and Release independently build the launcher and its existing Differential/compiler/runtime dependencies. Failed preparation skips only dependent executions for that profile. Comparison and idle cancellation run in separate actual processes; failure of one does not skip the other or later profiles. Rejected original-provider preparation still allows both independent SDK cancellation regressions, while overall acceptance fails. Popen failures, timeouts, force cleanup, descendant process groups, nonzero exits, malformed/missing JSON and changed input sources all remain visible failures. Root collector summary succeeds only after four accepted observations:8 original comparisons and1 idle regression in each profile.

The8 original comparisons retain their existing JSON/native source/stdout/stderr and actual output-byte checks: stream, packet-one/two/four, line, list, reject-exit-status and eof-closes. The existing idle regression closes the port while its pipe writer remains open, then verifies borrowed OS handles still work. Successful return also includes the existing CompiledModuleExecution await-using runtime disposal. No full statistics contract, arbitrary external active FD reuse or blocked transfer cancellation is promoted by this package.

Minimum cone-mode sparse checkout paths for project-reference builds:

```text
.github/workflows
src/Runtime
src/Compiler
src/Otp/Managed
src/Otp/Original/Foundation/Erlang.Otp.Erts.Beam
tools/Native/Erlang.PortProbe
tools/Validation/Erlang.Differential
tools/Validation/Erlang.DescriptorPollFdValidation
```

Cone checkout includes root files, including global.json and Directory.Build.props/targets. Existing imported metadata or root build settings must remain available. Full tests/Erlang.Tests is not built. Root may reuse previously built matching project outputs naturally through incremental MSBuild; this runner never substitutes the isolated component engine for production runtime.

Artifact preparation itself has not built the runtime, downloaded OTP, executed native code or modified repository/CI state. Root owns staging, exact final commit, dispatch and actual result acceptance.

Successor1576 retains the admitted provider prefix and exact copied receipt hash before executable use. Its independent finally path re-reads that frozen receipt, verifies the exact hash/source pin/version/prefix, and compares the entire installed file/symlink inventory after all consumers, including later failure paths. Provider and source checks are independent; either failure rejects the summary. Original1575 remains retained.
