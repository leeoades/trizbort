#!/usr/bin/env bash
set -eo pipefail

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
REPO_ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
TRX_FILE="${REPO_ROOT}/TestResults/core_tests.trx"

mkdir -p "${REPO_ROOT}/TestResults"
rm -f "${TRX_FILE}"

DOTNET_CMD="/home/lee/.dotnet/dotnet"
if ! command -v "${DOTNET_CMD}" &> /dev/null; then
  DOTNET_CMD="dotnet"
fi

echo "Running Trizbort.Core.Tests..."
set +e
"${DOTNET_CMD}" test "${REPO_ROOT}/src/Trizbort.Core.Tests/Trizbort.Core.Tests.csproj" \
  --logger "trx;LogFileName=${TRX_FILE}" \
  --nologo \
  -v quiet \
  -- RunConfiguration.TestSessionTimeout=60000 \
  NUnit.DefaultTimeout=20000

TEST_EXIT_CODE=$?
set -e

if [ ! -f "${TRX_FILE}" ]; then
  echo "Error: TRX result file not produced."
  exit 1
fi

python3 - <<EOF
import xml.etree.ElementTree as ET
import sys

tree = ET.parse("${TRX_FILE}")
root = tree.getroot()

ns = {'ns': 'http://microsoft.com/schemas/VisualStudio/TeamTest/2010'}

counters = root.find('.//ns:Counters', ns)
if counters is None:
    counters = root.find('.//Counters')

if counters is not None:
    total = counters.attrib.get('total', '0')
    executed = counters.attrib.get('executed', '0')
    passed = counters.attrib.get('passed', '0')
    failed = counters.attrib.get('failed', '0')
    error = counters.attrib.get('error', '0')
    skipped = str(int(counters.attrib.get('notExecuted', '0')) + int(counters.attrib.get('inconclusive', '0')))
    print(f"Results: Total={total}, Executed={executed}, Passed={passed}, Failed={failed}, Skipped={skipped}")
    
    unit_test_results = root.findall('.//ns:UnitTestResult', ns)
    if not unit_test_results:
        unit_test_results = root.findall('.//UnitTestResult')
        
    failures = []
    for r in unit_test_results:
        outcome = r.attrib.get('outcome', '')
        if outcome in ('Failed', 'Error'):
            name = r.attrib.get('testName', 'Unknown')
            msg_el = r.find('.//ns:Message', ns)
            if msg_el is None:
                msg_el = r.find('.//Message')
            msg = msg_el.text.strip() if msg_el is not None and msg_el.text else "No error message"
            failures.append((name, msg))
            
    if failures:
        print(f"\nFAILED TESTS ({len(failures)}):")
        for name, msg in failures:
            print(f"  - {name}: {msg}")
        sys.exit(1)
    else:
        print("All tests PASSED.")
        sys.exit(0)
else:
    print("Could not find test counters in TRX.")
    sys.exit(1)
EOF
