#!/bin/sh
set -eu
task_root=$(pwd)
output="$task_root/artifacts/nif-freebsd"
mkdir -p "$output/dotnet"
tar -xzf artifacts/freebsd-dotnet.tar.gz -C "$output/dotnet"
export DOTNET_ROOT="$output/dotnet"
export PATH="$DOTNET_ROOT:/usr/local/bin:$PATH"
# .NET's FreeBSD14 build probes .so.30/.so.12; FreeBSD15 uses a newer
# OpenSSL3 SONAME. Select the library actually resolved by this target loader.
export DOTNET_OPENSSL_VERSION_OVERRIDE=$(python3.11 - <<'PY'
import ctypes, ctypes.util, json
from pathlib import Path
name = ctypes.util.find_library('ssl')
assert name and '.so.' in name, name
library = ctypes.CDLL(name)
version = library.OpenSSL_version
version.argtypes = [ctypes.c_int]
version.restype = ctypes.c_char_p
text = version(0).decode()
assert text.startswith('OpenSSL 3.'), text
suffix = name.rsplit('.so.', 1)[1]
Path('artifacts/nif-freebsd/openssl.json').write_text(json.dumps({'Library':name,'Version':text,'VersionOverride':suffix}, indent=2) + '\n')
print(suffix)
PY
)
dotnet --info > "$output/dotnet-info.txt"
dotnet --list-runtimes > "$output/dotnet-runtimes.txt"
uname -a > "$output/host.txt"
# The managed assemblies were built with pinned SDK10.0.400 on Linux.
# Replace the Linux-generated host boundary with a real FreeBSD library.
cc -std=c11 -O2 -fPIC -Wall -Wextra -Werror -shared \
  src/Runtime/Erlang.Runtime.Core/Platform/Posix/FileSystemNative/filesystem.c \
  -o tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/libErlang.FileSystem.Native.so
mkdir -p artifacts/nif-platform-ci
git clone --depth 1 --branch OTP-29.1.1 https://github.com/erlang/otp.git artifacts/nif-platform-ci/otp
test "$(git -C artifacts/nif-platform-ci/otp rev-parse HEAD)" = ad05823719d77c8faee87348ea39513d4e2f99c5
(cd artifacts/nif-platform-ci/otp && MAKE=gmake ./configure --prefix="$task_root/artifacts/nif-platform-ci/installed" --without-javac --without-wx --without-odbc --without-debugger --without-observer --without-et --without-ssl --without-termcap --disable-jit)
python3.11 tools/Native/Erlang.NifPlatformValidation/prepare-configured-headers.py --repository . --otp-source artifacts/nif-platform-ci/otp --output artifacts/nif-platform-ci/headers
python3.11 tools/Native/Erlang.NifNativeBackend/build-unix-bridge.py --repository . --otp-source artifacts/nif-platform-ci/otp --otp-include artifacts/nif-platform-ci/headers --output artifacts/nif-platform-ci/native --smoke-fixtures
cp artifacts/nif-platform-ci/native/Erlang.NifBridge.so tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/
dotnet tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll \
  "$task_root/artifacts/nif-platform-ci/native/nif_platform_probe.so" > artifacts/nif-platform-ci/managed-checks.json
python3.11 -c 'import json; r=json.load(open("artifacts/nif-platform-ci/managed-checks.json")); assert r["Architecture"]=="X64" and r["Host"].startswith("freebsd"); assert all(x["Passed"] for x in r["Results"])'
(cd artifacts/nif-platform-ci/otp && gmake -j4 && gmake install)
python3.11 tools/Native/Erlang.NifPlatformValidation/build-platform-fixture.py --repository . --otp-root artifacts/nif-platform-ci/installed/lib/erlang --bridge artifacts/nif-platform-ci/native/Erlang.NifBridge.so --output artifacts/nif-platform-ci/image
dotnet tools/Native/Erlang.NifPlatformValidation/bin/Debug/net10.0/Erlang.NifNativeBackend.dll --image artifacts/nif-platform-ci/image/ebin/nif_platform_fixture.beam artifacts/nif-platform-ci/image/managed/nif_platform_fixture.so artifacts/nif-platform-ci/image/managed.etf > artifacts/nif-platform-ci/image/managed-checks.json
cmp artifacts/nif-platform-ci/image/original.etf artifacts/nif-platform-ci/image/managed.etf
# FreeBSD can load the same unchanged ELF NIF through process-global exports.
sh tools/Native/Erlang.NifPlatformValidation/run-unmodified-image.sh
