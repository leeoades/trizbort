#!/usr/bin/env python3
"""Compare legacy and new Trizbort export output.

Usage: compare_exports.py <output-root>
Reads <root>/legacy/<map>/ and <root>/new/<map>/, writes <root>/report/index.html (+ images, diffs).
Requires Pillow and numpy; pypdfium2 (PDF rasterising) and pypdf (PDF facts) are optional.
"""
import difflib
import html
import os
import re
import sys

import numpy as np
from PIL import Image, ImageFilter

try:
    import pypdfium2
except Exception:
    pypdfium2 = None
try:
    import pypdf
except Exception:
    pypdf = None

BLUR_RADIUS = 2
THRESHOLD = 48
ASPECT_TOLERANCE = 0.02
MAX_SIDE_BY_SIDE = 1600
PDF_RASTER_WIDTH = 1600
LANGUAGES = ["inform7", "inform6", "tads", "hugo", "zil", "quest", "alan", "adventuron"]


def load_rgb(path):
    img = Image.open(path)
    img.load()
    img = img.convert("RGBA")
    bg = Image.new("RGBA", img.size, (255, 255, 255, 255))
    return Image.alpha_composite(bg, img).convert("RGB")


def compare_images(legacy, new, out_prefix):
    """Returns a result dict; writes <prefix>-heat.png and <prefix>-side.png."""
    res = {
        "legacy_size": legacy.size,
        "new_size": new.size,
    }
    la = legacy.size[0] / max(1, legacy.size[1])
    na = new.size[0] / max(1, new.size[1])
    res["legacy_aspect"] = la
    res["new_aspect"] = na
    res["aspect_flag"] = abs(na - la) / la > ASPECT_TOLERANCE
    new_r = new.resize(legacy.size, Image.LANCZOS)
    a = np.asarray(legacy.filter(ImageFilter.GaussianBlur(BLUR_RADIUS)), dtype=np.int16)
    b = np.asarray(new_r.filter(ImageFilter.GaussianBlur(BLUR_RADIUS)), dtype=np.int16)
    over = (np.abs(a - b) > THRESHOLD).any(axis=2)
    res["score"] = float(over.mean() * 100.0)

    faded = (np.asarray(legacy, dtype=np.float32) * 0.35 + 255 * 0.65).astype(np.uint8)
    heat = faded.copy()
    heat[over] = (255, 0, 0)
    heat_img = Image.fromarray(heat, "RGB")
    heat_img.save(out_prefix + "-heat.png")

    side = Image.new("RGB", (legacy.size[0] * 3 + 20, legacy.size[1]), (128, 128, 128))
    side.paste(legacy, (0, 0))
    side.paste(new_r, (legacy.size[0] + 10, 0))
    side.paste(heat_img, (legacy.size[0] * 2 + 20, 0))
    if side.size[0] > MAX_SIDE_BY_SIDE:
        h = max(1, round(side.size[1] * MAX_SIDE_BY_SIDE / side.size[0]))
        side = side.resize((MAX_SIDE_BY_SIDE, h), Image.LANCZOS)
    side.save(out_prefix + "-side.png")
    return res


def pdf_facts(path):
    facts = {"pages": None, "size": None, "title": None, "author": None, "annots": None}
    if pypdf is not None:
        try:
            reader = pypdf.PdfReader(path)
            facts["pages"] = len(reader.pages)
            box = reader.pages[0].mediabox
            facts["size"] = (round(float(box.width), 1), round(float(box.height), 1))
            info = reader.metadata
            if info is not None:
                facts["title"] = info.title
                facts["author"] = info.author
            n = 0
            for page in reader.pages:
                for ref in page.get("/Annots") or []:
                    try:
                        if ref.get_object().get("/Subtype") == "/Text":
                            n += 1
                    except Exception:
                        pass
            facts["annots"] = n
            return facts
        except Exception as ex:
            facts["error"] = str(ex)
    try:
        with open(path, "rb") as f:
            data = f.read().decode("latin-1")
        facts["pages"] = len(re.findall(r"/Type\s*/Page(?![s\w])", data))
        m = re.search(r"/MediaBox\s*\[\s*([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)\s+([-\d.]+)", data)
        if m:
            x0, y0, x1, y1 = (float(v) for v in m.groups())
            facts["size"] = (round(x1 - x0, 1), round(y1 - y0, 1))
        t = re.search(r"/Title\s*\(([^)]*)\)", data)
        a = re.search(r"/Author\s*\(([^)]*)\)", data)
        facts["title"] = t.group(1) if t else None
        facts["author"] = a.group(1) if a else None
        facts["annots"] = len(re.findall(r"/Subtype\s*/Text\b", data))
    except Exception as ex:
        facts["error"] = str(ex)
    return facts


def rasterise_pdf(path):
    pdf = pypdfium2.PdfDocument(path)
    try:
        page = pdf[0]
        width = page.get_width()
        bitmap = page.render(scale=PDF_RASTER_WIDTH / max(1.0, width))
        return bitmap.to_pil().convert("RGB")
    finally:
        pdf.close()


_NOISE = re.compile(r"trizbort.*(\d+\.\d+|\d{4}[-/]\d{2}[-/]\d{2}|\d{1,2}[-/]\d{1,2}[-/]\d{2,4})", re.I)


def normalise_code(text):
    lines = []
    for line in text.replace("\r\n", "\n").replace("\r", "\n").split("\n"):
        line = line.rstrip()
        if _NOISE.search(line):
            continue
        lines.append(line)
    return lines


def compare_code(legacy_path, new_path, diff_path):
    with open(legacy_path, encoding="utf-8-sig", errors="replace") as f:
        a = normalise_code(f.read())
    with open(new_path, encoding="utf-8-sig", errors="replace") as f:
        b = normalise_code(f.read())
    diff = list(difflib.unified_diff(a, b, "legacy", "new", lineterm=""))
    changed = sum(1 for d in diff if (d.startswith("+") or d.startswith("-"))
                  and not d.startswith("+++") and not d.startswith("---"))
    if diff:
        with open(diff_path, "w", encoding="utf-8", newline="\n") as f:
            f.write("\n".join(diff) + "\n")
    return changed if diff else 0, bool(diff)


def read_text(path):
    try:
        with open(path, encoding="utf-8", errors="replace") as f:
            return f.read().strip()
    except OSError:
        return ""


def process_map(root, name, report_dir):
    ldir = os.path.join(root, "legacy", name)
    ndir = os.path.join(root, "new", name)
    out = {"name": name, "notes": []}
    safe = re.sub(r"[^A-Za-z0-9._-]", "_", name)
    for side, d in (("legacy", ldir), ("new", ndir)):
        if not os.path.isdir(d):
            out["notes"].append("missing %s folder" % side)
        else:
            err = read_text(os.path.join(d, "errors.txt"))
            if err:
                out["notes"].append("%s errors: %s" % (side, err.splitlines()[0][:160]))

    # PNG
    lp, np_ = os.path.join(ldir, "map.png"), os.path.join(ndir, "map.png")
    if os.path.isfile(lp) and os.path.isfile(np_):
        try:
            out["image"] = compare_images(load_rgb(lp), load_rgb(np_), os.path.join(report_dir, safe + "-png"))
            out["image"]["prefix"] = safe + "-png"
        except Exception as ex:
            out["notes"].append("png compare failed: %s" % ex)
    else:
        out["notes"].append("png missing on %s" % ("both sides" if not (os.path.isfile(lp) or os.path.isfile(np_))
                                                     else ("legacy" if not os.path.isfile(lp) else "new")))

    # PDF
    lf, nf = os.path.join(ldir, "map.pdf"), os.path.join(ndir, "map.pdf")
    if os.path.isfile(lf) and os.path.isfile(nf):
        out["pdf"] = {"legacy": pdf_facts(lf), "new": pdf_facts(nf)}
        if pypdfium2 is not None:
            try:
                out["pdf"]["image"] = compare_images(rasterise_pdf(lf), rasterise_pdf(nf),
                                                     os.path.join(report_dir, safe + "-pdf"))
                out["pdf"]["image"]["prefix"] = safe + "-pdf"
            except Exception as ex:
                out["notes"].append("pdf raster failed: %s" % ex)
    else:
        out["notes"].append("pdf missing on %s" % ("both sides" if not (os.path.isfile(lf) or os.path.isfile(nf))
                                                     else ("legacy" if not os.path.isfile(lf) else "new")))

    # Code
    out["code"] = {}
    for lang in LANGUAGES:
        lt, nt = os.path.join(ldir, lang + ".txt"), os.path.join(ndir, lang + ".txt")
        if os.path.isfile(lt) and os.path.isfile(nt):
            diff_name = "%s-%s.diff" % (safe, lang)
            changed, has = compare_code(lt, nt, os.path.join(report_dir, diff_name))
            out["code"][lang] = (changed, diff_name if has else None)
        else:
            out["code"][lang] = (None, None)
    return out


def worst_score(m):
    scores = [m["image"]["score"]] if "image" in m else []
    if "pdf" in m and "image" in m["pdf"]:
        scores.append(m["pdf"]["image"]["score"])
    return max(scores) if scores else -1.0


def fmt_size(s):
    return "%dx%d" % s


def fmt_facts(f):
    if f.get("pages") is None and "error" in f:
        return "error: " + html.escape(f["error"])
    size = "%gx%g pt" % f["size"] if f.get("size") else "?"
    return "%s page(s), %s, title=%s, author=%s, annots=%s" % (
        f.get("pages"), size, html.escape(str(f.get("title"))), html.escape(str(f.get("author"))), f.get("annots"))


def image_cell(res):
    if not res:
        return "<td>-</td>"
    p = res["prefix"]
    flag = " <b style='color:#c00'>ASPECT</b>" if res["aspect_flag"] else ""
    return ("<td><b>%.2f%%</b>%s<br>legacy %s (%.3f)<br>new %s (%.3f)<br>"
            "<a href='%s-side.png'><img src='%s-side.png' width='320'></a><br>"
            "<a href='%s-heat.png'>heat-map</a></td>") % (
        res["score"], flag, fmt_size(res["legacy_size"]), res["legacy_aspect"],
        fmt_size(res["new_size"]), res["new_aspect"], p, p, p)


def write_report(root, results, report_dir):
    scored = [worst_score(m) for m in results if worst_score(m) >= 0]
    flagged = [m["name"] for m in results
               if ("image" in m and m["image"]["aspect_flag"])
               or ("pdf" in m and "image" in m["pdf"] and m["pdf"]["image"]["aspect_flag"])]
    code_diffs = sum(1 for m in results for c, _ in m["code"].values() if c)
    parts = ["<!DOCTYPE html><html><head><meta charset='utf-8'><title>Trizbort export comparison</title>",
             "<style>body{font-family:sans-serif}table{border-collapse:collapse}td,th{border:1px solid #bbb;"
             "padding:4px;vertical-align:top;font-size:12px}</style></head><body>",
             "<h1>Trizbort legacy vs new export comparison</h1>",
             "<p>Maps: %d. Image score = %% of pixels differing by more than %d after blur radius %d "
             "(NEW resized to legacy size).</p>" % (len(results), THRESHOLD, BLUR_RADIUS)]
    if scored:
        parts.append("<p>Mean worst image score: %.2f%%. Worst: %.2f%% (%s).</p>" % (
            sum(scored) / len(scored), max(scored),
            html.escape(max(results, key=worst_score)["name"])))
    parts.append("<p>Aspect ratio differs by more than 2%%: %s</p>" % (html.escape(", ".join(flagged)) or "none"))
    parts.append("<p>Code export files with differences: %d</p>" % code_diffs)
    if pypdfium2 is None:
        parts.append("<p><i>pypdfium2 not installed: PDF pages were not rasterised or scored.</i></p>")
    if pypdf is None:
        parts.append("<p><i>pypdf not installed: PDF facts come from regex and may be incomplete.</i></p>")
    parts.append("<table><tr><th>Map</th><th>PNG</th><th>PDF image</th><th>PDF facts</th>"
                 "<th>Code changed lines</th><th>Notes</th></tr>")
    for m in sorted(results, key=lambda r: (-worst_score(r), r["name"])):
        pdf = m.get("pdf")
        facts = ("legacy: %s<br>new: %s" % (fmt_facts(pdf["legacy"]), fmt_facts(pdf["new"]))) if pdf else "-"
        code = []
        for lang in LANGUAGES:
            c, d = m["code"][lang]
            if c is None:
                code.append("%s: n/a" % lang)
            elif d:
                code.append("%s: <a href='%s'>%d</a>" % (lang, d, c))
            else:
                code.append("%s: 0" % lang)
        parts.append("<tr><td>%s</td>%s%s<td>%s</td><td>%s</td><td>%s</td></tr>" % (
            html.escape(m["name"]), image_cell(m.get("image")),
            image_cell(pdf.get("image") if pdf else None), facts, "<br>".join(code),
            html.escape("; ".join(m["notes"]))))
    parts.append("</table></body></html>")
    path = os.path.join(report_dir, "index.html")
    with open(path, "w", encoding="utf-8", newline="\n") as f:
        f.write("\n".join(parts))
    return path


def main(argv):
    if len(argv) != 2:
        print(__doc__)
        return 2
    root = argv[1]
    names = set()
    for side in ("legacy", "new"):
        d = os.path.join(root, side)
        if os.path.isdir(d):
            names.update(n for n in os.listdir(d) if os.path.isdir(os.path.join(d, n)))
    if not names:
        print("No map folders found under %s/legacy or %s/new" % (root, root))
        return 1
    report_dir = os.path.join(root, "report")
    os.makedirs(report_dir, exist_ok=True)
    results = []
    for name in sorted(names):
        print("comparing", name)
        results.append(process_map(root, name, report_dir))
    print("wrote", write_report(root, results, report_dir))
    return 0


if __name__ == "__main__":
    sys.exit(main(sys.argv))
