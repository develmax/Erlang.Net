# Part 1043 capture ordering experiment

OTP beam_core_to_ssa builds lifted environments from an ordered set of Core variable identities. The sample Core dump retains names for Includes and Options2. The positional probe shows a different case where OTP renames incoming parameters to numbered Core variables, so sorting all source names gives the wrong order.

The revised experiment kept incoming slots in positional order and sorted other captures by source name in the general emitter. It removed the swap from asn1ct's compile closure and the local capture probe. Both execution probes passed in OTP. The revised build passed all three focused checks. The first experimental build timed out on one focused case during parallel validation.

The revised experiment kept all 1366 byte-comparison statuses and all 1021 differing files still prepared in OTP. The register-only lifted tail body analysis paired functions by name and arity and excluded labels, line markers and headers. It found 12 new body matches and 13 lost matches. One observed distinction is that tuple-bound names and mixed-clause input aliases can be assigned different Core identities from those inferred by the experiment.

The compiler changes were reverted. Part 1042 remains the retained compiler baseline. The experimental data below records the discarded implementation.

- [Revised experimental corpus](experimental-otp-corpus.json)
- [Revised experimental loader results](experimental-loader.txt)
- [Affected lifted bodies](experimental-tail-transitions.txt)
- [asn1ct Core dump](asn1ct-core.txt)
- [asn1ct lifted body](asn1ct-tail.txt)
- [Local capture probe](local_capture_probe.erl)
- [Positional capture probe](capture_order_probe.erl)
