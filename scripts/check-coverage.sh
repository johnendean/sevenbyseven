#!/usr/bin/env bash
#
# Fails if line coverage is below the threshold. Run it after `dotnet test`:
#
#   dotnet test --settings coverlet.runsettings --collect "XPlat Code Coverage" \
#     --results-directory TestResults
#   ./scripts/check-coverage.sh
#
# CI runs exactly this, so a red gate is reproducible locally before you push.
# What is measured, and what is left out of it, is decided in coverlet.runsettings.
#
# Usage: check-coverage.sh [results-directory]
#        COVERAGE_THRESHOLD=85 ./scripts/check-coverage.sh
set -euo pipefail

threshold=${COVERAGE_THRESHOLD:-80}
results=${1:-TestResults}

# Coverlet writes the report into a folder named for a GUID, so the path can only be a
# glob. Take the newest: a results directory that was not cleaned holds every earlier run
# too, and the stale ones would answer the wrong question.
report=$(find "$results" -name 'coverage.cobertura.xml' -type f -print0 2>/dev/null \
    | xargs -0 ls -t 2>/dev/null \
    | head -n 1 || true)

if [ -z "$report" ]; then
    echo "No coverage.cobertura.xml under $results/." >&2
    echo "Did the test run collect coverage? A report we did not produce is our problem." >&2
    exit 1
fi

# The root <coverage> element carries the totals, and it is the first element in the
# file, so the first match of each attribute is the overall figure.
attribute() {
    grep -m 1 -o "$1=\"[0-9.]*\"" "$report" | grep -o '[0-9.]*' || true
}

covered=$(attribute lines-covered)
valid=$(attribute lines-valid)
rate=$(attribute line-rate)

if [ -z "${rate:-}" ] || [ -z "${valid:-}" ] || [ "$valid" = "0" ]; then
    echo "Could not read a line rate out of $report." >&2
    exit 1
fi

percent=$(awk -v r="$rate" 'BEGIN { printf "%.1f", r * 100 }')
short=$(awk -v v="$valid" -v t="$threshold" -v c="$covered" \
    'BEGIN { n = (v * t / 100) - c; printf "%d", (n > 0 && n != int(n)) ? int(n) + 1 : n }')

echo "Line coverage: ${percent}% (${covered} of ${valid} lines), threshold ${threshold}%"
echo "Report: $report"

if awk -v r="$rate" -v t="$threshold" 'BEGIN { exit !(r * 100 + 0.05 < t) }'; then
    echo
    echo "Coverage is below the ${threshold}% threshold: ${short} more covered lines are needed." >&2
    exit 1
fi
