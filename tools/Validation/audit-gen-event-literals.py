"""Enforce semantic literal ownership in gen_event production and host checks."""
import argparse
import json
from pathlib import Path
import re

SOURCE = Path("src/Otp/Managed/Erlang.Otp.Stdlib/Processes/GenEvent")
VALIDATOR = Path("tools/Validation/Erlang.GenEventCompatibility")
LOGIC = {
    "GenEventModule.cs", "GenEventModule.Handlers.cs", "GenEventModule.Logging.cs",
    "GenEventModule.Loop.cs", "GenEventModule.System.cs", "GenEventHandler.cs", "GenEventSource.cs",
}
HOST_LOGIC = {"Program.cs", "GenEventBoot.cs"}
STRING = re.compile(r'"(?:\\.|[^"\\])*"')
COMMENTS = re.compile(r"//[^\n]*|/\*.*?\*/", re.S)
NUMBER = re.compile(r"(?<![\w])\d+(?![\w])")
# Small literal arities, tuple positions and loop/empty-collection identities
# remain structural syntax. Time/resource limits and source lines must be named.
MAXIMUM_STRUCTURAL_NUMBER = 7


def audit(repository):
    rows = []
    failures = []
    for directory, names in ((SOURCE, LOGIC), (VALIDATOR, HOST_LOGIC)):
        for name in sorted(names):
            path = repository / directory / name
            source = COMMENTS.sub("", path.read_text(encoding="utf-8-sig"))
            strings = STRING.findall(source)
            numbers = [int(value) for value in NUMBER.findall(STRING.sub("", source))]
            row = {"File": str(directory / name).replace("\\", "/"),
                   "InlineStrings": strings,
                   "UnownedNumbers": [n for n in numbers if n > MAXIMUM_STRUCTURAL_NUMBER]}
            rows.append(row)
            if row["InlineStrings"] or row["UnownedNumbers"]:
                failures.append(row)
    return {"Files": rows, "Failures": failures,
            "Scope": "Semantic strings/source lines/limits/deadlines; structural tuple arities/indices retained."}


def main():
    parser = argparse.ArgumentParser(description=__doc__)
    parser.add_argument("repository", type=Path)
    parser.add_argument("--output", type=Path)
    arguments = parser.parse_args()
    result = audit(arguments.repository.resolve())
    encoded = json.dumps(result, ensure_ascii=False, indent=2)
    if arguments.output:
        arguments.output.write_text(encoded + "\n", encoding="utf-8")
    print(encoded)
    return int(bool(result["Failures"]))


if __name__ == "__main__":
    raise SystemExit(main())
