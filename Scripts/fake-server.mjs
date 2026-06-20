import http from 'node:http';

const HOST = '127.0.0.1';
const PORT = 1212;
const HUB_URL = 'http://localhost:21953';
const SS14_ADDRESS = `ss14://${HOST}:${PORT}/`;

const serverData = {
  name: 'SS14 Test Server [EN] [MRP]',
  players: 0,
  tags: ['lang:en', 'rp:med'],
};

const infoData = {
  connect_address: `${HOST}:${PORT}`,
  auth_server: 'http://localhost:5000',
  server_name: 'SS14 Test Server',
  server_owner: 'TestOwner',
  build: {
    version: '0.1.0',
    download_url: 'https://example.com/client.zip',
    manifest_url: 'https://example.com/manifest',
    manifest_hash: 'abc123',
    manifest_download_url: 'https://example.com/manifest.zip',
  },
  auth_information: {
    public_key: 'test-public-key-base64',
    address: SS14_ADDRESS,
  },
  preset: {
    mode: 'Sandbox',
    description: 'Test server for development',
  },
};

function handleStatus(req, res) {
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(serverData));
}

function handleInfo(req, res) {
  res.writeHead(200, { 'Content-Type': 'application/json' });
  res.end(JSON.stringify(infoData));
}

async function advertiseToHub() {
  try {
    const body = JSON.stringify({ address: SS14_ADDRESS });
    const response = await fetch(`${HUB_URL}/api/servers/advertise`, {
      method: 'POST',
      headers: { 'Content-Type': 'application/json' },
      body,
    });
    console.log(`[${new Date().toISOString()}] Hub advertise: ${response.status} ${response.statusText}`);
    if (response.ok) {
      const text = await response.text();
      if (text) console.log('  Response:', text);
    }
  } catch (err) {
    console.error(`[${new Date().toISOString()}] Hub advertise FAILED:`, err.message);
  }
}

const server = http.createServer((req, res) => {
  console.log(`[${new Date().toISOString()}] ${req.method} ${req.url}`);

  if (req.method === 'GET') {
    const url = new URL(req.url, `http://${HOST}:${PORT}`);
    const path = url.pathname.replace(/\/+$/, '') || '/';

    if (path === '/status') return handleStatus(req, res);
    if (path === '/info') return handleInfo(req, res);
  }

  res.writeHead(404);
  res.end('Not Found');
});

server.listen(PORT, HOST, () => {
  console.log(`\n=== SS14 Fake Server ===`);
  console.log(` Listening: http://${HOST}:${PORT}`);
  console.log(` SS14 addr: ${SS14_ADDRESS}`);
  console.log(` Hub URL:   ${HUB_URL}`);
  console.log(` Status:    http://${HOST}:${PORT}/status`);
  console.log(` Info:      http://${HOST}:${PORT}/info`);
  console.log(`\n Advertising to hub every 2 minutes...\n`);

  advertiseToHub();
  setInterval(advertiseToHub, 2 * 60 * 1000);
});
