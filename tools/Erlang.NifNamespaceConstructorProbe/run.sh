#!/usr/bin/env bash
set -euo pipefail
source_dir=$(cd -- "$(dirname -- "$0")" && pwd)
output=$1
test ! -e "$output"
mkdir -p "$(dirname -- "$output")"
mkdir "$output"
output=$(cd -- "$output" && pwd)
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/bridge.c" -Wl,-soname,constructor-bridge.so -o "$output/constructor-bridge.so"
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/entry.c" -o "$output/entry-unlinked.so"
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/capsule.c" -Wl,--no-as-needed "$output/constructor-bridge.so" "$output/entry-unlinked.so" '-Wl,-rpath,$ORIGIN' -o "$output/capsule.so"
gcc -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/driver.c" -ldl -o "$output/probe"
readelf -d "$output/capsule.so" >"$output/capsule.dynamic.txt"
readelf -d -Ws "$output/entry-unlinked.so" >"$output/entry.dynamic-symbols.txt"
sha256sum "$output/constructor-bridge.so" "$output/entry-unlinked.so" "$output/capsule.so" >"$output/images.before.sha256"
set +e
"$output/probe" "$output/constructor-bridge.so" "$output/capsule.so" >"$output/probe.stdout" 2>"$output/probe.stderr"
probe=$?
set -e
sha256sum -c "$output/images.before.sha256" >"$output/images.after-check.txt"
passed=false
if [ "$probe" -eq 0 ]; then passed=true; fi
printf '{"ConstructorCapsuleExitCode":%d,"TwoSimultaneousNamespaces":%s,"RegisteredBeforeTargetConstructors":%s,"SameUnlinkedTargetBytes":true,"NativeProbeOnly":true,"ProductionNamespaceImplemented":false}\n' "$probe" "$passed" "$passed" >"$output/result.json"
cat "$output/result.json" "$output/probe.stdout" "$output/probe.stderr"
test "$probe" -eq 0
