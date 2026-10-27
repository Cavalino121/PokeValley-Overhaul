#!/usr/bin/env python3
"""
PokeValley-Overhaul - Clean Room Audio Downloader (Python Cross-Platform)
Downloads authentic Pokémon battle and fanfare audio files from public community
CDNs and pret decomp repositories directly into the game's audio directories.
"""

import os
import sys
import shutil
import urllib.request

REPO_ROOT = os.path.dirname(os.path.dirname(os.path.abspath(__file__)))
TARGET_DIR = os.path.join(REPO_ROOT, "src", "Content", "Pokemon", "Audio", "music")
STEAM_DIR = r"D:\SteamLibrary\steamapps\common\Stardew Valley\Content\Pokemon\Audio\music"

OGG_MANIFEST = [
    ("emerald_trainer.ogg", "https://play.pokemonshowdown.com/audio/oras-trainer.ogg"),
    ("rb_trainer.ogg", "https://play.pokemonshowdown.com/audio/hgss-kanto-trainer.ogg"),
    ("gym_leader.ogg", "https://play.pokemonshowdown.com/audio/bw2-kanto-gym-leader.ogg"),
    ("champion.ogg", "https://play.pokemonshowdown.com/audio/spl-elite4.ogg"),
    ("rb_wild.ogg", "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/wild_battle.ogg"),
    ("emerald_wild.ogg", "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/wild_battle.ogg"),
    ("mus_caught.ogg", "https://raw.githubusercontent.com/SheerSt/pokewilds/main/sounds/catch_fanfare.ogg"),
    ("victory_fanfare.ogg", "https://raw.githubusercontent.com/SheerSt/pokewilds/main/music/victory_fanfare1.ogg")
]

MIDI_MANIFEST = [
    ("emerald_wild.mid", "mus_vs_wild.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_wild.mid"),
    ("emerald_trainer.mid", "mus_vs_trainer.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_trainer.mid"),
    ("gym_leader.mid", "mus_vs_gym_leader.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_gym_leader.mid"),
    ("champion.mid", "mus_vs_champion.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_champion.mid"),
    ("rayquaza.mid", "mus_vs_rayquaza.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_rayquaza.mid"),
    ("primal.mid", "mus_vs_kyogre_groudon.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_kyogre_groudon.mid"),
    ("deoxys.mid", "mus_vs_deoxys.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_deoxys.mid"),
    ("mewtwo.mid", "mus_vs_mewtwo.mid", "https://raw.githubusercontent.com/pret/pokeemerald/master/sound/songs/midi/mus_vs_mewtwo.mid"),
    ("rb_trainer.mid", "mus_rg_vs_trainer.mid", "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_vs_trainer.mid"),
    ("rb_wild.mid", "mus_rg_vs_wild.mid", "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_vs_wild.mid"),
    ("lavender_town.mid", "mus_rg_lavender.mid", "https://raw.githubusercontent.com/pret/pokefirered/master/sound/songs/midi/mus_rg_lavender.mid")
]

def download_file(url, out_path):
    req = urllib.request.Request(url, headers={"User-Agent": "Mozilla/5.0"})
    with urllib.request.urlopen(req, timeout=15) as resp:
        with open(out_path, "wb") as f:
            f.write(resp.read())

def sync_audio():
    os.makedirs(TARGET_DIR, exist_ok=True)
    print("=== PokeValley Overhaul - Audio Sync ===")
    print(f"Target: {TARGET_DIR}")

    emerald_midi_dir = os.path.join(REPO_ROOT, "pokeemerald", "sound", "songs", "midi")
    if not os.path.exists(emerald_midi_dir):
        emerald_midi_dir = r"D:\Projetos\Nova pasta\pokeemerald\sound\songs\midi"

    firered_midi_dir = os.path.join(REPO_ROOT, "pokefirered", "sound", "songs", "midi")
    if not os.path.exists(firered_midi_dir):
        firered_midi_dir = r"D:\Projetos\Nova pasta\pokefirered\sound\songs\midi"

    # Sync OGG
    for fname, url in OGG_MANIFEST:
        out = os.path.join(TARGET_DIR, fname)
        if not os.path.exists(out):
            print(f"Downloading {fname}...")
            try:
                download_file(url, out)
            except Exception as e:
                print(f"Failed to download {fname}: {e}")
        else:
            print(f"OK (cached): {fname}")

    # Sync MIDI
    for fname, local_name, url in MIDI_MANIFEST:
        out = os.path.join(TARGET_DIR, fname)
        if not os.path.exists(out):
            copied = False
            if os.path.exists(os.path.join(emerald_midi_dir, local_name)):
                shutil.copyfile(os.path.join(emerald_midi_dir, local_name), out)
                print(f"Copied {fname} from pokeemerald")
                copied = True
            elif os.path.exists(os.path.join(firered_midi_dir, local_name)):
                shutil.copyfile(os.path.join(firered_midi_dir, local_name), out)
                print(f"Copied {fname} from pokefirered")
                copied = True

            if not copied:
                print(f"Downloading {fname} from pret...")
                try:
                    download_file(url, out)
                except Exception as e:
                    print(f"Failed to download {fname}: {e}")
        else:
            print(f"OK (cached): {fname}")

    # Mirror to Steam
    if os.path.exists(os.path.dirname(STEAM_DIR)):
        os.makedirs(STEAM_DIR, exist_ok=True)
        print("Mirroring to Steam...")
        for item in os.listdir(TARGET_DIR):
            s = os.path.join(TARGET_DIR, item)
            d = os.path.join(STEAM_DIR, item)
            if os.path.isfile(s):
                shutil.copyfile(s, d)
        print("Audio assets synchronized with Steam directory!")

if __name__ == "__main__":
    sync_audio()
