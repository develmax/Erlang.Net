#!/bin/sh
set -eu
task_root=$(pwd)
mkdir -p artifacts/nif-platform-ci
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git artifacts/nif-platform-ci/otp
test "$(git -C artifacts/nif-platform-ci/otp rev-parse HEAD)" = ad05823719d77c8faee87348ea39513d4e2f99c5
cd artifacts/nif-platform-ci/otp
./configure --prefix="$task_root/artifacts/nif-platform-ci/installed" --without-javac --without-wx --without-odbc --without-debugger --without-observer --without-et --without-ssl --without-termcap --disable-jit
cd "$task_root"
python3 tools/Native/Erlang.NifPlatformValidation/prepare-configured-headers.py \
  --repository . --otp-source artifacts/nif-platform-ci/otp --output artifacts/nif-platform-ci/headers
python3 tools/Native/Erlang.NifNativeBackend/build-unix-bridge.py \
  --repository . --otp-source artifacts/nif-platform-ci/otp \
  --otp-include artifacts/nif-platform-ci/headers \
  --output artifacts/nif-platform-ci/native --smoke-fixtures
dotnet build tools/Native/Erlang.NifPlatformValidation -m:1
cp artifacts/nif-platform-ci/native/Erlang.NifBridge.so tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/
dotnet tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll \
  "$task_root/artifacts/nif-platform-ci/native/nif_platform_probe.so" \
  > artifacts/nif-platform-ci/managed-checks.json
(cd artifacts/nif-platform-ci/otp && make -j4 && make install)
python3 tools/Native/Erlang.NifPlatformValidation/build-platform-fixture.py \
  --repository . --otp-root artifacts/nif-platform-ci/installed/lib/erlang \
  --bridge artifacts/nif-platform-ci/native/Erlang.NifBridge.so \
  --output artifacts/nif-platform-ci/image
dotnet tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll \
  --image artifacts/nif-platform-ci/image/ebin/nif_platform_fixture.beam \
  artifacts/nif-platform-ci/image/managed/nif_platform_fixture.so \
  artifacts/nif-platform-ci/image/managed.etf \
  > artifacts/nif-platform-ci/image/managed-checks.json
cmp artifacts/nif-platform-ci/image/original.etf artifacts/nif-platform-ci/image/managed.etf

sh tools/Native/Erlang.NifPlatformValidation/run-unmodified-image.sh
