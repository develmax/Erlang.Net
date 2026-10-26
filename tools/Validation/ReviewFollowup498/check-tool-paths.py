"""Execute the relocated observers' real import setup and resolve their active paths."""
import ast
import hashlib
import importlib.util
import json
from pathlib import Path
import re
import sys


def load(path, name):
    specification = importlib.util.spec_from_file_location(name, path)
    module = importlib.util.module_from_spec(specification)
    specification.loader.exec_module(module)
    return module


def check(repository):
    root = Path(repository).resolve()
    runner = root / 'tools/Native/Erlang.NifOrdinaryLoaderObservations/run.py'
    tree = ast.parse(runner.read_bytes(), filename=str(runner))
    main = next(node for node in tree.body if isinstance(node, ast.FunctionDef) and node.name == 'main')
    setup = next(node for node in main.body if isinstance(node, ast.For)
                 and isinstance(node.target, ast.Name) and node.target.id == 'tool')
    imports = next(node for node in main.body if isinstance(node, ast.Import)
                   and any(alias.name == 'common' for alias in node.names))
    runner_module = load(runner, 'review498_ordinary_loader')
    assert runner_module.R == root
    namespace = vars(runner_module).copy()
    previous_path = sys.path[:]
    try:
        # Execute actual current statements, not copied search paths or mock modules.
        exec(compile(ast.Module(body=[setup, imports], type_ignores=[]), str(runner), 'exec'), namespace)
        modules = {}
        for name, category in [('common', 'Native'), ('owned_process', 'Distribution'),
                               ('source_inputs', 'Runtime'), ('passive_etf', 'Distribution')]:
            path = Path(namespace[name].__file__).resolve()
            assert path.is_relative_to(root / 'tools' / category), path
            modules[name] = path.relative_to(root).as_posix()
    finally:
        sys.path[:] = previous_path
    preparation = root / 'tools/Native/Erlang.NifLinuxCallbackObservations/prepare.py'
    preparation_module = load(preparation, 'review498_linux_preparation')
    assert preparation_module.R == root
    tree = ast.parse(preparation.read_bytes(), filename=str(preparation))
    expression = next(node.value for node in ast.walk(tree) if isinstance(node, ast.Assign)
                      and any(isinstance(target, ast.Name) and target.id == 'tool' for target in node.targets))
    workflow = (root / '.github/workflows/nif-linux-callback17.yml').read_text()
    commands = re.findall(r'python3 -B "([^"]*\$tool/(?:build-native|collect)\.py)"', workflow)
    assert len(commands) == 2, commands
    observers = []
    for name in ['Erlang.NifMapSizeOriginalObservations', 'Erlang.NifMakeReferenceOriginalObservations']:
        directory = eval(compile(ast.Expression(expression), str(preparation), 'eval'),
                         dict(vars(preparation_module), toolname=name))
        assert directory.is_relative_to(root / 'tools/Native'), directory
        for file in ['manifest.json', 'cases.json']:
            json.loads((directory / file).read_bytes())
        for command in commands:
            assert (root / command.replace('$tool', name)).is_file(), command
        observers.append(directory.relative_to(root).as_posix())
    files = [runner, preparation, root / '.github/workflows/nif-linux-callback17.yml',
             Path(__file__).resolve(), *[root / path for path in modules.values()]]
    return {'Passed': True, 'Platform': sys.platform,
            'SourceBindings': {path.relative_to(root).as_posix(): hashlib.sha256(path.read_bytes()).hexdigest().upper()
                               for path in files},
            'ImportedModules': modules, 'PreparedObservers': observers,
            'WorkflowCommandsResolved': len(commands) * len(observers),
            'NativeExecutionClaimed': False}


if __name__ == '__main__':
    result = check(Path(__file__).resolve().parents[3])
    print(json.dumps(result, indent=2))
