# Complete Unicode PR108 CI producer bindings

Head9d925b24d passed the previously failed source admissions, but the completed
workflows still failed downstream. The earlier checkpoint did not establish full
CI success. This follow-up repairs the remaining producer/consumer identities.

Validate CLR implementation run41763841553 failed its22 CI-reuse controls before
building the final host. Integrated PlanPins still contained the old N1 producer
hashes. Downstream Windows comparisons then lacked their required host; those
errors are retained as dependent failures, not counted as executed comparisons.
The Linux resource-control job, Native GC and Math workflows passed at that head.

N1 run41763841582 built its native images and managed host, and passed GC binary
and mutex comparisons. Capture then rejected its native receipt because the
capture plan's BuildPlanSha256 still named the old build plan. Callback acceptance
separately rejected two stale FixtureManifests. No comparison is waived.

The repair updates explicit producer fields, all eleven integrated PlanPins,
qualified Packet/Tool/Repository source rows and the dependent existing workflow
pins. N2's wrong-entry Linux producer link also follows the current N1 build plan.
The byte refresh records identities only; OTP baseline, input membership,
approval/status flags, triggers, original outcome comparisons and cleanup gates
are preserved. Earlier evidence directories are unchanged.

The passive CI-reuse verifier now checks N1 capture/acceptance producer fields and
fixture manifest identities before SDK/product execution. Existing required-field
negative controls cover these roles too. Its22 tests include actual committed
workflow consistency and rejection of stale or missing environment pins. This
would catch the previously missed N1 producer links before building native code.

Local validation uses those22 helper controls, the13 passive source-plan checks,
Resource529, current MFA/native raw bindings, documentation layout and N1 source
bytes. No runtime/C# implementation changed in this follow-up; Part576's20 focused
groups and differential Unicode observations remain earlier-source evidence for
the unchanged implementation. No full local regression or manual workflow
dispatch is performed. Exact final-head CI remains required and is monitored
through completion; intermediate source-admission success is not workflow success.

Commands: `py -B -m unittest discover -s tools/Validation/Erlang.CiReuse`;
the existing validate.yml13-plan admission; Resource529 verify-source-plan.py;
verify-mfa-source-bindings.py; `node tools/Validation/check-documentation-layout.cjs`;
N1 byte checks. Reports and original failure excerpts are retained here. The
downloaded N1 artifact digest is recorded with its originating run/head; no
historical source freeze is rewritten and no new compatibility credit is claimed.
