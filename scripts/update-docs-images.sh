#!/usr/bin/env bash
# Draws the user guide's pictures (Docs/images) from the app itself: the Lighthouse scenes (DocsCaptureTests), the
# dialogs (DialogCaptureTests) and the token (HereTokenTests), then copies the ones listed in docs-images.txt.
# Re-run it before a release, and look at what changed: git diff --stat Docs/images
set -euo pipefail
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
ROOT="$(cd "${SCRIPT_DIR}/.." && pwd)"
CAPTURE="$(mktemp -d -t trizbort-docs-XXXXXX)"
trap 'rm -rf "${CAPTURE}"' EXIT

TRIZBORT_DOCS_CAPTURE="${CAPTURE}" TRIZBORT_CAPTURE_OUTPUT="${CAPTURE}" \
  dotnet test "${ROOT}/src/Trizbort.Avalonia.Tests/Trizbort.Avalonia.Tests.csproj" --nologo \
  --filter 'TestCategory=Docs|FullyQualifiedName~DialogCaptureTests|FullyQualifiedName~HereTokenTests.CaptureTheTokenOnTheMap'

mkdir -p "${ROOT}/Docs/images"
missing=()
while read -r picture from; do
  [[ -z "${picture}" || "${picture}" == \#* ]] && continue
  if [[ -f "${CAPTURE}/${from}" ]]; then cp "${CAPTURE}/${from}" "${ROOT}/Docs/images/${picture}"; else missing+=("${from}"); fi
done < "${SCRIPT_DIR}/docs-images.txt"
if (( ${#missing[@]} )); then echo "Not captured: ${missing[*]}" >&2; exit 1; fi
echo "Docs/images updated"
