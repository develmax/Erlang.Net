# Independent original OTP NIF observations — proposal only

**No C compilation, erlc, native VM, network, dispatch or Main change has been performed.** This directory prepares a reusable observer for an already source-built pinned Linux OTP tree. It does not download/build a production emulator or add a production BEAM dependency to Erlang.Net.

The C fixture is byte-exact to artifacts/memory-allocator-nif-audit/fixture.c, SHA256 **7B6AF591C635DED2FE686A9E8671ACD58F9C8A6A080D47C61420F844FF33F074**. Its two NIF functions and load/unload callbacks are unchanged. The declaring module has real erlang:load_nif/2 loading and not_loaded fallbacks. The observer independently calls original OTP and retains complete returned terms or full {exception,Class,Reason,Stack}; it does not insert SDK expected results into native output.

## Deferred collection recipe

After root reviews source-plan.json and grants a serial native slot:

```text
python3 artifacts/nif-original-observer-proposal/collect.py \
  --otp-source /absolute/pinned-built-otp \
  --otp-include /absolute/pinned-built-otp/usr/include \
  --otp-bin /absolute/pinned-built-otp/bin \
  --fixture-source /absolute/reviewed-tool/native/fixture.c \
  --output /absolute/new-nif-observation-directory \
  --approved-plan-sha256 <independently-reviewed-plan-sha256>
```

Point otp-include and otp-bin at actual generated headers and executables of the same built tree. The explicit fixture-source may point at the promoted tool's native/fixture.c, but must match the unchanged reviewed source hash. HEAD must be exactly ad05823719d77c8faee87348ea39513d4e2f99c5 with clean tracked sources; untracked build products are permitted. Each public header must byte-match its Git blob at that commit. The actual configured erl_int_sizes_config.h is copied and hashed, rather than synthesizing a Linux size header or using newer checkout headers. Existing gcc/readelf and built erl/erlc are required. No install/restore/network fallback exists.

gcc builds the unchanged C fixture with -shared -fPIC -O2 -std=c11 -Wall -Wextra -Werror against those hydrated built headers. readelf symbols/dynamic dependencies, compiler version, compile logs and binary hashes remain raw evidence. erlc compiles both declaring and observer modules with -Werror +debug_info. A metadata VM records original OTP/ERTS/system architecture/word size/emulator/build profile; another retains complete BEAM disassembly. Each of the seven observation cases launches a **fresh** original VM, loads its own fresh fixture instance, preserves ETF plus human output and process stdout/stderr/exit status. The collector stops on process failure and never overwrites an existing evidence directory.

## Seven raw cases and later independent review

| Case | Source-derived contract to review against actual output |
| --- | --- |
| increment_zero | Before loading: error not_loaded. LoadInfo42: ok. Native increment0 returns1. |
| increment_negative | LoadInfo42 succeeds; native increment(-1) returns0. |
| increment_maximum_success | Native increment2147483646 returns2147483647. |
| increment_maximum_badarg | Native increment2147483647 raises error badarg. |
| increment_atom_badarg | Native increment(not_an_integer) raises error badarg. |
| greeting | Native function returns Latin1 list "native hello". |
| load_veto | LoadInfo17 returns original load error; subsequent increment0 executes original not_loaded fallback, proving no successful native installation. |

Those are review obligations, **not executed assertions or native results**. Each case records its pre-load fallback, exact load result and exact subsequent call result. A failed load in a nominal success case is not success merely because the observer process exits0. The receipt says semantic assertions not executed; independent typed review must inspect load ok and the actual operation separately. Preserve the full native load-veto message/exception details without guessing OS-specific text or replacing complete stacks. Portable class/reason/value comparisons may later be explicit, but this proposal makes no SDK-versus-original full-stack equivalence claim.

The C fixture's extra unload-counter export is observable through the managed native-library bootstrap, but the unchanged Erlang module exposes only increment/1 and greeting/0. This observer **cannot independently read that counter through Erlang**. It therefore deliberately omits original-versus-managed unload-counter comparison and makes no unload-on-purge equivalence claim. Do not add a third NIF function or mutate the fixture to close that gap unnoticed; a future independently reviewed lifecycle probe is needed.

The collector receipt binds sources/headers/native library/tools/raw files and ordered fresh VM names. Complete=true means collection finished, not semantic compatibility. Process failures/timeouts preserve stdout/stderr/return status, available ETF files and an explicit Complete=false failure receipt; a preflight refusal before output creation emits Complete=false on stderr. Root still needs original typed review, comparison to actual SDK native fixture observations, and separate Main integration evidence before publishing behavioral conclusions.
