#!/usr/bin/env python3
"""Builds the user guide website from Docs/ with Zensical (pip install zensical).

  python scripts/build-docs-site.py            # build into .site-build/site
  python scripts/build-docs-site.py --serve    # build and preview at http://localhost:8000
  python scripts/build-docs-site.py --ref recast-v0.16.0-beta

The pages are copied to .site-build/Docs first: links out of Docs/ (the changelog, the original guide in
legacy/Docs) become links to those files on GitHub at --ref, and pages that aren't part of the guide are left out.
"""
import argparse
import pathlib
import re
import shutil
import subprocess
import sys

ROOT = pathlib.Path(__file__).resolve().parent.parent
REPO = "https://github.com/leeoades/trizbort/blob"
NOT_IN_GUIDE = {"manual-test-checklist.md"}


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--ref", default="master", help="the branch or tag links out of the guide point at")
    parser.add_argument("--serve", action="store_true", help="preview the site after building it")
    args = parser.parse_args()

    build = ROOT / ".site-build"
    pages = build / "Docs"
    shutil.rmtree(build, ignore_errors=True)
    shutil.copytree(ROOT / "Docs", pages)
    for name in NOT_IN_GUIDE:
        (pages / name).unlink(missing_ok=True)

    outside = re.compile(r"\]\(\.\./([^)\s]+)\)")
    for page in pages.glob("*.md"):
        text = page.read_text(encoding="utf-8")
        page.write_text(outside.sub(lambda m: f"]({REPO}/{args.ref}/{m.group(1)})", text), encoding="utf-8")

    command = ["zensical", "serve" if args.serve else "build", "-f", str(ROOT / "zensical.yml")]
    return subprocess.call(command, cwd=ROOT)


if __name__ == "__main__":
    sys.exit(main())
