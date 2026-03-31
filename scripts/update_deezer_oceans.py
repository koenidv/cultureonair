import json
import urllib.request
import urllib.parse
import time
import sys

DATA_PATH = "Assets/data/countries.json"

def fetch_json(url):
    try:
        req = urllib.request.Request(url, headers={'User-Agent': 'culture/on/air'})
        with urllib.request.urlopen(req) as response:
            return json.loads(response.read().decode())
    except Exception as e:
        return None

def find_best_ocean_playlist(name):
    # For oceans, we use more descriptive thematic queries
    queries = [name, f"{name} music", f"{name} vibes"]
    
    # Specific overrides for better thematic fit
    if "Arctic" in name: queries = ["Arctic", "Nordic Folk", "Icelandic Folk"]
    elif "Baltic" in name: queries = ["Sea Shanties", "Baltic Folk"]
    elif "Caribbean" in name or "China Sea" in name: queries = [name, "Reggae", "Island Vibes"]
    elif "Mediterranean" in name: queries = ["Mediterranean Sea", "Italian Summer", "Greek Summer"]

    best_score = -1
    best_id = None
    best_name = None

    for query in queries:
        encoded_query = urllib.parse.quote(query)
        url = f"https://api.deezer.com/search/playlist?q={encoded_query}"
        
        data = fetch_json(url)
        if not data or "data" not in data or len(data["data"]) == 0:
            continue

        for pl in data["data"]:
            title = pl["title"].lower()
            score = 0
            
            # Prefer larger playlists
            if pl.get("nb_tracks", 0) >= 30: score += 10
            
            # Check for name match
            if query.lower() in title: score += 15
            
            # High fans count is good
            if pl.get("nb_fans", 0) > 1000: score += 5

            if score > best_score:
                best_score = score
                best_id = str(pl["id"])
                best_name = pl["title"]

    return (best_id, best_name) if best_score > 5 else None

def main():
    with open(DATA_PATH, "r") as f:
        countries = json.load(f)

    oceans = [c for c in countries if c.get("ocean")]
    total = len(oceans)
    updated = 0

    print(f"Starting update for {total} oceans/seas...")

    for country in countries:
        if not country.get("ocean"):
            continue
            
        name = country["c_name"]
        sys.stdout.write(f"Searching for {name}... ")
        
        result = find_best_ocean_playlist(name)
        if result:
            country["p_id"] = result[0]
            country["p_name"] = result[1]
            updated += 1
            print(f"OK ({result[1]})")
        else:
            print("Not found")
        
        time.sleep(0.2)

    with open(DATA_PATH, "w") as f:
        json.dump(countries, f, indent=4)
    
    print(f"\nUpdate complete! {updated}/{total} oceans updated.")

if __name__ == "__main__":
    main()
