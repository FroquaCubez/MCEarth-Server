# Project Earth API 🌍

*The central nervous system of Project Earth* 

## DISCLAIMER ⚠️

The API implementation is **NOT complete**, which means that not all of the game features might work as expected.

Thanks to [jackcaver](https://github.com/jackcaver). 🙌
Thanks to [BitcoderCZ](https://github.com/BitcoderCZ). 🙌
Special thanks to [LukeFZ](https://github.com/LukeFZ) for providing all of the necessary data for recovering missing features 🙌

## What does this component do? 🤔

The core API handles the bulk of game functionality - pretty much everything that isn't direct AR gameplay is done here.

| Currently working features               | Partially working features                            | 
|------------------------------------------|-------------------------------------------------------|
| 🗺️ Map                                   | 🏗️ Buildplates (Implementation is not complete)           |
| 🎯 Tappables                             | 🏞️ Adventures/Encounters (Implementation is not complete) |
| 🔨 Crafting                              | 🛠️ Smelting (Implementation is not complete)             |
| 🏬 Store                                 | 🔄 Buildplate sharing (Webpage is not implemented)       |
| 🎒 Inventory                             | 🏆 Challenges (Challenge conditions are broken)          |
| 💥 Boosts (Do not confuse with boost minis) |                                                       |
| 📖 Journal                               |                                                       |
| 📝 Activity Log                          |                                                       |

## Building 🛠️

1. `git clone --recursive https://github.com/andiricum2/ProjectEarthApi.git`
2. `cd Api`
3. `dotnet build` or use any IDE that you want and build it there

## Setting up the Project Earth server infrastructure. 🛠️

### Getting all the parts 🛠️

To start, ensure that you have built copies of all the required components downloaded:

- A built copy of the Api (you are in this repo), which you can get from [GitHub Actions](https://github.com/andiricum2/ProjectEarthApi/actions/workflows/build.yml)
- You'll need the Modified Minecraft Earth resource pack file with new mobs, download the `vanilla.zip` and placed in the `data/resourcepacks`. You can procure the resourcepack from [here](https://github.com/andiricum2/MC-Earth-Resourcepack/releases/latest).
- Our fork of [Cloudburst](https://github.com/Project-Earth-Team/Server). Builds of this can be found [here](https://ci.rtm516.co.uk/job/ProjectEarth/job/Server/job/earth-inventory/). This jar can be located elsewhere from the Api things.
- Run Cloudburst once to generate the file structure.
- In the plugins folder, you'll need [GenoaPlugin](https://github.com/jackcaver/GenoaPlugin), and [GenoaAllocatorPlugin](https://github.com/jackcaver/GenoaAllocatorPlugin). The CI for this can be found [here](https://github.com/jackcaver/GenoaPlugin/actions/workflows/CI.yml) and [here](https://github.com/jackcaver/GenoaAllocatorPlugin/actions/workflows/CI.yml). **Note: make sure to rename your GenoaAllocatorPlugin.jar to ZGenoaAllocatorPlugin.jar, or you will run into issues with class loading** 
- IF YOU WANT TO USE CUSTOM SPAWN YOU'LL NEED TO INSTALL THE TILE SERVER [here](https://cdn.discordapp.com/attachments/529281805216382998/1224666385792241765/TileServer.zip?ex=661e5273&is=660bdd73&hm=53938e23c35b7d08bd5387caf259f858f507c5d16c637348c5ea78704d357c85&) 🔴 YOU NEED DOCKER 😅

### Setting up 🚀

On the cloudburst side:

- Within the `plugins` folder, create a `GenoaAllocatorPlugin` folder, and in there, make a `key.txt` file containing a base64 encryption key and `ip.txt` file containing your server's ip address. An example key is:
 ```
/g1xCS33QYGC+F2s016WXaQWT8ICnzJvdqcVltNtWljrkCyjd5Ut4tvy2d/IgNga0uniZxv/t0hELdZmvx+cdA==
```
- Edit the cloudburst.yml file, and change the core API url to the url your Api will be accessible from.
- On the Api side, go to `data/config/apiconfig.json`, and add the following:
```json
"multiplayerAuthKeys": {
        "The same IP you put in ip.txt": "the same key you put in key.txt earlier"
 }
```
- Start up the Api.
- Start up Cloudburst. After a short while, the Api should mention a server being connected.
- If you run into issues, retrace your steps, or [contact us on Discord](https://discord.gg/Zf9aYZACU4).
- If everything works, your next challenge is to get Minecraft Earth to talk to your Api. If you're on Android, you can utilize [our patcher](https://github.com/Project-Earth-Team/PatcherApp). If you're on IOS, the only way to accomplish this without jailbreak is to utilize a DNS, such as bind9. Setup for that goes beyond the scope of this guide.
