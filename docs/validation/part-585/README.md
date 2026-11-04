# Part585: PR111 main conflicts and source admission repair

Merge main48eff39281cd81a2938e68dfd6a93d6f08899948 into PR111, retaining
code, file and gen_event startup registrations and keeping File's published enum
ordinal. Original gen_event evidence is relocated from the conflicting Part580
directory to Part584; main's file Part580/581/582/583 evidence stays unchanged.
Both sets of journal entries are retained. Historical reports keep their bytes
and recorded source/assembly identities.

The review finding in summarize-gen-event-audit.ps1 is reproduced: the previous
script admits a Beam report after the retained gen_event.beam was changed.
Compare InputSha256 against the current retained image before admission. A
changed image and a missing image hash now fail. Production images are never
mutated by these controls. The baseline remains OTP29.1.1/ad058237.

Refresh declared source identities and producer/workflow pins without changing
statuses, approvals, contract membership or required gates. Add all41 gen_event
C# files to the four existing broad source scopes; preserve each scope's hash
mode. Create a successor Monitor freeze, preserving historical freezes. The
source-plan-refresh helper/audits list additions and byte-identity changes.

Focused build: zero warnings/errors. Fresh Managed105/105 and Beam105/105 both
pass and are admitted against current source/assemblies. All13 passive plans,
Resource529 admission and5400 MFA/native raw bindings pass. The first CI helper
unit run used the parent checkout HEAD and failed to locate the new freeze;
that failure is retained. Validation with an isolated candidate HEAD and exact
Pending membership is recorded separately. No full regression or manual CI
dispatch is run. Prior depth3 timing failure remains open historical evidence;
no scheduling or100% compatibility promotion is made.

Reproduce from the source tree with the focused gen_event commands documented
in ../part-584/README.md. Run the passive workflow Python block retained in
checks/preflight.py, the Resource529 verify-source-plan.py, MFA raw binding
verifier and the CIReuse unit tests. Source-only admissions do not execute native
code or certify Linux runtime behavior. Hosted final-head CI remains required.

Final candidate-HEAD results: all23 CIReuse tests pass, including both Windows
checkout-filter modes. Exact reviewed input membership passes at4991 rows.
The earlier missing-index membership attempts are retained as setup failures;
the final check explicitly uses the candidate index and isolated HEAD.
