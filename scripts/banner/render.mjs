// Renders a layered banner or thumbnail SVG (assets/banner.svg, assets/thumbnail.svg) as PNG frames:
// assets/logo-animated.svg drawing itself in the visible strip and the title fading in.
// Usage: node render.mjs <image.svg> <logo.svg> <outDir> [--layout right|left] [--static]
//          [--video clip.mp4 --start 22.3 --focus 75% --ease 2.4] [--title-at 0.35] [--fps 25] [--seconds 3.2]
// --layout shows the SVG's "Layout: Strip right" or "Layout: Strip left" layer; without it, the visible one.
// --static renders one frame of the SVG as it is, for the still exports.
// --video plays a clip behind the dark fade in place of the still background, from --start seconds,
// cropped to the background around --focus (a CSS object-position y). Over the last --ease seconds it slows
// smoothly to a stop, blending neighbouring clip frames so the slow part doesn't stutter.
// It renders in Edge, which can decode H.264.
// Then encode.py turns the frames into a WebP. build.ps1 makes every variant. See docs/development.md.
import { chromium } from 'playwright';
import fs from 'node:fs';
import path from 'node:path';
import { pathToFileURL } from 'node:url';
import { parseArgs } from 'node:util';

const { positionals, values } = parseArgs({
  allowPositionals: true,
  options: {
    layout: { type: 'string' },
    static: { type: 'boolean', default: false },
    video: { type: 'string' },
    start: { type: 'string', default: '0' },
    focus: { type: 'string', default: '50%' },
    ease: { type: 'string', default: '2.4' },
    'video-fps': { type: 'string', default: '29.97' },
    'title-at': { type: 'string' },
    fps: { type: 'string', default: '25' },
    seconds: { type: 'string' },
  },
});
const [svgPath, logoPath, outDir] = positionals;
const isStatic = values.static;
const useVideo = Boolean(values.video) && !isStatic;
const fps = Number(values.fps), start = Number(values.start), videoFps = Number(values['video-fps']);
const ease = useVideo ? Number(values.ease) : 0;
// With a video, the default length plays 3.2 seconds of clip whatever the slow-down's length, since
// the slow-down plays half its time's worth (see clipTime).
const seconds = isStatic ? 0 : Number(values.seconds ?? (useVideo ? 3.2 + ease / 2 : 3.2));
fs.mkdirSync(outDir, { recursive: true });

// Seconds of clip played by time t: full speed, then over the last `ease` seconds the speed falls
// along 1 - smoothstep to zero, which plays half that time's worth of clip and settles without a jolt.
function clipTime(t) {
  const easeFrom = seconds - ease;
  if (t <= easeFrom) return t;
  const u = Math.min((t - easeFrom) / ease, 1);
  return easeFrom + ease * (u - (u ** 3 - u ** 4 / 2));
}

const attr = (tag, name) => tag.match(new RegExp(`\\s${name}="([^"]*)"`))?.[1];

let svg = fs.readFileSync(svgPath, 'utf8');
const width = Number(attr(svg.match(/<svg[^>]*>/)[0], 'width'));
const height = Number(attr(svg.match(/<svg[^>]*>/)[0], 'height'));

// Show the chosen layout layer and hide the others. Everything after is looked up inside it.
const layoutTags = [...svg.matchAll(/<g id="layout-([^"]+)"[^>]*>/g)];
const layout = values.layout ?? layoutTags.findLast((m) => !m[0].includes('display:none'))?.[1];
for (const m of layoutTags) {
  const tag = m[0].replace(/\s*style="[^"]*"/, '');
  svg = svg.replace(m[0], m[1] === layout ? tag : tag.replace(/>$/, ' style="display:none">'));
}
const regionStart = layoutTags.length ? svg.indexOf(`<g id="layout-${layout}"`) : 0;
const nextLayout = svg.indexOf('<g id="layout-', regionStart + 1);
const regionEnd = nextLayout === -1 ? svg.length : nextLayout;
const findInRegion = (re) => {
  const m = re.exec(svg.slice(regionStart, regionEnd));
  return m && { tag: m[0], match: m, index: regionStart + m.index };
};

// The strip that shows is the last strip layer that isn't hidden. Its raster logo is replaced by
// the animated one, drawn in the strip's text colour.
const strips = [...svg.slice(regionStart, regionEnd).matchAll(/<g id="strip-[^"]*"[^>]*>/g)]
  .filter((m) => !m[0].includes('display:none'));
const strip = regionStart + strips[strips.length - 1].index;
const stripColour = svg.slice(strip).match(/<text[^>]* fill="([^"]+)"/)[1];
const logoStart = svg.indexOf('<image', strip);
const logoEnd = svg.indexOf('/>', logoStart) + 2;
const logoTag = svg.slice(logoStart, logoEnd);
const logoBox = ['x', 'y', 'width', 'height'].map((n) => attr(logoTag, n));
// Everything below is sized for the banner's 64px logo; this scales it to the thumbnail's.
const scale = Number(logoBox[2]) / 64;

let css = '';
if (!isStatic) {
  const logo = fs.readFileSync(logoPath, 'utf8');
  const logoStyle = logo.match(/<style>([\s\S]*?)<\/style>/)[1]
    .replace(/@media \(prefers-reduced-motion[\s\S]*$/, '')
    // At 64px wide the original 3-unit stroke is under half a pixel; thicken it so the draw shows.
    .replace('stroke-width:3', `stroke-width:${+(9 / scale).toFixed(2)}`);
  const logoBody = logo.slice(logo.indexOf('<g class="all">'), logo.lastIndexOf('</svg>'));
  const nested = `<svg x="${logoBox[0]}" y="${logoBox[1]}" width="${logoBox[2]}" height="${logoBox[3]}" viewBox="-6 -6 412.3 550" overflow="visible" color="${stripColour}">${logoBody}</svg>`;
  svg = svg.slice(0, logoStart) + nested + svg.slice(logoEnd);

  // The title fades in at --title-at seconds. With a video it defaults to the end, so the clip is seen first.
  const titleAt = Number(values['title-at'] ?? (useVideo ? seconds - 1.4 : 0.35));
  css = `${logoStyle}
[id^="title"] text,[id^="label"] text{opacity:0;animation:aa-in .9s cubic-bezier(.22,.61,.36,1) both}
[id^="title"] text:nth-of-type(1),[id^="label"] text{animation-delay:${titleAt}s}
[id^="title"] text:nth-of-type(2){animation-delay:${titleAt + 0.25}s}
@keyframes aa-in{from{opacity:0;transform:translateY(${Math.round(14 * scale)}px)}to{opacity:1;transform:none}}`;
}

// With a video, the still background is hidden and the video, placed where it was, shows through
// the SVG under the dark fade. Two copies of the clip: one on the clip frame before each moment and
// one on the frame after, faded in by how far between them the moment falls.
let video = '';
if (useVideo) {
  css += '[id^="background"]{display:none}';
  const bg = findInRegion(/<g id="background[^"]*"[\s\S]*?<image[^>]*>/);
  const image = bg.tag.slice(bg.tag.lastIndexOf('<image'));
  const [x, y, w, h] = ['x', 'y', 'width', 'height'].map((n) => attr(image, n));
  const tag = (id) => `<video id="${id}" muted preload="auto" src="${pathToFileURL(path.resolve(values.video)).href}"
      style="position:absolute;left:${x}px;top:${y}px;width:${w}px;height:${h}px;object-fit:cover;object-position:50% ${values.focus}"></video>`;
  video = tag('bg') + tag('bg-next');
}
svg = svg.replace('</style>', `</style><style>${css}</style>`);

const html = `<!doctype html><html><head><style>html,body{margin:0;background:#000}svg{position:absolute;left:0;top:0}</style></head>
<body>${video}${svg}</body></html>`;
const htmlPath = path.resolve(outDir, 'page.html');
fs.writeFileSync(htmlPath, html);

const browser = await chromium.launch(useVideo ? { channel: 'msedge' } : {});
const page = await browser.newPage({ viewport: { width, height } });
await page.goto(pathToFileURL(htmlPath).href);
await page.evaluate(() => document.fonts.ready);
const inter = await page.evaluate(() => document.fonts.check('600 132px Inter'));
if (!inter) throw new Error('Inter did not load, so the text would render in a fallback font');
if (useVideo) {
  await page.evaluate(() => Promise.all(['bg', 'bg-next'].map((id) => new Promise((resolve, reject) => {
    const v = document.getElementById(id);
    if (v.readyState >= 1) resolve();
    v.onloadedmetadata = resolve;
    v.onerror = () => reject(new Error('video failed to load'));
  }))));
}

const frames = Math.round(seconds * fps) + 1;
for (let i = 0; i < frames; i++) {
  const t = i / fps;
  // Clip frame numbers either side of this moment; seeking to a frame's middle lands on it reliably.
  const clipFrame = (start + clipTime(t)) * videoFps;
  const before = Math.floor(clipFrame);
  await page.evaluate(async ({ t, before, blend, videoFps }) => {
    for (const a of document.getAnimations()) { a.pause(); a.currentTime = t * 1000; }
    const seek = (id, frame) => new Promise((resolve) => {
      const v = document.getElementById(id);
      if (!v) return resolve();
      v.onseeked = resolve;
      v.currentTime = (frame + 0.5) / videoFps;
    });
    await Promise.all([seek('bg', before), seek('bg-next', before + 1)]);
    const next = document.getElementById('bg-next');
    if (next) next.style.opacity = blend;
  }, { t, before, blend: clipFrame - before, videoFps });
  await page.screenshot({ path: path.join(outDir, `f${String(i).padStart(3, '0')}.png`), clip: { x: 0, y: 0, width, height } });
}
console.log(`${path.basename(svgPath)} ${layout ?? ''}: ${frames} frame${frames === 1 ? '' : 's'}`);
await browser.close();
