# Typed projections, selected call lifetimes and range refinements

The initial linked source build found invalid constant patterns against BigInteger operands. The next compiled retake passed 92 of 94 focused checks, retaining the full-module byte fixture. Two fixture failures came from an incorrect synthetic label count and an unsupported managed external test call. Both original logs are retained, and Part 1900 contains the corrected checks. No whole release or repository acceptance is inferred from these attempts.
