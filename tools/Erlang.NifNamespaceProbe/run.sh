#!/usr/bin/env bash
set -euo pipefail
source_dir=$(cd -- "$(dirname -- "$0")" && pwd)
output=$1
mkdir -p "$(dirname -- "$output")"
mkdir "$output"
output=$(cd -- "$output" && pwd)
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/bridge.c" -ldl -Wl,-soname,bridge.so -o "$output/bridge.so"
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/entry.c" -o "$output/entry-unlinked.so"
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/entry.c" -L"$output" -l:bridge.so '-Wl,-rpath,$ORIGIN' -o "$output/entry-linked.so"
gcc -shared -fPIC -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/capsule.c" -Wl,--no-as-needed "$output/bridge.so" "$output/entry-unlinked.so" '-Wl,-rpath,$ORIGIN' -o "$output/capsule.so"
readelf -d "$output/capsule.so" >"$output/capsule.dynamic.txt"
gcc -std=c11 -O2 -Wall -Wextra -Werror "$source_dir/driver.c" -ldl -o "$output/probe"
set +e
"$output/probe" "$output/bridge.so" "$output/entry-unlinked.so" >"$output/unlinked.stdout" 2>"$output/unlinked.stderr"
unlinked=$?
"$output/probe" "$output/bridge.so" "$output/entry-linked.so" >"$output/linked.stdout" 2>"$output/linked.stderr"
linked=$?
"$output/probe" "$output/capsule.so" >"$output/capsule.stdout" 2>"$output/capsule.stderr"
capsule=$?
set -e
printf '{"UnlinkedExitCode":%d,"LinkedExitCode":%d,"CapsuleExitCode":%d,"NativeProbeOnly":true,"ProductionNamespaceImplemented":false}\n' "$unlinked" "$linked" "$capsule" >"$output/result.json"
cat "$output/result.json"
cat "$output/unlinked.stdout" "$output/unlinked.stderr" "$output/linked.stdout" "$output/linked.stderr" "$output/capsule.stdout" "$output/capsule.stderr"
test "$linked" -eq 0
test "$capsule" -eq 0
