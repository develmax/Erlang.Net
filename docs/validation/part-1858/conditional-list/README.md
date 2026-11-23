# Conditional ordinary list helper verification

Input seal ../launch-inputs.json SHA-256 EF455D14DBA74E22E972461EA9857B6EF139A0774CC59747764209D8B6C21B46. OTP-29.1.1 commit ad05823719d77c8faee87348ea39513d4e2f99c5. Standalone integration patch SHA-256 2C771C95D4FED8F74AED188EEFFFEBEEADD6B9D3DCB96EB823F5DF1211432709. Candidate compiler SHA-256 1CCBC84C8990C38C081102CF0AFB43DED95263EE37DDD85FBB2F39B90736625C.

| Seven-image uncached scope, jobs=1 | Baseline | Candidate |
|---|---:|---:|
| Equal | 2 | 2 |
| Different | 5 | 5 |
| Unsupported | 0 | 0 |
| SourceRejected | 0 | 0 |
| Corpus comparison time | 6.79 s | 6.32 s |

Exactly the three MEGACO binary name resolver outputs changed. In each, the new '-to_lower/1-lc$^0/1-0-'/1 equals OTP after normalizing physical labels, including source lines and typed operands. Its existing to_lower/1 caller also becomes newly normalized-exact. Four controls retain their exact previous file bytes, including both release equals. No common-function normalized loss or whole-file gain/loss. Changed sections are AtU8, Attr, Code, ExpT, ImpT, Line, LocT, and Type in all three. Fun table identity shape is unchanged.

Both C# builds succeeded with zero warnings or errors. The frozen compiler fails the permanent test at its intended missing-helper assertion. The candidate passes the helper shape and six interpreted outcomes, covering numeric and nonnumeric fallthrough, nil, bad input/tail, effect order, and abort of later effects. ERTS separately loaded both fixture images and agreed on all six outcomes and both bad_generator error reasons at line 7. Only the candidate reports the pinned generated helper in these stacks.

ERTS prepared all three changed release images, then loaded and executed each variant's actual decode_name/3 on device names with boundary integers, floating and nonnumeric values, empty lists, and an improper tail. Candidate reports the reference helper name and source lines 503, 518, and 577 in the invalid-tail stacks. Previous reports the correct lines under to_lower/1 because it used a reversed accumulator. See helper-exact.log and release-*-native.log for raw evidence.

Managed setup, build, red/green and the two comparisons took 98.55 s in one BelowNormal worker with DOTNET_PROCESSOR_COUNT=1. Compiler build took 66.69 s, isolated test build 6.01 s and independent native validation 4.93 s. This isolated scope performed no broad corpus run or shared integration build.
