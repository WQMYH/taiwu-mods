# Mod Merge Workspace

## Directory Structure

```
todo-merge/
├── 3737804824/          # Original mod: MoFa (幻梦蝶)
├── 3746015837/          # Original mod: XiaoYao (逍遥游)
├── 3746021415/          # Original mod: KanPo (洞玄)
├── 3746022765/          # Original mod: MakeMaster (斡旋造化)
├── 3746024435/          # Original mod: BreakMaster (无忧突破)
├── 3746071342/          # Original mod: QuantumMaster (气运之子)
├── merge.bat                      # Merge all mods into one
├── deploy-to-game.bat            # Deploy merged mod to game
├── sync-Tianji-Creations.bat     # Sync Plugins from Workshop
└── merged/              # Merged output directory
    └── Tianji-Creations/
        ├── Config.lua
        ├── Plugins/
        ├── TianDao/
        ├── index.md     # Source mod information
        └── 幻梦蝶.png
```

## Usage

### 1. Merge Mods
```bash
merge.bat
```
Output: `merged/Tianji-Creations/`

### 2. Deploy to Game
```bash
deploy-to-game.bat
```
Deploys all contents from `merged/` to: `Game/Mod/`
Result: `Game/Mod/Tianji-Creations/`

### 3. Sync from Workshop (if needed)
```bash
sync-Tianji-Creations.bat
```
Syncs Plugins/ folder from all 6 workshop mods to merged/Tianji-Creations/Plugins/
Features:
- Checks if workshop mods are subscribed
- Lists missing mods with links
- Shows added/removed DLL files
- Verifies file changes

## Notes

- **todo-merge/**: Contains original mod files before merging
- **merged/**: Contains merged mod output (only index.md for documentation)
- No additional documentation in merged/ directory
