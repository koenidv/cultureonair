// update_playlists.js
// Requires Node 18+ for global fetch. If older: `npm i node-fetch` and: const fetch = (...args) => import('node-fetch').then(({default: fetch}) => fetch(...args));

const CLIENT_ID = process.env.SPOTIFY_CLIENT_ID || "YOUR_SPOTIFY_CLIENT_ID";
const CLIENT_SECRET = process.env.SPOTIFY_CLIENT_SECRET || "YOUR_SPOTIFY_CLIENT_SECRET";

const INPUT_PATH = "./Assets/data/countries.json";
const OUTPUT_PATH = "./Assets/data/countries.json";
const MAX_RESULTS_PER_QUERY = 20;
const SLEEP_BETWEEN_REQUESTS_MS = 100;
const DRY_RUN = false;

import fs from "node:fs/promises";
import path from "node:path";

// ====== UTIL ======
const sleep = (ms) => new Promise((res) => setTimeout(res, ms));

function normalize(s) {
  return s.toLowerCase().normalize("NFKD").replace(/\p{Diacritic}/gu, "").trim();
}

function containsAll(hay, needles) {
  hay = normalize(hay);
  return needles.every((n) => hay.includes(normalize(n)));
}

function wordOverlapScore(a, b) {
  const aw = new Set(normalize(a).split(/\W+/).filter(Boolean));
  const bw = new Set(normalize(b).split(/\W+/).filter(Boolean));
  if (aw.size === 0 || bw.size === 0) return 0;
  let overlap = 0;
  for (const w of aw) if (bw.has(w)) overlap++;
  return overlap / Math.max(aw.size, bw.size);
}

const CHART_KEYWORDS = [
  "top 50",
  "viral 50",
  "top songs",
  "top hits",
  "daily top",
  "weekly top",
  "chart",
  "charts",
  "top 100",
  "hot hits",
  "top 40",
];

const OWNER_PREF_HINTS = [
  "spotify",
  "spotify charts",
  "spotify charts editors",
  "spotifycharts",
];

function hasChartFlavor(p) {
  const title = p.name || "";
  const desc = p.description || "";
  const text = `${title} ${desc}`.toLowerCase();
  return CHART_KEYWORDS.some((k) => text.includes(k));
}

function scorePlaylist(p, entry) {
  const name = p.name || "";
  const desc = p.description || "";
  const owner = p.owner?.display_name || p.owner?.id || "";
  const followers = p.followers?.total || 0;

  const pName = entry.p_name || "";
  const cName = entry.c_name || "";

  let score = 0;

  if (normalize(name) === normalize(pName)) score += 8;
  score += 5 * wordOverlapScore(name, pName);

  if (containsAll(name, [cName])) score += 3;
  if (containsAll(desc, [cName])) score += 1;

  if (hasChartFlavor(p)) score += 4;

  const ownerNorm = normalize(owner);
  if (OWNER_PREF_HINTS.some((h) => ownerNorm.includes(normalize(h)))) score += 2;

  score += Math.min(Math.log10(followers + 1), 3);

  return score;
}

// ====== SPOTIFY API ======
async function getAccessToken() {
  const resp = await fetch("https://accounts.spotify.com/api/token", {
    method: "POST",
    headers: {
      Authorization:
        "Basic " +
        Buffer.from(`${CLIENT_ID}:${CLIENT_SECRET}`).toString("base64"),
      "Content-Type": "application/x-www-form-urlencoded",
    },
    body: new URLSearchParams({ grant_type: "client_credentials" }),
  });
  if (!resp.ok) {
    const t = await resp.text();
    throw new Error(`Token request failed: ${resp.status} ${t}`);
  }
  const data = await resp.json();
  return data.access_token;
}

async function searchPlaylists(q, token, limit = MAX_RESULTS_PER_QUERY) {
  const url = new URL("https://api.spotify.com/v1/search");
  url.searchParams.set("q", q);
  url.searchParams.set("type", "playlist");
  url.searchParams.set("limit", String(limit));

  const resp = await fetch(url, {
    headers: { Authorization: `Bearer ${token}` },
  });
  if (!resp.ok) {
    const t = await resp.text();
    throw new Error(`Search failed: ${resp.status} ${t}`);
  }
  const data = await resp.json();
  return data.playlists?.items || [];
}

async function getPlaylistDetails(id, token) {
  const url = `https://api.spotify.com/v1/playlists/${encodeURIComponent(id)}`;
  const resp = await fetch(url, { headers: { Authorization: `Bearer ${token}` } });
  if (!resp.ok) {
    if (resp.status === 404) return null; // playlist not found
    // Other errors: throw so caller can decide
    const t = await resp.text();
    throw new Error(`Playlist details failed (${id}): ${resp.status} ${t}`);
  }
  return await resp.json();
}

// NEW: quick existence check using details endpoint
async function checkPlaylistExists(id, token) {
  try {
    const details = await getPlaylistDetails(id, token);
    return !!details;
  } catch (e) {
    // For non-404 errors (e.g., transient network issues), we treat as unknown; return false to trigger search.
    return false;
  }
}

function buildQueries(entry) {
  const name = entry.p_name || "";
  const country = entry.c_name || "";
  const cId = entry.c_id || "";

  const queries = [
    `"${name}"`,
    `${name} ${country}`,
    `"Top 50 - ${country}"`,
    `"Viral 50 - ${country}"`,
    `"Top Songs - ${country}"`,
    `"Top Hits - ${country}"`,
    `${country} charts`,
    `${country} top 50`,
    `${country} top songs`,
    `${country} viral`,
  ];

  if (cId) {
    queries.push(`${country} ${cId} charts`);
  }

  return Array.from(new Set(queries.map((s) => s.trim()).filter(Boolean)));
}

async function findBestPlaylist(entry, token) {
  const queries = buildQueries(entry);
  let candidates = [];

  for (const q of queries) {
    await sleep(SLEEP_BETWEEN_REQUESTS_MS);
    let items = [];
    try {
      items = await searchPlaylists(q, token);
    } catch (e) {
      continue;
    }

    const detailed = [];
    for (const p of items) {
      await sleep(40);
      try {
        const full = await getPlaylistDetails(p.id, token);
        if (full) {
          detailed.push({
            id: full.id,
            name: full.name,
            description: full.description ?? p.description ?? "",
            owner: full.owner ?? p.owner,
            followers: full.followers ?? { total: 0 },
          });
        }
      } catch {
        // ignore
      }
    }
    candidates.push(...detailed);

    if (candidates.length >= 60) break;
  }

  if (candidates.length === 0) return null;

  const scored = candidates.map((p) => ({
    p,
    score: scorePlaylist(p, entry),
  }));

  const bestById = new Map();
  for (const item of scored) {
    const prev = bestById.get(item.p.id);
    if (!prev || item.score > prev.score) bestById.set(item.p.id, item);
  }
  const ranked = Array.from(bestById.values()).sort((a, b) => b.score - a.score);

  return ranked[0].p;
}

// ====== MAIN ======
async function main() {
  if (!CLIENT_ID || !CLIENT_SECRET || CLIENT_ID.includes("YOUR_") || CLIENT_SECRET.includes("YOUR_")) {
    console.error("Please set SPOTIFY_CLIENT_ID and SPOTIFY_CLIENT_SECRET env vars or hardcode them at the top.");
    process.exit(1);
  }

  const inputAbs = path.resolve(INPUT_PATH);
  const raw = await fs.readFile(inputAbs, "utf8");
  let entries;
  try {
    entries = JSON.parse(raw);
    if (!Array.isArray(entries)) throw new Error("Input JSON must be an array.");
  } catch (e) {
    console.error("Failed to parse input JSON:", e.message);
    process.exit(1);
  }

  const token = await getAccessToken();

  let updatedCount = 0;
  let unchangedCount = 0;
  const updated = [];

  for (const entry of entries) {
    const beforeId = entry.p_id;
    const countryLabel = entry.c_name || entry.c_id || "Unknown country";

    try {
      // Step 1: Check if existing ID is still valid
      if (beforeId) {
        const exists = await checkPlaylistExists(beforeId, token);
        if (exists) {
          unchangedCount++;
          console.log(`= ${countryLabel}: existing ID still valid (${beforeId}).`);
          updated.push(entry);
          continue; // skip searching
        }
      }

      // Step 2: Find replacement via search
      const best = await findBestPlaylist(entry, token);
      if (best && best.id && best.name) {
        entry.p_id = best.id;
        // Optionally sync name:
        // entry.p_name = best.name;
        updatedCount++;
        console.log(`✔ ${countryLabel}: ${entry.p_name} -> ${best.name} [${best.id}]`);
      } else {
        console.warn(`✖ ${countryLabel}: No suitable playlist found. Leaving p_id as-is (${beforeId ?? "null"}).`);
      }
    } catch (e) {
      console.warn(`! ${countryLabel}: Error (${e.message}). Leaving p_id as-is (${beforeId ?? "null"}).`);
    }

    updated.push(entry);
  }

  if (!DRY_RUN) {
    await fs.writeFile(OUTPUT_PATH, JSON.stringify(updated, null, 2), "utf8");
    console.log(`\nDone. Updated ${updatedCount}/${entries.length} entries. ${unchangedCount} unchanged.`);
    console.log(`Wrote: ${path.resolve(OUTPUT_PATH)}`);
  } else {
    console.log("(DRY RUN) Skipped writing output.");
  }
}

main().catch((e) => {
  console.error("Fatal:", e);
  process.exit(1);
});
