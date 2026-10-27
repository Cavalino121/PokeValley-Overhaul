# PokeValley - Clean Room Audio Directory

This directory holds authentic Pokémon music tracks (OGG and MIDI).
In accordance with clean-room repository guidelines, binary music files are **not** committed to Git.

To download or synchronize all audio assets automatically, run:
```powershell
powershell -ExecutionPolicy Bypass -File scripts\download_audio_assets.ps1
```
or with Python:
```bash
python scripts/download_audio_assets.py
```

The build script (`scripts/build_local.ps1`) will also automatically invoke the downloader if assets are missing.
