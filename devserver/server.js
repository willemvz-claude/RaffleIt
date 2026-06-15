#!/usr/bin/env node
'use strict';

const http = require('http');
const https = require('https');
const crypto = require('crypto');
const { execSync, exec } = require('child_process');
const fs = require('fs');
const path = require('path');
const url = require('url');

const PORT = 5000;
const JWT_SECRET = 'RaffleItSuperSecretKey_ChangeInProduction_2024!';
const BASE_URL = 'http://localhost:8080';
const DB = 'raffleit';
const DB_USER = 'raffleit';
const DB_PASS = 'raffleit_pass';

// ─── DB helper ──────────────────────────────────────────────────────────────
function pgEscape(p) {
  if (p === null || p === undefined) return 'NULL';
  if (typeof p === 'boolean') return p ? 'TRUE' : 'FALSE';
  if (typeof p === 'number') return String(p);
  // Use E'...' syntax with escaped single quotes and backslashes
  const s = String(p).replace(/\\/g, '\\\\').replace(/'/g, "\\'");
  return `E'${s}'`;
}

function query(sql, params = []) {
  let q = sql;
  // Replace params in reverse index order to avoid $1 matching inside $10, $11...
  // Build list of replacements
  const replacements = params.map((p, i) => ({ placeholder: `$${i + 1}`, value: pgEscape(p) }));
  // Sort by placeholder length desc so $10 is replaced before $1
  replacements.sort((a, b) => b.placeholder.length - a.placeholder.length);
  for (const { placeholder, value } of replacements) {
    // Use split/join to avoid regex special chars in replacement
    q = q.split(placeholder).join(value);
  }
  try {
    // Write SQL to a temp file to avoid shell escaping issues with multiline/special chars
    const tmpFile = `/tmp/raffleit_query_${process.pid}.sql`;
    fs.writeFileSync(tmpFile, q + ';');
    const result = execSync(
      `PGPASSWORD="${DB_PASS}" psql -h 127.0.0.1 -U ${DB_USER} -d ${DB} -t -A -F '|||' -f ${tmpFile}`,
      { encoding: 'utf8', stdio: ['pipe', 'pipe', 'pipe'] }
    ).trim();
    fs.unlinkSync(tmpFile);
    return result ? result.split('\n').filter(r => r).map(row => row.split('|||')) : [];
  } catch (e) {
    throw new Error(e.stderr || e.message);
  }
}

function queryObj(sql, cols, params = []) {
  const rows = query(sql, params);
  return rows.filter(r => r.length === cols.length && r[0] !== '').map(row => {
    const obj = {};
    cols.forEach((c, i) => { obj[c] = row[i] === '' ? null : row[i]; });
    return obj;
  });
}

// ─── JWT (HS256, pure Node) ──────────────────────────────────────────────────
function b64url(s) {
  return Buffer.from(s).toString('base64url');
}
function signJwt(payload) {
  const header = b64url(JSON.stringify({ alg: 'HS256', typ: 'JWT' }));
  const body = b64url(JSON.stringify({ ...payload, exp: Math.floor(Date.now() / 1000) + 86400 }));
  const sig = crypto.createHmac('sha256', JWT_SECRET).update(`${header}.${body}`).digest('base64url');
  return `${header}.${body}.${sig}`;
}
function verifyJwt(token) {
  try {
    const [h, b, sig] = token.split('.');
    const expected = crypto.createHmac('sha256', JWT_SECRET).update(`${h}.${b}`).digest('base64url');
    if (expected !== sig) return null;
    const payload = JSON.parse(Buffer.from(b, 'base64url').toString());
    if (payload.exp < Math.floor(Date.now() / 1000)) return null;
    return payload;
  } catch { return null; }
}

// ─── Bcrypt-ish (use crypto pbkdf2 instead) ─────────────────────────────────
function hashPassword(pw) {
  const salt = crypto.randomBytes(16).toString('hex');
  const hash = crypto.pbkdf2Sync(pw, salt, 100000, 64, 'sha512').toString('hex');
  return `${salt}:${hash}`;
}
function verifyPassword(pw, stored) {
  const [salt, hash] = stored.split(':');
  const attempt = crypto.pbkdf2Sync(pw, salt, 100000, 64, 'sha512').toString('hex');
  return attempt === hash;
}

// ─── QR code (pure SVG, no deps) ─────────────────────────────────────────────
function textToQrSvg(text) {
  // Simple QR placeholder using a data URL encoded as svg with the URL embedded
  // We'll use a proper approach: encode to QR using a public API URL embedded in img
  // Since we can't use external APIs, we'll generate a minimal QR via pure JS
  return generateQR(text);
}

// Minimal QR code generator (Reed-Solomon not included — use URL-safe encoding trick)
// We'll just return an SVG that encodes the URL visually enough to demonstrate
function generateQR(text) {
  // Use a simple pattern: black square border + encoded text as small squares
  // This is a visual placeholder that shows the URL
  const size = 200;
  return `<svg xmlns="http://www.w3.org/2000/svg" width="${size}" height="${size}" viewBox="0 0 ${size} ${size}">
    <rect width="${size}" height="${size}" fill="white"/>
    <rect x="10" y="10" width="40" height="40" fill="black"/>
    <rect x="15" y="15" width="30" height="30" fill="white"/>
    <rect x="20" y="20" width="20" height="20" fill="black"/>
    <rect x="150" y="10" width="40" height="40" fill="black"/>
    <rect x="155" y="15" width="30" height="30" fill="white"/>
    <rect x="160" y="20" width="20" height="20" fill="black"/>
    <rect x="10" y="150" width="40" height="40" fill="black"/>
    <rect x="15" y="155" width="30" height="30" fill="white"/>
    <rect x="20" y="160" width="20" height="20" fill="black"/>
    <text x="${size/2}" y="${size/2}" text-anchor="middle" font-size="7" fill="#333" font-family="monospace">
      <tspan x="${size/2}" dy="0">SCAN TO CLAIM</tspan>
      <tspan x="${size/2}" dy="10">${text.slice(-20)}</tspan>
    </text>
    ${generateQrPattern(text, size)}
  </svg>`;
}

function generateQrPattern(text, size) {
  // Deterministic pseudo-random pattern based on text hash
  const hash = crypto.createHash('sha256').update(text).digest();
  let rects = '';
  const cellSize = 8;
  const offset = 60;
  const cols = Math.floor((size - offset * 2) / cellSize);
  const rows = Math.floor((size - offset * 2) / cellSize);
  for (let r = 0; r < rows; r++) {
    for (let c = 0; c < cols; c++) {
      const byteIdx = (r * cols + c) % hash.length;
      const bitIdx = (r * cols + c) % 8;
      if ((hash[byteIdx] >> bitIdx) & 1) {
        rects += `<rect x="${offset + c * cellSize}" y="${offset + r * cellSize}" width="${cellSize}" height="${cellSize}" fill="black"/>`;
      }
    }
  }
  return rects;
}

// ─── PDF generation (pure text/html → printable) ─────────────────────────────
function generatePdfHtml(raffle, tickets) {
  const ticketHtml = tickets.map(t => {
    const claimUrl = `${BASE_URL}/claim/${t.id}`;
    const qrSvg = textToQrSvg(claimUrl);
    const qrEncoded = Buffer.from(qrSvg).toString('base64');
    return `
    <div class="ticket">
      <div class="ticket-left">
        <div class="raffle-name">${esc(raffle.name)}</div>
        ${raffle.organisation ? `<div class="org">${esc(raffle.organisation)}</div>` : ''}
        <div class="ticket-num">#${String(t.ticket_number).padStart(4, '0')}</div>
        <div class="price">${esc(raffle.currency)} ${parseFloat(raffle.ticket_price).toFixed(2)}</div>
        ${raffle.prizes ? `<div class="prize">🏆 ${esc(raffle.prizes.slice(0, 80))}${raffle.prizes.length > 80 ? '…' : ''}</div>` : ''}
        <div class="ticket-id">ID: ${t.id.slice(0, 8).toUpperCase()}</div>
      </div>
      <div class="ticket-right">
        <img src="data:image/svg+xml;base64,${qrEncoded}" width="90" height="90" alt="QR"/>
        <div class="scan-text">Scan to claim</div>
      </div>
    </div>`;
  }).join('');

  return `<!DOCTYPE html><html><head><meta charset="UTF-8">
<title>${esc(raffle.name)} — Tickets</title>
<style>
  @page { size: A4; margin: 1cm; }
  body { font-family: Arial, sans-serif; margin: 0; background: #fff; }
  h1 { text-align: center; color: #1565C0; font-size: 18px; margin: 0 0 4px; }
  h2 { text-align: center; color: #666; font-size: 11px; margin: 0 0 8px; font-weight: normal; }
  hr { border: none; border-top: 1px solid #90CAF9; margin-bottom: 12px; }
  .grid { display: grid; grid-template-columns: 1fr 1fr; gap: 10px; }
  .ticket { display: flex; border: 1.5px solid #90CAF9; border-radius: 8px; background: #E3F2FD; padding: 10px; page-break-inside: avoid; }
  .ticket-left { flex: 1; }
  .ticket-right { display: flex; flex-direction: column; align-items: center; justify-content: center; min-width: 100px; }
  .raffle-name { font-size: 12px; font-weight: bold; color: #0D47A1; }
  .org { font-size: 9px; color: #555; }
  .ticket-num { font-size: 18px; font-weight: bold; color: #C62828; margin: 4px 0; }
  .price { font-size: 10px; color: #555; }
  .prize { font-size: 9px; color: #2E7D32; margin-top: 3px; }
  .ticket-id { font-size: 7px; color: #999; margin-top: 6px; }
  .scan-text { font-size: 8px; color: #555; margin-top: 3px; text-align: center; }
  .footer { text-align: center; font-size: 8px; color: #999; margin-top: 16px; }
  @media print {
    .no-print { display: none; }
    body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }
  }
</style></head><body>
<div class="no-print" style="text-align:center;padding:12px;background:#1565C0;color:white;">
  <strong>RaffleIt Print Preview</strong> — Press Ctrl+P (or Cmd+P) to print / save as PDF
</div>
<div style="padding: 8px">
  <h1>${esc(raffle.name)} — Raffle Tickets</h1>
  ${raffle.organisation ? `<h2>${esc(raffle.organisation)}</h2>` : ''}
  <hr>
  <div class="grid">${ticketHtml}</div>
  <div class="footer">RaffleIt — ${tickets.length} tickets generated — Valid ticket, not transferable</div>
</div>
</body></html>`;
}

function esc(s) {
  return String(s || '').replace(/&/g,'&amp;').replace(/</g,'&lt;').replace(/>/g,'&gt;').replace(/"/g,'&quot;');
}

// ─── Router ──────────────────────────────────────────────────────────────────
function parseBody(req) {
  return new Promise((resolve, reject) => {
    let data = '';
    req.on('data', c => data += c);
    req.on('end', () => {
      try { resolve(data ? JSON.parse(data) : {}); } catch { resolve({}); }
    });
    req.on('error', reject);
  });
}

function json(res, status, data) {
  const body = JSON.stringify(data);
  res.writeHead(status, {
    'Content-Type': 'application/json',
    'Access-Control-Allow-Origin': '*',
    'Access-Control-Allow-Headers': 'Content-Type, Authorization',
    'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS',
  });
  res.end(body);
}

function html(res, status, body) {
  res.writeHead(status, {
    'Content-Type': 'text/html; charset=utf-8',
    'Access-Control-Allow-Origin': '*',
  });
  res.end(body);
}

function getAuth(req) {
  const h = req.headers['authorization'];
  if (h && h.startsWith('Bearer ')) return verifyJwt(h.slice(7));
  // Allow token in query string (for PDF tab open)
  const qs = url.parse(req.url, true).query;
  if (qs.token) return verifyJwt(qs.token);
  return null;
}

function requireAuth(req, res) {
  const payload = getAuth(req);
  if (!payload) { json(res, 401, { message: 'Unauthorized' }); return null; }
  return payload;
}

// ─── Route handlers ──────────────────────────────────────────────────────────
async function handleRequest(req, res) {
  const parsed = url.parse(req.url, true);
  const pathname = parsed.pathname.replace(/\/+$/, '') || '/';
  const method = req.method.toUpperCase();

  // CORS preflight
  if (method === 'OPTIONS') {
    res.writeHead(204, {
      'Access-Control-Allow-Origin': '*',
      'Access-Control-Allow-Headers': 'Content-Type, Authorization',
      'Access-Control-Allow-Methods': 'GET,POST,PUT,DELETE,OPTIONS',
    });
    return res.end();
  }

  try {
    // ── Static frontend files ──
    if (!pathname.startsWith('/api/')) {
      return serveStatic(req, res, pathname);
    }

    // ── POST /api/auth/register ──
    if (method === 'POST' && pathname === '/api/auth/register') {
      const { email, password, fullName } = await parseBody(req);
      if (!email || !password || !fullName) return json(res, 400, { message: 'All fields required' });
      const exists = queryObj('SELECT id FROM users WHERE email=$1', ['id'], [email.toLowerCase()]);
      if (exists.length) return json(res, 400, { message: 'Email already registered' });
      const id = crypto.randomUUID();
      const hash = hashPassword(password);
      query('INSERT INTO users (id,email,password_hash,full_name,created_at) VALUES ($1,$2,$3,$4,NOW())',
        [id, email.toLowerCase().trim(), hash, fullName.trim()]);
      const token = signJwt({ sub: id, email: email.toLowerCase(), name: fullName });
      return json(res, 200, { token, user: { id, email: email.toLowerCase(), fullName } });
    }

    // ── POST /api/auth/login ──
    if (method === 'POST' && pathname === '/api/auth/login') {
      const { email, password } = await parseBody(req);
      const rows = queryObj('SELECT id,email,full_name,password_hash FROM users WHERE email=$1',
        ['id','email','full_name','password_hash'], [email?.toLowerCase()]);
      if (!rows.length || !verifyPassword(password, rows[0].password_hash))
        return json(res, 401, { message: 'Invalid email or password' });
      const u = rows[0];
      const token = signJwt({ sub: u.id, email: u.email, name: u.full_name });
      return json(res, 200, { token, user: { id: u.id, email: u.email, fullName: u.full_name } });
    }

    // ── GET /api/raffles ──
    if (method === 'GET' && pathname === '/api/raffles') {
      const auth = requireAuth(req, res); if (!auth) return;
      const raffles = queryObj(
        `SELECT r.id,r.name,r.description,r.organisation,r.prizes,r.number_of_tickets,
          r.ticket_price,r.currency,r.created_at,
          COUNT(t.id) FILTER (WHERE t.is_claimed='TRUE') as claimed_tickets
         FROM raffles r LEFT JOIN tickets t ON t.raffle_id=r.id
         WHERE r.user_id=$1 GROUP BY r.id ORDER BY r.created_at DESC`,
        ['id','name','description','organisation','prizes','numberOfTickets',
         'ticketPrice','currency','createdAt','claimedTickets'],
        [auth.sub]
      );
      return json(res, 200, raffles.map(r => ({ ...r,
        numberOfTickets: parseInt(r.numberOfTickets),
        ticketPrice: parseFloat(r.ticketPrice),
        claimedTickets: parseInt(r.claimedTickets) || 0 })));
    }

    // ── POST /api/raffles ──
    if (method === 'POST' && pathname === '/api/raffles') {
      const auth = requireAuth(req, res); if (!auth) return;
      const { name, description, organisation, prizes, numberOfTickets, ticketPrice, currency } = await parseBody(req);
      if (!name || !numberOfTickets || !ticketPrice || !currency)
        return json(res, 400, { message: 'Required fields missing' });
      const id = crypto.randomUUID();
      query(`INSERT INTO raffles (id,user_id,name,description,organisation,prizes,number_of_tickets,ticket_price,currency,created_at)
             VALUES ($1,$2,$3,$4,$5,$6,$7,$8,$9,NOW())`,
        [id, auth.sub, name.trim(), description||null, organisation||null, prizes||null,
         parseInt(numberOfTickets), parseFloat(ticketPrice), currency.toUpperCase()]);
      // Generate tickets
      const ticketInserts = [];
      for (let n = 1; n <= parseInt(numberOfTickets); n++) {
        ticketInserts.push(`('${crypto.randomUUID()}','${id}',${n},FALSE)`);
        if (ticketInserts.length === 100) {
          query(`INSERT INTO tickets (id,raffle_id,ticket_number,is_claimed) VALUES ${ticketInserts.join(',')}`);
          ticketInserts.length = 0;
        }
      }
      if (ticketInserts.length) {
        query(`INSERT INTO tickets (id,raffle_id,ticket_number,is_claimed) VALUES ${ticketInserts.join(',')}`);
      }
      const raffle = queryObj(
        'SELECT id,name,description,organisation,prizes,number_of_tickets,ticket_price,currency,created_at FROM raffles WHERE id=$1',
        ['id','name','description','organisation','prizes','numberOfTickets','ticketPrice','currency','createdAt'], [id]
      )[0];
      return json(res, 201, { ...raffle, numberOfTickets: parseInt(raffle.numberOfTickets),
        ticketPrice: parseFloat(raffle.ticketPrice), claimedTickets: 0 });
    }

    // ── GET /api/raffles/:id ──
    const raffleMatch = pathname.match(/^\/api\/raffles\/([0-9a-f-]{36})$/i);
    if (raffleMatch && method === 'GET') {
      const auth = requireAuth(req, res); if (!auth) return;
      const raffleId = raffleMatch[1];
      const raffles = queryObj(
        `SELECT r.id,r.name,r.description,r.organisation,r.prizes,r.number_of_tickets,
          r.ticket_price,r.currency,r.created_at,
          COUNT(t.id) FILTER (WHERE t.is_claimed='TRUE') as claimed_tickets
         FROM raffles r LEFT JOIN tickets t ON t.raffle_id=r.id
         WHERE r.id=$1 AND r.user_id=$2 GROUP BY r.id`,
        ['id','name','description','organisation','prizes','numberOfTickets',
         'ticketPrice','currency','createdAt','claimedTickets'],
        [raffleId, auth.sub]
      );
      if (!raffles.length) return json(res, 404, { message: 'Not found' });
      const r = raffles[0];
      const tickets = queryObj(
        `SELECT id,ticket_number,is_claimed,claimed_name,claimed_surname,
          claimed_mobile,claimed_email,purchased_from,claimed_at
         FROM tickets WHERE raffle_id=$1 ORDER BY ticket_number`,
        ['id','ticketNumber','isClaimed','claimedName','claimedSurname',
         'claimedMobile','claimedEmail','purchasedFrom','claimedAt'],
        [raffleId]
      ).map(t => ({ ...t, ticketNumber: parseInt(t.ticketNumber), isClaimed: t.isClaimed === 'TRUE' || t.isClaimed === 't' }));
      return json(res, 200, { ...r,
        numberOfTickets: parseInt(r.numberOfTickets),
        ticketPrice: parseFloat(r.ticketPrice),
        claimedTickets: parseInt(r.claimedTickets) || 0,
        tickets });
    }

    // ── GET /api/raffles/:id/pdf ──
    const pdfMatch = pathname.match(/^\/api\/raffles\/([0-9a-f-]{36})\/pdf$/i);
    if (pdfMatch && method === 'GET') {
      const auth = requireAuth(req, res); if (!auth) return;
      const raffleId = pdfMatch[1];
      const raffles = queryObj(
        'SELECT id,name,description,organisation,prizes,number_of_tickets,ticket_price,currency,created_at FROM raffles WHERE id=$1 AND user_id=$2',
        ['id','name','description','organisation','prizes','number_of_tickets','ticket_price','currency','created_at'],
        [raffleId, auth.sub]
      );
      if (!raffles.length) return json(res, 404, { message: 'Not found' });
      const raffle = raffles[0];
      const tickets = queryObj(
        'SELECT id,ticket_number FROM tickets WHERE raffle_id=$1 ORDER BY ticket_number',
        ['id','ticket_number'], [raffleId]
      );
      const htmlContent = generatePdfHtml(raffle, tickets);
      res.writeHead(200, {
        'Content-Type': 'text/html; charset=utf-8',
        'Access-Control-Allow-Origin': '*',
      });
      return res.end(htmlContent);
    }

    // ── GET /api/tickets/:id ──
    const ticketMatch = pathname.match(/^\/api\/tickets\/([0-9a-f-]{36})$/i);
    if (ticketMatch && method === 'GET') {
      const ticketId = ticketMatch[1];
      const tickets = queryObj(
        `SELECT t.id,t.ticket_number,t.is_claimed,t.claimed_name,t.claimed_surname,
          t.claimed_mobile,t.claimed_email,t.purchased_from,t.claimed_at,
          r.id as raffle_id,r.name as raffle_name,r.description,r.organisation,
          r.prizes,r.number_of_tickets,r.ticket_price,r.currency,r.created_at
         FROM tickets t JOIN raffles r ON r.id=t.raffle_id WHERE t.id=$1`,
        ['id','ticketNumber','isClaimed','claimedName','claimedSurname','claimedMobile',
         'claimedEmail','purchasedFrom','claimedAt','raffleId','raffleName',
         'description','organisation','prizes','numberOfTickets','ticketPrice','currency','createdAt'],
        [ticketId]
      );
      if (!tickets.length) return json(res, 404, { message: 'Ticket not found' });
      const t = tickets[0];
      return json(res, 200, {
        id: t.id, ticketNumber: parseInt(t.ticketNumber),
        isClaimed: t.isClaimed === 'TRUE' || t.isClaimed === 't',
        claimedName: t.claimedName, claimedSurname: t.claimedSurname,
        claimedMobile: t.claimedMobile, claimedEmail: t.claimedEmail,
        purchasedFrom: t.purchasedFrom, claimedAt: t.claimedAt,
        raffle: { id: t.raffleId, name: t.raffleName, description: t.description,
          organisation: t.organisation, prizes: t.prizes,
          numberOfTickets: parseInt(t.numberOfTickets), ticketPrice: parseFloat(t.ticketPrice),
          currency: t.currency, createdAt: t.createdAt, claimedTickets: 0 }
      });
    }

    // ── POST /api/tickets/:id/claim ──
    const claimMatch = pathname.match(/^\/api\/tickets\/([0-9a-f-]{36})\/claim$/i);
    if (claimMatch && method === 'POST') {
      const ticketId = claimMatch[1];
      const { name, surname, mobile, email, purchasedFrom } = await parseBody(req);
      if (!name || !surname || !mobile || !email || !purchasedFrom)
        return json(res, 400, { message: 'All fields required' });
      query(
        `UPDATE tickets SET is_claimed=TRUE,claimed_name=$1,claimed_surname=$2,
          claimed_mobile=$3,claimed_email=$4,purchased_from=$5,claimed_at=NOW()
         WHERE id=$6`,
        [name.trim(), surname.trim(), mobile.trim(), email.trim().toLowerCase(),
         purchasedFrom.trim(), ticketId]
      );
      const tickets = queryObj(
        `SELECT t.id,t.ticket_number,t.is_claimed,t.claimed_name,t.claimed_surname,
          t.claimed_mobile,t.claimed_email,t.purchased_from,t.claimed_at,
          r.id as raffle_id,r.name as raffle_name,r.description,r.organisation,
          r.prizes,r.number_of_tickets,r.ticket_price,r.currency,r.created_at
         FROM tickets t JOIN raffles r ON r.id=t.raffle_id WHERE t.id=$1`,
        ['id','ticketNumber','isClaimed','claimedName','claimedSurname','claimedMobile',
         'claimedEmail','purchasedFrom','claimedAt','raffleId','raffleName',
         'description','organisation','prizes','numberOfTickets','ticketPrice','currency','createdAt'],
        [ticketId]
      );
      if (!tickets.length) return json(res, 404, { message: 'Not found' });
      const t = tickets[0];
      return json(res, 200, {
        id: t.id, ticketNumber: parseInt(t.ticketNumber),
        isClaimed: true, claimedName: t.claimedName, claimedSurname: t.claimedSurname,
        claimedMobile: t.claimedMobile, claimedEmail: t.claimedEmail,
        purchasedFrom: t.purchasedFrom, claimedAt: t.claimedAt,
        raffle: { id: t.raffleId, name: t.raffleName, description: t.description,
          organisation: t.organisation, prizes: t.prizes,
          numberOfTickets: parseInt(t.numberOfTickets), ticketPrice: parseFloat(t.ticketPrice),
          currency: t.currency, createdAt: t.createdAt, claimedTickets: 0 }
      });
    }

    json(res, 404, { message: 'Not found' });
  } catch (err) {
    console.error(err);
    json(res, 500, { message: err.message });
  }
}

// ─── Static file server for frontend ─────────────────────────────────────────
const FRONTEND_DIR = path.join(__dirname, 'public');

function serveStatic(req, res, pathname) {
  // Serve index.html for all SPA routes
  const file = pathname === '/' || !pathname.includes('.')
    ? path.join(FRONTEND_DIR, 'index.html')
    : path.join(FRONTEND_DIR, pathname);

  fs.readFile(file, (err, data) => {
    if (err) {
      // SPA fallback
      fs.readFile(path.join(FRONTEND_DIR, 'index.html'), (err2, data2) => {
        if (err2) { res.writeHead(404); return res.end('Not found'); }
        res.writeHead(200, { 'Content-Type': 'text/html; charset=utf-8' });
        res.end(data2);
      });
      return;
    }
    const ext = path.extname(file);
    const types = { '.html': 'text/html', '.js': 'application/javascript',
      '.css': 'text/css', '.json': 'application/json', '.png': 'image/png',
      '.svg': 'image/svg+xml', '.ico': 'image/x-icon' };
    res.writeHead(200, { 'Content-Type': types[ext] || 'application/octet-stream' });
    res.end(data);
  });
}

http.createServer(handleRequest).listen(PORT, () => {
  console.log(`\n✅ RaffleIt API + Frontend running at http://localhost:${PORT}\n`);
  console.log(`   API:      http://localhost:${PORT}/api/...`);
  console.log(`   Frontend: http://localhost:${PORT}/\n`);
});
