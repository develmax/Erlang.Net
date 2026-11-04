# Part588: qualify the ASN.1 file module atom

Main run41841256172 passes source admission and then fails the solution build:
Asn1LibraryChecks.cs:129 CS0119, FileModule is a type invalid in this context.
The newly retained C# file library exposes Erlang.FileModule. In the nested
Erlang.Otp.Asn1.Library.Tests namespace, the bare FileModule reference resolves
to that type instead of the statically imported string constant.

Qualify the existing atom as Asn1LibraryNames.FileModule in the observer's
dependency list. Keep its value, dependency membership and original fixtures
unchanged. The existing package consumer is the regression test; do not rename
the supported production type or introduce another literal.

Reproduced the same CS0119 on current main1a2e3e359. The focused project then
builds with0 warnings/errors and passes65 package checks, including26 complete
outcome/stack matches. The admitted OTP29.1.1 original observer independently
confirms the same deterministic raw ETF hash and verifies packaged originals.
SDK source layout, all13 passive source plans and5400 raw MFA/native bindings
pass. No source-plan identity updates are needed for this test-only change.

Commands from the isolated source root:

    dotnet build tests/Erlang.Otp.Asn1.Library.Tests -m:1 --artifacts-path ../build
    dotnet ../build/bin/Erlang.Otp.Asn1.Library.Tests/debug/Erlang.Otp.Asn1.Library.Tests.dll artifacts/ci588-managed
    pwsh tools/validate-otp-asn1-original.ps1 -ManagedDirectory artifacts/ci588-managed -Admission <official-admission.json> -Output artifacts/ci588-original

The before/after build logs, host outcomes, fresh original receipt and tested
source hash are retained here. No full regression, readiness refresh or manual
CI dispatch is run. No runtime, package API, license or compatibility status
changes. Hosted checks on the published head remain required.
