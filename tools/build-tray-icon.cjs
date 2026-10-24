// Run with Node.js and sharp installed (or NODE_PATH pointing to its modules).
const fs = require('node:fs');
const path = require('node:path');
const sharp = require('sharp');
(async () => {
  const assets = path.join(__dirname, '..', 'ScreenMover', 'Assets');
  const sizes = [16, 20, 24, 32, 40, 48, 64];
  const frames = await Promise.all(sizes.map(size => sharp(path.join(assets, 'TrayIcon.svg'), {density: 384}).resize(size, size).png().toBuffer()));
  const header = Buffer.alloc(6 + sizes.length * 16);
  header.writeUInt16LE(1, 2); header.writeUInt16LE(sizes.length, 4);
  let offset = header.length;
  sizes.forEach((size, i) => {
    const pos = 6 + i * 16;
    header[pos] = size; header[pos + 1] = size;
    header.writeUInt16LE(1, pos + 4); header.writeUInt16LE(32, pos + 6);
    header.writeUInt32LE(frames[i].length, pos + 8); header.writeUInt32LE(offset, pos + 12);
    offset += frames[i].length;
  });
  fs.writeFileSync(path.join(assets, 'TrayIcon.ico'), Buffer.concat([header, ...frames]));
  console.log('TrayIcon.ico: ' + sizes.join(', ') + ' px');
})().catch(error => { console.error(error); process.exitCode = 1; });
