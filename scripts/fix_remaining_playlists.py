import json
import urllib.request
import urllib.parse
import time
import sys

DATA_PATH = "Assets/data/countries.json"
WORLDWIDE_ID = "3155776842"

def fetch_json(url):
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'culture/on/air'})
        with urllib.request.urlopen(req) as response:
            return json.loads(response.read().decode())
    except:
        return None

def find_playlist(queries):
    for query in queries:
        encoded = urllib.parse.quote(query)
        url = f"https://api.deezer.com/search/playlist?q={encoded}"
        data = fetch_json(url)
        if data and "data" in data and len(data["data"]) > 0:
            # Score first few results
            for pl in data["data"][:5]:
                if pl.get("nb_tracks", 0) > 10:
                    return str(pl["id"]), pl["title"]
    return None, None

# Specific thematic overrides for territories without direct charts
OVERRIDE_MAP = {
    "Antarctica": ["Ambient Ice", "Arctic Chill", "Atmospheric Ambient"],
    "Vatican": ["Gregorian Chant", "Classical Essentials", "Sacred Choral Music"],
    "Antigua and Barb.": ["Caribbean Top 100", "Soca 2025", "Reggae Hits"],
    "Grenada": ["Caribbean Top 100", "Soca 2025"],
    "St. Kitts and Nevis": ["Caribbean Top 100", "Island Vibes"],
    "Saint Lucia": ["Caribbean Top 100", "Soca"],
    "St. Vin. and Gren.": ["Caribbean Top 100", "Island Hits"],
    "Trinidad and Tobago": ["Soca 2025", "Trinidad Chutney", "Caribbean Top 100"],
    "Dominican Rep.": ["Top Dominican Republic", "Bachata Hits", "Merengue Essentials"],
    "Burundi": ["African Heat", "East Africa Top 100"],
    "Central African Rep.": ["African Heat", "Central Africa Hits"],
    "Dem. Rep. Congo": ["Congo Rumba", "African Heat"],
    "Comoros": ["Indian Ocean Vibes", "Island Music"],
    "Djibouti": ["East Africa Top 100", "Arabic Top Hits"],
    "Eritrea": ["Ethiopian Top 100", "East Africa Hits"],
    "Equatorial Guinea": ["African Heat", "Spanish Hits"],
    "Sao Tome and Principe": ["African Heat", "Lusophone African Hits"],
    "eSwatini": ["South Africa Top 50", "African Heat"],
    "Seychelles": ["Indian Ocean Vibes", "Island Music"],
    "Mauritania": ["North Africa Top 100", "Arabic Hits"],
    "S. Sudan": ["African Heat", "East Africa Top 100"],
    "Bosnia and Herz.": ["Balkan Top 50", "Ex-Yu Hits"],
    "North Macedonia": ["Balkan Top 50", "Ex-Yu Hits"],
    "Liechtenstein": ["Top Switzerland", "Top Germany"],
    "Bhutan": ["Himalayan Chill", "Indian Top 100"],
    "Tajikistan": ["Central Asia Hits", "Russian Top 100"],
    "Turkmenistan": ["Central Asia Hits", "Russian Top 100"],
    "Uzbekistan": ["Central Asia Hits", "Russian Top 100"],
    "Brunei": ["Malay Top 100", "South East Asia Hits"],
    "Timor-Leste": ["South East Asia Hits", "Indonesian Top 100"],
    "Micronesia": ["Pacific Island Vibes", "Island Reggae"],
    "Kiribati": ["Pacific Island Vibes", "Island Reggae"],
    "Nauru": ["Pacific Island Vibes", "Island Reggae"],
    "Solomon Is.": ["Pacific Island Vibes", "Island Reggae"],
    "Vanuatu": ["Pacific Island Vibes", "Island Reggae"],
    "Papua New Guinea": ["Pacific Island Vibes", "Island Reggae"],
    "Maldives": ["Indian Ocean Vibes", "Chill Island"],
    "South Atlantic": ["Ocean Vibes", "Sea Shanties", "Deep Blue"]
}

def main():
    with open(DATA_PATH, "r") as f:
        countries = json.load(f)

    updated = 0
    total_broken = len([c for c in countries if not c["p_id"].isdigit()])
    
    print(f"Fixing {total_broken} remaining Spotify IDs...")

    for country in countries:
        if country["p_id"].isdigit():
            continue
            
        name = country["c_name"]
        sys.stdout.write(f"Finding replacement for {name}... ")
        
        # 1. Check override map
        queries = OVERRIDE_MAP.get(name, [f"Top {name}", f"{name} Music"])
        
        pl_id, pl_name = find_playlist(queries)
        
        if not pl_id:
            # 2. Final fallback to Worldwide if search failed
            pl_id, pl_name = WORLDWIDE_ID, "Top Worldwide"
            
        country["p_id"] = pl_id
        country["p_name"] = pl_name
        updated += 1
        print(f"DONE ({pl_name})")
        time.sleep(0.2)

    with open(DATA_PATH, "w") as f:
        json.dump(countries, f, indent=4)
    
    print(f"\nFinished! Updated {updated} entries.")

if __name__ == "__main__":
    main()
