# Controlled original dirty-NIF retirement observation

This Windows x64 collector compiles the checked-in C fixture against the pinned OTP-29.1.1 headers and executes it in the admitted original provider. It verifies all 6427 provider files before and after execution, the actual compiled source/DLL receipt, bounded process completion, whole raw returned ETF and actual module cleanup. Preparation failure prevents dependent execution.

The dirty CPU callback exposes independent entered/done/wait-result markers and waits for an explicit ordinary callback release. Before release the original process is logically dead (public and internal alive queries are false, name is absent and DOWN is delivered) while the actual native body remains unfinished. After release its physical marker completes. `is_process_executing_dirty/1` alone is not a physical-completion witness.

Run from the repository root with `py -3 -B tools/Native/Erlang.ProcessRetirement502OriginalObservations/collect.py --repository . --provider-admission artifacts/library376-official-compiler/official-admission.json --plan-sha256 63171EAC39BEF4AD0DEBE1F4CD09241EA6C4F6967CB8FABBCC638D6F540B7B76 --output artifacts/process-retirement502/fresh-output --check-golden`. The output directory must be fresh. CI repeats this collector separately from the managed regression and map-loader comparisons.

This is a controlled reference observation, not a new managed C NIF admission or a Linux/C++ TLS/general NIF ABI claim. C supplies the test fixture; the runtime implementation remains C#.
