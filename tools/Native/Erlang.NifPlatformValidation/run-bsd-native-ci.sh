#!/bin/sh
set -eu
export PATH="/usr/pkg/bin:/usr/pkg/sbin:/usr/local/bin:$PATH"
task_root=$(pwd)
output="$task_root/artifacts/nif-bsd-native"
mkdir -p "$output/headers"
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git "$output/otp"
test "$(git -C "$output/otp" rev-parse HEAD)" = ad05823719d77c8faee87348ea39513d4e2f99c5
(cd "$output/otp" && MAKE=gmake ./configure --without-javac --without-wx --without-odbc --without-debugger --without-observer --without-et --without-ssl --without-termcap --disable-jit)
for header in erl_nif.h erl_nif_api_funcs.h erl_drv_nif.h; do
  git -C "$output/otp" show "HEAD:erts/emulator/beam/$header" > "$output/headers/$header"
done
set -- "$output"/otp/erts/include/*/erl_int_sizes_config.h
test "$#" = 1
cp "$1" "$output/headers/"
native=tools/Native/Erlang.NifNativeBackend/native
cc -std=c11 -O2 -fPIC -fvisibility=hidden -Wall -Wextra -Werror -shared \
  -I"$output/headers" "$native/nif_bridge_exports.c" "$native/nif_bridge_varargs.c" \
  "$native/nif_bridge_platform.c" "$native/nif_bridge_layout.c" -o "$output/Erlang.NifBridge.so"
cc -std=c11 -O2 -Wall -Wextra -Werror -I"$output/headers" \
  tools/Native/Erlang.NifPlatformValidation/unix_native_smoke.c "$output/Erlang.NifBridge.so" \
  -Wl,-rpath,"$output" -o "$output/smoke"
"$output/smoke" "$output/Erlang.NifBridge.so" > "$output/native-checks.json"
uname -a > "$output/host.txt"
cc --version > "$output/compiler.txt"
git -C "$output/otp" rev-parse HEAD > "$output/otp-commit.txt"
# Keep headers/evidence, not a full VM/reference source tree, in artifacts.
