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
        print(f"\nError fetching {url}: {e}")
        return None

def find_best_playlist(country_name):
    # Try multiple search variations
    queries = [f"Top 100 {country_name}", f"Top {country_name}"]
    
    best_overall_score = -1
    best_overall_id = None
    best_overall_name = None

    for query in queries:
        encoded_query = urllib.parse.quote(query)
        url = f"https://api.deezer.com/search/playlist?q={encoded_query}"
        
        data = fetch_json(url)
        if not data or "data" not in data or len(data["data"]) == 0:
            continue

        for pl in data["data"]:
            title = pl["title"].lower()
            score = 0
            
            # High score for official charts
            if "deezer" in pl.get("user", {}).get("name", "").lower():
                score += 15
            
            # Check for exact matches or containing names
            if country_name.lower() in title:
                score += 10
            
            if "top" in title:
                score += 5
                
            # Prefer larger playlists
            if pl.get("nb_tracks", 0) >= 50:
                score += 5
            elif pl.get("nb_tracks", 0) >= 20:
                score += 2
                
            if score > best_overall_score:
                best_overall_score = score
                best_overall_id = str(pl["id"])
                best_overall_name = pl["title"]

    # We want a reasonably confident match
    return (best_overall_id, best_overall_name) if best_overall_score > 10 else None

def main():
    try:
        with open(DATA_PATH, "r") as f:
            countries = json.load(f)
    except FileNotFoundError:
        print(f"Error: {DATA_PATH} not found.")
        return

    countries_to_process = [c for c in countries if not c.get("ocean")]
    total = len(countries_to_process)
    updated_count = 0

    print(f"Starting update for {total} countries...")

    for country in countries:
        if country.get("ocean"):
            continue
            
        name = country["c_name"]
        sys.stdout.write(f"Searching for {name}... ")
        sys.stdout.flush()
        
        result = find_best_playlist(name)
        if result:
            country["p_id"] = result[0]
            country["p_name"] = result[1]
            updated_count += 1
            print(f"OK ({result[1]})")
        else:
            print("Not found (keeping fallback)")
        
        # Avoid rate limiting
        time.sleep(0.15)

    with open(DATA_PATH, "w") as f:
        json.dump(countries, f, indent=4)
    
    print(f"\nUpdate complete! {updated_count}/{total} countries updated. Results saved to {DATA_PATH}")

if __name__ == "__main__":
    main()
