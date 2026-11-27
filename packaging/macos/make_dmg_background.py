"""Draws the background of the Mac disk image: a light, gridded page (like Recast's map) with an arrow from the app
to Applications, and a line saying what to do. Run it to redraw dmg-background.png and dmg-background@2x.png
(Lee, 27 Nov 2026). The icons sit at (170, 190) and (490, 190) in a 660 x 400 window: make-dmg.sh puts them there."""
from pathlib import Path
from PIL import Image, ImageDraw, ImageFont

HERE = Path(__file__).parent
FONT = "/usr/share/fonts/truetype/dejavu/DejaVuSans.ttf"


def draw(scale: int) -> Image.Image:
    w, h = 660 * scale, 400 * scale
    image = Image.new("RGB", (w, h), (250, 250, 248))
    d = ImageDraw.Draw(image)
    grid = 32 * scale
    for x in range(0, w, grid):
        d.line([(x, 0), (x, h)], fill=(234, 234, 230), width=scale)
    for y in range(0, h, grid):
        d.line([(0, y), (w, y)], fill=(234, 234, 230), width=scale)

    # a join between the two "rooms", ending in an arrowhead, as Recast draws one
    teal = (38, 110, 118)
    y = 190 * scale
    d.line([(250 * scale, y), (400 * scale, y)], fill=teal, width=4 * scale)
    d.polygon([(412 * scale, y), (392 * scale, y - 11 * scale), (392 * scale, y + 11 * scale)], fill=teal)

    font = ImageFont.truetype(FONT, 17 * scale)
    text = "Drag Trizbort Recast to Applications"
    box = d.textbbox((0, 0), text, font=font)
    d.text(((w - (box[2] - box[0])) / 2, 318 * scale), text, font=font, fill=(70, 70, 70))
    return image


draw(1).save(HERE / "dmg-background.png")
draw(2).save(HERE / "dmg-background@2x.png")
