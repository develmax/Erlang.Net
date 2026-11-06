# erpc publication validation

See the [review follow-up](review/README.md) for the current validation.

The isolated checkout was based on main at `8dccf8f8193e50ef3f2dff964c67b989706bf38d`. Both backends matched OTP 29.1.1 in 330 observations each at backtrace depths 0, 3, 8 and 32. Each depth directory contains the raw corpus, source and capture digests, and backend reports. The build, style check and eight connected checks passed. The connected checks used the test assembly built in this checkout.

The source admissions verify the descriptor production tree, the current source and evidence bindings, and the native source plans. The hash refresh report records manifest and workflow pin changes. The pending native source plan also includes previously omitted source members already present on main. These are admission checks for those workflows. Native VM runs and full local regression were outside the local validation scope.

The [backend guide](../../erpc-backends.md) describes reproduction and the remaining compatibility scope.
