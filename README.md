# 7TD
![7TD Release](https://github.com/BoSen29/7TD/actions/workflows/build_release_tag.yml/badge.svg)

Become the best memer in town with these new custom emotes for your Legion TD2 chats. 

## Installation
- Close the game
- If not already done, follow this guide to install [BepInEx](https://github.com/LegionTD2-Modding/.github/wiki/Installation-of-BepInEx)
- Download the latest [release](https://github.com/BoSen29/7TD/releases/latest), and drop `7TD.dll` inside your `Legion TD 2/BepInEx/plugins/` folder
- You are done, you can start the game and enjoy all the :xdding: in the world.!

## Build
This mod is made using [BepInEx](https://github.com/BepInEx/BepInEx), which is required to build.\
Using JetBrain's Rider, you can use this as quick 'build and deploy' script:

```
dotnet build;
cp .\bin\Debug\netstandard2.0\NarrowMasterMinded.dll 'C:\SteamLibrary\steamapps\common\Legion TD 2\BepInEx\plugins\';
```
