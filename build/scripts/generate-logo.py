"""Generates the Commit Dock 'Anchor' logo: SVG sources plus every icon file the app and installers use.

Usage (from the repository root):
    pip install resvg-py pillow
    python build/scripts/generate-logo.py

Writes src/App.ico, build/resources/app/App.icns, build/resources/_common/icons/commitdock.png
and the files in build/logo/.
"""
import io
import os
import sys

HERE = os.path.dirname(os.path.abspath(__file__))
import resvg_py  # noqa: E402
from PIL import Image  # noqa: E402

DEFS = '''
  <defs>
    <linearGradient id="bg" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#3A2F96"/>
      <stop offset="0.55" stop-color="#211B5C"/>
      <stop offset="1" stop-color="#110E2C"/>
    </linearGradient>
    <radialGradient id="glow" cx="0.5" cy="0.32" r="0.6">
      <stop offset="0" stop-color="#8B7BFF" stop-opacity="0.35"/>
      <stop offset="1" stop-color="#8B7BFF" stop-opacity="0"/>
    </radialGradient>
    <linearGradient id="violet" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#B4A8FF"/>
      <stop offset="1" stop-color="#6A57F0"/>
    </linearGradient>
    <linearGradient id="teal" x1="0" y1="0" x2="1" y2="1">
      <stop offset="0" stop-color="#6EF0DC"/>
      <stop offset="1" stop-color="#14B8A6"/>
    </linearGradient>
    <filter id="shadow" x="-20%" y="-20%" width="140%" height="140%">
      <feDropShadow dx="0" dy="14" stdDeviation="18" flood-color="#05040F" flood-opacity="0.45"/>
    </filter>
  </defs>
'''

TILE = '''
  <rect x="64" y="64" width="896" height="896" rx="208" fill="url(#bg)"/>
  <rect x="64" y="64" width="896" height="896" rx="208" fill="url(#glow)"/>
  <rect x="65.5" y="65.5" width="893" height="893" rx="206.5" fill="none" stroke="#FFFFFF" stroke-opacity="0.10" stroke-width="3"/>
'''

# Detailed glyph for 48px and up. Ring = commit, shank = main line with a commit,
# arms = two branches ending in commits.
GLYPH_LARGE = '''
  <g transform="translate(0 38)" filter="url(#shadow)">
    <g fill="none" stroke-linecap="round" stroke-linejoin="round">
      <path d="M512 340 V748" stroke="#F4F2FF" stroke-width="52"/>
      <path d="M388 436 H636" stroke="#F4F2FF" stroke-width="46"/>
      <path d="M512 748 C428 748 334 706 298 606" stroke="url(#teal)" stroke-width="52"/>
      <path d="M512 748 C596 748 690 706 726 606" stroke="url(#violet)" stroke-width="52"/>
    </g>
    <circle cx="512" cy="262" r="72" fill="none" stroke="#F4F2FF" stroke-width="46"/>
    <circle cx="298" cy="584" r="58" fill="url(#teal)" stroke="#F4F2FF" stroke-width="22"/>
    <circle cx="726" cy="584" r="58" fill="url(#violet)" stroke="#F4F2FF" stroke-width="22"/>
    <circle cx="512" cy="566" r="40" fill="#2A2372" stroke="#F4F2FF" stroke-width="22"/>
  </g>
'''

# Heavier, simplified glyph that stays crisp at 16-32px.
GLYPH_SMALL = '''
  <g transform="translate(0 30)">
    <g fill="none" stroke-linecap="round" stroke-linejoin="round">
      <path d="M512 360 V740" stroke="#F4F2FF" stroke-width="84"/>
      <path d="M372 452 H652" stroke="#F4F2FF" stroke-width="76"/>
      <path d="M512 740 C420 740 318 700 282 600" stroke="#2DD4BF" stroke-width="84"/>
      <path d="M512 740 C604 740 706 700 742 600" stroke="#8B7BFF" stroke-width="84"/>
    </g>
    <circle cx="512" cy="262" r="84" fill="none" stroke="#F4F2FF" stroke-width="72"/>
    <circle cx="282" cy="574" r="78" fill="#2DD4BF"/>
    <circle cx="742" cy="574" r="78" fill="#8B7BFF"/>
  </g>
'''

# Glyph only (no tile), for use on top of other backgrounds, e.g. the README header.
GLYPH_MARK = GLYPH_LARGE.replace('<g transform="translate(0 38)" filter="url(#shadow)">', '<g transform="translate(0 38)">')


def svg(body, tile=True):
    return ('<svg xmlns="http://www.w3.org/2000/svg" viewBox="0 0 1024 1024" width="1024" height="1024">'
            + DEFS + (TILE if tile else '') + body + '</svg>')


LARGE = svg(GLYPH_LARGE)
SMALL = svg(GLYPH_SMALL)


def render(svg_text, size):
    png = bytes(resvg_py.svg_to_bytes(svg_string=svg_text, width=size, height=size))
    return Image.open(io.BytesIO(png)).convert('RGBA')


def icon(size):
    return render(SMALL if size <= 32 else LARGE, size)


def main(out_root):
    out = lambda *p: os.path.join(out_root, *p)  # noqa: E731
    os.makedirs(out('build', 'logo'), exist_ok=True)

    open(out('build', 'logo', 'commitdock.svg'), 'w').write(LARGE)
    open(out('build', 'logo', 'commitdock-small.svg'), 'w').write(SMALL)

    # Windows: multi-size .ico (16..256), the app and installer icon.
    sizes = [16, 20, 24, 32, 40, 48, 64, 128, 256]
    frames = [icon(s) for s in sizes]
    frames[-1].save(out('src', 'App.ico'), format='ICO', sizes=[(s, s) for s in sizes], append_images=frames[:-1])

    # macOS: .icns generated from the 1024px artwork.
    render(LARGE, 1024).save(out('build', 'resources', 'app', 'App.icns'), format='ICNS')

    # Linux: desktop/AppImage icon.
    icon(256).save(out('build', 'resources', '_common', 'icons', 'commitdock.png'))

    # README / branding.
    icon(256).save(out('build', 'logo', 'commitdock-256.png'))
    print('written to', out_root)


if __name__ == '__main__':
    main(sys.argv[1] if len(sys.argv) > 1 else os.path.join(HERE, '..', '..'))
