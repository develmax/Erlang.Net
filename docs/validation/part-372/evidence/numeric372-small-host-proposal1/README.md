# Permanent SMALL parser HOST candidate

Two new test files plus a unique Program.cs registration anchor, source-only and unexecuted. No production files/resources/project files change.

The test invokes actual erlang:list_to_integer/1,2 through ProcessRuntime.Modules.Call on an owned actor. It covers positive/negative/wide SMALL bytes, native signed limits, actual versus wrapped signs, long wrapped-zero prefixes, decimal17/18 and hexadecimal14/15 fallback boundaries, every base2..36, invalid base/kind, improper suffix and out-of-base wrapped digits. Error checks require badarg, the exact native erlang/list_to_integer frame, argument count and original argument object identities, plus complete error_info/erl_erts_errors metadata. The existing compiled NumericSourceContractHost remains responsible for real caller file/line context; this new check supplements it.

Root applies the two source files and Program anchor, style/builds and runs this HOST after the SMALL patch and actual native observations. These checks do not replace the60 original numeric cases/full stacks or close large-bignum/system_limit/cancellation contracts.
