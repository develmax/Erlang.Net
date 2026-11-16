# Part 1315

The computed argument path reads the current location again after evaluating the argument. The nested call location can suppress an outer line instruction when it equals the caller location. This preserves the line state updates that take place during pattern emission.

A function clause whose discarded expressions are literals can emit its final constant directly. For the SNMP filter, the Windows release profile also records the build path of snmp_debug.hrl in the debug information. The generated snmpa_net_if_filter.beam matches the release image byte for byte.

- [SNMP release image comparison](snmp-filter.txt)
- [SNMP debug form difference before the profile fix](snmp-dbgi.txt)
- [Crypto image regression check](crypto-regression.txt)
- [TLS image regression check against part 1313](ssl-regression.txt)
- [Literal self-matches in the remaining corpus](literal-matches.txt)

The final corpus accepted and generated all 1,366 release files with 363 Equal and 1,003 Different. The SNMP filter moved to Equal and no Equal status regressed. The other 13 changed images narrowed their Code size gaps. Five of the ten images with a larger gap in part 1314 returned to the exact bytes generated in part 1313. The loader accepted all 1,003 differing images.

- [Full corpus](otp-corpus.json)
- [Changed images](delta.json)
- [Restored images](restored-images.json)
- [OTP loader](loader.txt)
