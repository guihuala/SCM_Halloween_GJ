// Run from project root with Node.js and the sharp package available (or via NODE_PATH).
// node Tools/RenderCourierCursors.js
const fs = require('node:fs/promises');
const sharp = require('sharp');
const names = ['default', 'pointer', 'pressed', 'grab', 'grabbing', 'blocked', 'inspect'];
(async () => {
  await fs.mkdir('ArtPreviews/Cursors', { recursive: true });
  const parts = ['<svg xmlns="http://www.w3.org/2000/svg" width="700" height="180"><path fill="#32454B" d="M0 0H700V180H0Z"/>'];
  for (const [i, name] of names.entries()) {
    const source = await fs.readFile(`Assets/Art/Cursors/${name}.svg`, 'utf8');
    await sharp(Buffer.from(source)).resize(48, 48).png().toFile(`Assets/Resources/NightVoyage/Cursors/${name}.png`);
    parts.push(`<g transform="translate(${i * 100 + 26} 38)">${source.slice(source.indexOf('<g'), source.lastIndexOf('</svg>'))}</g>`);
    parts.push(`<text x="${i * 100 + 50}" y="126" text-anchor="middle" font-family="sans-serif" font-size="12" fill="#F1E7D2">${name}</text>`);
  }
  parts.push('</svg>');
  const sheet = parts.join('');
  await fs.writeFile('ArtPreviews/Cursors/cursor-states.svg', sheet);
  await sharp(Buffer.from(sheet)).png().toFile('ArtPreviews/Cursors/cursor-states.png');
  for (const name of names) {
    const { data, info } = await sharp(`Assets/Resources/NightVoyage/Cursors/${name}.png`).ensureAlpha().raw().toBuffer({ resolveWithObject: true });
    const alpha = data.filter((_, index) => index % 4 === 3);
    if (info.width !== 48 || info.height !== 48 || !alpha.some(v => v > 0) || !alpha.some(v => v === 0))
      throw new Error(`Invalid transparent cursor: ${name}`);
  }
  console.log('Rendered and verified 7 transparent 48×48 cursor textures.');
})().catch(error => { console.error(error); process.exitCode = 1; });
