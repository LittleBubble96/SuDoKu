# 奖杯源图补全

方式：内置 image_gen。基于已审核奖杯馆造型，将未获得预览补齐为彩色，移除 UI 底板、文字和状态角标，输出透明3列4行源图，再分别裁切。

```text
Use case: precise-object-edit / production sprite sheet / background extraction.
Edit the supplied TWELVE ZODIAC TROPHIES from the existing approved trophy gallery. Produce ONE clean 3-COLUMN x 4-ROW sprite sheet of those SAME twelve trophy designs, ALL in their FULL-COLOR EARNED appearance, on a genuinely TRANSPARENT RGBA background. This is source artwork to slice into twelve separate Unity PNG sprites, not a UI concept page.
Keep each trophy's silhouette, bowl/stem/pedestal, zodiac emblem, star pattern and handle designs as faithful to its matching approved reference card as possible. Recolor the gray/unearned cup previews using the SAME cobalt-blue enamel bowl, warm gold trim/handles, and silver-white pedestal family as the three colored earned cups. Do not invent new cup shapes. Match the approved fine illustrated game-render style, not photography.
The ONLY subjects are the TWELVE trophies. Completely REMOVE all UI cards, borders, labels, Chinese text, month numbers, year, completion states, badge circles, locks, checkmarks and navigation. Keep the small zodiac SYMBOL engraved on each pedestal as part of its original design. No other text or glyphs. No decorative surrounding stars or sparkles outside the cup. No background, no matte color, no checker pattern baked into pixels. Open spaces between handles and inside bow/scale structures must be transparent.
Use a REGULAR 3x4 layout on a portrait canvas approximately1536x2048, equal square cells, generous transparent margin inside every cell (at least10% on all four sides); no cup, horn, arrow, handle or shadow may touch any cell boundary. All twelve cups centered in their cell and roughly the same apparent height, clean pedestal baseline. Soft edge antialiasing; no ground plane, no cast background shadow.
Exact row/column ordering must match the source gallery:
ROW1: Capricorn goat-horn cup; Aquarius pouring-water handle cup; Pisces double-fish handle cup.
ROW2: Aries curled ram-horn cup; Taurus broad bull-horn cup; Gemini two-face/twin handle cup.
ROW3: Cancer crab-claw cup; Leo lion-mane medallion cup; Virgo wheat-ear handle cup.
ROW4: Libra hanging-balance-scale-pan cup; Scorpio curved segmented scorpion-tail cup; Sagittarius arrow-crossing cup.
Check that ALL TWELVE are full blue/gold/silver color, including Capricorn, Aquarius, Aries, Taurus, Gemini, Leo, Libra, Scorpio, Sagittarius. No remaining gray trophy and no duplicate cups. Transparent background is mandatory.
```
