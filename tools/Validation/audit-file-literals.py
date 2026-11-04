"""Reject embedded protocol/configuration literals in file behavior and replay code.

Literal declarations belong in semantic owner files. Small tuple/argument indexes,
arithmetic identities and the explicit MFA export declarations remain readable
inline. Erlang fixtures are independent contract inputs, not production values.
"""
import argparse
import json
from pathlib import Path
import re
import shutil
import tempfile

TOKEN = re.compile(
    r'(?P<comment>//[^\n]*|/\*.*?\*/)'
    r'|(?P<string>\$?@"(?:""|[^"])*"|\$?"(?:\\.|[^"\\])*"|\'(?:\\.|[^\'\\])*\')'
    r'|(?P<number>\b(?:0[xX][0-9A-Fa-f]+|\d+(?:\.\d+)?)(?:[uUlLfFdDmM]+)?\b)',
    re.DOTALL,
)


def audit(repository):
    area = repository / 'src/Otp/Managed/Erlang.Otp.Kernel/FileSystem'
    paths = sorted(area.glob('FileModule*.cs')) + [area / 'FileSource.cs']
    paths += [repository / 'tools/Validation/Erlang.FileCompatibility' / name
              for name in ('Program.cs', 'FileCompatibilityAdmission.cs')]
    findings = []
    for path in paths:
        source = path.read_text(encoding='utf-8-sig')
        for token in TOKEN.finditer(source):
            if token.lastgroup == 'comment':
                continue
            line_start = source.rfind('\n', 0, token.start()) + 1
            line_end = source.find('\n', token.end())
            if line_end < 0:
                line_end = len(source)
            line = source[line_start:line_end]
            if token.lastgroup == 'string' and re.fullmatch(r'\s*public const string Name = "file";\s*', line):
                continue
            if token.lastgroup == 'number':
                if token.group() in ('0', '1', '2', '3'):
                    continue
                if re.fullmatch(r'\s*\(FileFunctionNames\.\w+, \d+\),?\s*', line):
                    continue
            findings.append({'Path': path.relative_to(repository).as_posix(),
                             'Line': source.count('\n', 0, token.start()) + 1,
                             'Literal': token.group()})
    return {'Files': len(paths), 'Findings': findings}


def self_test(repository):
    artifacts = repository / 'artifacts'
    artifacts.mkdir(exist_ok=True)
    with tempfile.TemporaryDirectory(prefix='file-literal-audit-', dir=artifacts) as temporary:
        temporary = Path(temporary).resolve()
        assert temporary.is_relative_to(artifacts.resolve())
        area = Path('src/Otp/Managed/Erlang.Otp.Kernel/FileSystem')
        for path in (repository / area).glob('FileModule*.cs'):
            destination = temporary / path.relative_to(repository)
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(path, destination)
        for relative in [area / 'FileSource.cs',
                         Path('tools/Validation/Erlang.FileCompatibility/Program.cs'),
                         Path('tools/Validation/Erlang.FileCompatibility/FileCompatibilityAdmission.cs')]:
            destination = temporary / relative
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copyfile(repository / relative, destination)
        candidate = temporary / area / 'FileModule.cs'
        original = candidate.read_text(encoding='utf-8')
        baseline = not audit(temporary)['Findings']
        candidate.write_text(original.rsplit('}', 1)[0] +
                             '    private static string AtomControl() => "file_request";\n}\n', encoding='utf-8')
        atom = bool(audit(temporary)['Findings'])
        candidate.write_text(original.rsplit('}', 1)[0] +
                             '    private static int NumberControl() => 4096;\n}\n', encoding='utf-8')
        number = bool(audit(temporary)['Findings'])
        candidate.write_text(original + '\n// Documentation sample: "file_request", 4096.\n', encoding='utf-8')
        comment = not audit(temporary)['Findings']
        return {'BaselineAccepted': baseline, 'AtomRejected': atom,
                'NumberRejected': number, 'CommentAccepted': comment}


if __name__ == '__main__':
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument('repository', type=Path)
    parser.add_argument('--output', type=Path)
    parser.add_argument('--self-test', action='store_true')
    arguments = parser.parse_args()
    result = audit(arguments.repository.resolve())
    if arguments.self_test:
        result['Controls'] = self_test(arguments.repository.resolve())
    serialized = json.dumps(result, indent=2)
    if arguments.output:
        arguments.output.write_text(serialized + '\n', encoding='utf-8')
    print(serialized)
    raise SystemExit(1 if result['Findings'] or not all(result.get('Controls', {}).values()) else 0)
