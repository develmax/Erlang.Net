#!/bin/sh
set -eu
task_root=$(pwd)
# Also execute the unchanged NIF image produced and run by the original VM.
if [ "$(uname -s)" = Darwin ]; then
  python3 tools/Native/Erlang.NifNativeBackend/build-macos-host.py \
    --repository . --otp-source artifacts/nif-platform-ci/otp \
    --otp-include artifacts/nif-platform-ci/headers --dotnet-root "$DOTNET_ROOT" \
    --output artifacts/nif-platform-ci/process-host
  fxr=$(find "$DOTNET_ROOT/host/fxr" -name libhostfxr.dylib | sort -V | tail -1)
  "$task_root/artifacts/nif-platform-ci/process-host/erlang-nif-host" "$fxr" "$DOTNET_ROOT" \
    "$task_root/tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll" \
    --image artifacts/nif-platform-ci/image/ebin/nif_platform_fixture.beam \
    artifacts/nif-platform-ci/image/original/nif_platform_fixture.so \
    artifacts/nif-platform-ci/image/unmodified.etf \
    > artifacts/nif-platform-ci/image/unmodified-checks.json
  python3 -c 'import json; r=json.load(open("artifacts/nif-platform-ci/image/unmodified-checks.json")); assert r["Passed"] and r["ProcessHost"]'
else
  dotnet tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll \
    --image artifacts/nif-platform-ci/image/ebin/nif_platform_fixture.beam \
    artifacts/nif-platform-ci/image/original/nif_platform_fixture.so \
    artifacts/nif-platform-ci/image/unmodified.etf \
    > artifacts/nif-platform-ci/image/unmodified-checks.json
fi
cmp artifacts/nif-platform-ci/image/original.etf artifacts/nif-platform-ci/image/unmodified.etf
