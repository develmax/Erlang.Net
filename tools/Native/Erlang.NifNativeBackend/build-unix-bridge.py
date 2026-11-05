"""Build the LP64 Unix NIF bridge on its target OS; never claim cross-host execution."""
import argparse
import ctypes
import hashlib
import json
import platform
import shutil
import subprocess
from pathlib import Path


def digest(path):
    return hashlib.sha256(path.read_bytes()).hexdigest().upper()


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('--repository', type=Path, required=True)
    parser.add_argument('--otp-include', type=Path, required=True,
                        help='Configured headers from pinned OTP-29.1.1, including erl_int_sizes_config.h')
    parser.add_argument('--otp-source', type=Path, required=True,
                        help='Read-only OTP Git checkout containing the pinned reference commit')
    parser.add_argument('--output', type=Path, required=True)
    parser.add_argument('--cc', default='cc')
    parser.add_argument('--smoke-fixtures', action='store_true')
    args = parser.parse_args()
    root = args.repository.resolve()
    output = args.output.resolve()
    if platform.system() == 'Windows' or ctypes.sizeof(ctypes.c_void_p) != 8:
        parser.error('Run on a 64-bit Unix target host')
    if not output.is_relative_to(root / 'artifacts') or output.exists():
        parser.error('Output must be a new directory below repository/artifacts')
    include = args.otp_include.resolve()
    baseline = json.loads((root / 'reference-baseline.json').read_text())
    for header in ('erl_nif.h', 'erl_nif_api_funcs.h', 'erl_drv_nif.h', 'erl_int_sizes_config.h'):
        if not (include / header).is_file():
            parser.error('Missing configured OTP header: ' + header)
    for header in ('erl_nif.h', 'erl_nif_api_funcs.h', 'erl_drv_nif.h'):
        original = subprocess.check_output(['git', '-C', str(args.otp_source.resolve()),
                                            'show', baseline['commit'] + ':erts/emulator/beam/' + header])
        if (include / header).read_bytes() != original:
            parser.error('Installed header differs from the pinned original: ' + header)
    compiler = shutil.which(args.cc)
    if compiler is None:
        parser.error('C compiler not found')
    native = root / 'tools/Native/Erlang.NifNativeBackend/native'
    sources = [native / name for name in (
        'nif_bridge_exports.c', 'nif_bridge_varargs.c',
        'nif_bridge_platform.c', 'nif_bridge_layout.c')]
    # The glibc namespace path forwards native worker/TLS providers to the
    # CLR's base libc. Portable process-global hosts use libc directly.
    glibc = platform.system() == 'Linux' and hasattr(ctypes.CDLL(None), 'gnu_get_libc_version')
    if glibc and platform.machine().lower() in ('x86_64', 'amd64', 'aarch64', 'arm64'):
        sources.append(native / 'nif_bridge_pthread_control.c')
    output.mkdir(parents=True)
    library = output / 'Erlang.NifBridge.so'
    flags = ['-std=c11', '-O2', '-fPIC', '-fvisibility=hidden', '-Wall', '-Wextra', '-Werror']
    flags += ['-dynamiclib'] if platform.system() == 'Darwin' else ['-shared']
    command = [compiler, *flags, '-I' + str(include), *map(str, sources), '-o', str(library)]
    if platform.system() == 'Darwin':
        command += ['-Wl,-install_name,@rpath/Erlang.NifBridge.so']
    elif platform.system() == 'Linux':
        command += ['-ldl', '-pthread', '-Wl,-soname,Erlang.NifBridge.so']
    result = subprocess.run(command, capture_output=True, text=True, timeout=120)
    (output / 'compile.stdout').write_text(result.stdout)
    (output / 'compile.stderr').write_text(result.stderr)
    if result.returncode:
        raise RuntimeError(result.stderr)
    bridge = ctypes.CDLL(str(library))
    layout = bridge.nif_bridge_platform_layout
    layout.argtypes = [ctypes.c_uint, ctypes.POINTER(ctypes.c_size_t), ctypes.c_uint]
    layout.restype = ctypes.c_int
    values = (ctypes.c_size_t * 13)()
    assert layout(1, values, len(values)) == 0
    assert tuple(values[:6]) == (8, 8, 96, 32, 40, 496)
    assert layout(0, values, len(values)) != 0
    open_file = bridge.nif_bridge_platform_open_file
    open_file.argtypes = [ctypes.c_char_p]
    open_file.restype = ctypes.c_int
    assert open_file(str(output / 'missing-file').encode()) < 0
    cwd = bridge.nif_bridge_platform_getcwd
    cwd.argtypes = [ctypes.c_void_p, ctypes.c_size_t]
    cwd.restype = ctypes.c_int
    buffer = ctypes.create_string_buffer(65536)
    assert cwd(buffer, len(buffer)) == 0 and buffer.value.startswith(b'/')
    assert cwd(buffer, 1) < 0
    open_library = bridge.nif_bridge_platform_open
    open_library.argtypes = [ctypes.c_char_p, ctypes.c_int]
    open_library.restype = ctypes.c_void_p
    assert not open_library(str(output / 'missing-nif.so').encode(), 0)
    probe_evidence = None
    if args.smoke_fixtures:
        probe = output / 'nif_platform_probe.so'
        probe_command = [compiler, *flags, '-I' + str(include),
                         str(native / 'nif_bridge_abi_probe.c'),
                         str(root / 'tools/Native/Erlang.NifPlatformValidation/alias_only.c'),
                         str(library), '-Wl,-rpath,' + str(output), '-o', str(probe)]
        subprocess.run(probe_command, check=True, timeout=120)
        probe_evidence = {'Command': probe_command, 'Sha256': digest(probe),
                          'Sources': {str(p.relative_to(root)): digest(p) for p in (
                              native / 'nif_bridge_abi_probe.c',
                              root / 'tools/Native/Erlang.NifPlatformValidation/alias_only.c')}}
    receipt = {
        'Status': 'NATIVE_BUILD_AND_LOADER_SMOKE_PASSED',
        'HostOS': platform.system(), 'HostArchitecture': platform.machine(),
        'OtpBaseline': baseline, 'Probe': probe_evidence,
        'Libc': platform.libc_ver(), 'Command': command,
        'CompilerVersion': subprocess.check_output([compiler, '--version'], text=True),
        'Sources': {str(p.relative_to(root)): digest(p) for p in sources + [native / 'nif_bridge_api.h', native / 'nif_snprintf791.h']},
        'Headers': {p.name: digest(p) for p in include.glob('*.h')},
        'NativeLayout': list(values), 'LibrarySha256': digest(library),
        'BridgeLoadedOnTarget': True, 'NifExecutedOnRuntime': False,
        'OtpCompatibilityAccepted': False
    }
    (output / 'build-receipt.json').write_text(json.dumps(receipt, indent=2) + '\n')
    print(json.dumps({'Receipt': str(output / 'build-receipt.json')}))


if __name__ == '__main__':
    main()
