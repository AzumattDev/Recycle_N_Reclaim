# Description

<img src="https://github.com/AzumattDev/Recycle_N_Reclaim/assets/80414405/69371c0f-d56a-4019-807b-13696778a1f1" alt="5B0B26B8-8064-421E-8888-6D40E71C56DC-min" width="100%" style="max-width: 1280px; height: auto;">

## A mod that allows you to recycle/reclaim items back into resources used to make them. Adds a 'Reclaim' tab/button to the crafting menu. Additionally can be used inside of your inventory directly.

`Version checks with itself. If installed on the server, it will kick clients who do not have it installed.`

`This mod uses ServerSync, if installed on the server and all clients, it will sync all configs to client`

`This mod uses a file watcher. If the configuration file is not changed with BepInEx Configuration manager, but changed in the file directly on the server, upon file save, it will sync the changes to all clients.`


---

## Original Author's Credits

All original code for the crafting tab/button was written
by [ABearCodes](https://valheim.thunderstore.io/package/abearcodes/SimpleRecycling/)   
Original Mod Link: [Simple Recycling](https://valheim.thunderstore.io/package/abearcodes/SimpleRecycling/)

## Mod Information

This mod is a combination
of [OdinsInventoryDiscard](https://valheim.thunderstore.io/package/OdinPlus/OdinsInventoryDiscard/) and the mod above.
The aim is to provide an all in one solution for
recycling items back into resources.

All code changes after the initial release of this mod were written by me. This includes the merging of the two mods,
addition of ServerSync and
any improvements made to the code.

<details>
<summary><b>Installation Instructions</b></summary>

***You must have BepInEx installed correctly! I can not stress this enough.***

### Manual Installation

`Note: (Manual installation is likely how you have to do this on a server, make sure BepInEx is installed on the server correctly)`

1. **Download the latest release of BepInEx.**
2. **Extract the contents of the zip file to your game's root folder.**
3. **Download the latest release of Recycle_N_Reclaim from Thunderstore.io.**
4. **Extract the contents of the zip file to the `BepInEx/plugins` folder.**
5. **Launch the game.**

### Installation through r2modman or Thunderstore Mod Manager

1. **Install [r2modman](https://valheim.thunderstore.io/package/ebkr/r2modman/)
   or [Thunderstore Mod Manager](https://www.overwolf.com/app/Thunderstore-Thunderstore_Mod_Manager).**

   > For r2modman, you can also install it through the Thunderstore site.
   ![](https://i.imgur.com/s4X4rEs.png "r2modman Download")

   > For Thunderstore Mod Manager, you can also install it through the Overwolf app store
   ![](https://i.imgur.com/HQLZFp4.png "Thunderstore Mod Manager Download")
2. **Open the Mod Manager and search for "Recycle_N_Reclaim" under the Online
   tab. `Note: You can also search for "Azumatt" to find all my mods.`**

   `The image below shows VikingShip as an example, but it was easier to reuse the image.`

   ![](https://i.imgur.com/5CR5XKu.png)

3. **Click the Download button to install the mod.**
4. **Launch the game.**

</details>

<br>
<br>

<details><summary><b>Configuration Options</b></summary>

### Please note that Inventory Recycle and Reclaiming are different sections as well as different functionality within the game. Recycling happens only in the inventory and (by default) limited to admins only. Change the config should you wish to give this ability to everyone. Admins will always (by default) get 100% of the resources returned to them. Reclaiming happens in the crafting menu and is available to everyone. The amount of resources returned is configurable but is 50% by default.

#### What this looks like in the [BepInEx Configuration Manager](https://valheim.thunderstore.io/package/Azumatt/Official_BepInEx_ConfigurationManager/)

![image](https://github.com/AzumattDev/Recycle_N_Reclaim/assets/80414405/00f139cf-30a5-4433-b154-4c544aa1efd9)

`1 - General`

Lock Configuration [Synced with Server]

* If on, the configuration is locked and can be changed by server admins only.
    * Default Value: On

`2 - Inventory Recycle`

Enabled [Synced with Server]

* If on, you'll be able to discard things inside of the player inventory.
    * Default Value: On

Lock to Admin [Synced with Server]

* If on, only admin's can use this feature.
    * Default Value: On

DiscardHotkey(s) [Not Synced with Server]

* The hotkey to discard an item or regain resources. Must be enabled
    * Default Value: Delete

ReturnUnknownResources [Synced with Server]

* If on, discarding an item in the inventory will return resources if recipe is unknown
    * Default Value: Off

ReturnEnchantedResources [Synced with Server]

* If on and Epic Loot is installed, discarding an item in the inventory will return resources for Epic Loot enchantments
    * Default Value: Off

ReturnResources [Synced with Server]

* Fraction of resources to return (0.0 - 1.0). This setting is forced to be between 0 and 1. Any higher or lower values
  will be set to 0 or 1 respectively.
    * Default Value: 1

`3 - Reclaiming`

RecyclingRate [Synced with Server]

* Rate at which the resources are recycled. Value must be between 0 and 1.
  The mod always rolls *down*, so if you were supposed to get 2.5 items, you would only receive 2. If the recycling rate
  is 0.5 (50%), the player will receive half of the resources they would usually need to craft the item, assuming a
  single item in a stack and the item is of quality level 1. If the item is of higher quality, the resulting yield would
  be higher as well.
    * Default Value: 0.5

UnstackableItemsAlwaysReturnAtLeastOneResource [Synced with Server]

* If enabled and recycling a specific _unstackable_ item would yield 0 of a material,
  instead you will receive 1. If disabled, you get nothing.
    * Default Value: On

RequireExactCraftingStationForRecycling [Synced with Server]

* If enabled, recycling will also check for the required crafting station type and level.
  If disabled, will ignore all crafting station requirements altogether.
  Enabled by default, to keep things close to how Valheim operates.
    * Default Value: On

PreventZeroResourceYields [Synced with Server]

* If enabled and recycling an item that would yield 0 of any material,
  instead you will receive 1. If disabled, you get nothing.
    * Default Value: On

AllowRecyclingUnknownRecipes [Synced with Server]

* If enabled, it will allow you to recycle items that you do not know the recipe for yet.
  Disabled by default as this can be cheaty, but sometimes required due to people losing progress.
    * Default Value: Off

`4 - UI`

ContainerButtonPosition [Synced with Server]

* The last saved recycling button position stored in JSON
    * Default Value: {"x":496.0,"y":-374.0,"z":-1.0}

ContainerRecyclingEnabled [Synced with Server]

* If enabled, the mod will display the container recycling button
    * Default Value: On

NotifyOnSalvagingImpediments [Synced with Server]

* If enabled and recycling a specific item runs into any issues, the mod will print a message
  in the center of the screen (native Valheim notification). At the time of implementation,
  this happens in the following cases:

- not enough free slots in the inventory to place the resulting resources
- player does not know the recipe for the item
- if enabled, cases when `PreventZeroResourceYields` kicks in and prevent the crafting
    * Default Value: On

EnableExperimentalCraftingTabUI [Synced with Server]

* If enabled, will display the experimental work in progress crafting tab UI
  Enabled by default.
    * Default Value: On

HideRecipesForEquippedItems [Synced with Server]

* If enabled, it will hide equipped items in the crafting tab.
  This does not make the item recyclable and only influences whether or not it's shown.
  Enabled by default.
    * Default Value: On

IgnoreItemsOnHotbar [Synced with Server]

* If enabled, it will hide hotbar items in the crafting tab.
  Enabled by default.
    * Default Value: On

StationFilterEnabled [Synced with Server]

* If enabled, will filter all recycling recipes based on the crafting station
  used to produce said item. Main purpose of this is to prevent showing food
  as a recyclable item, but can be extended further if needed.
  Enabled by default
    * Default Value: On

StationFilterList [Synced with Server]

* Comma separated list of crafting stations (by their "prefab name")
  recipes from which should be ignored in regards to recycling.
  Main purpose of this is to prevent showing food as a recyclable item,
  but can be extended further if needed.

Full list of stations used in recipes as of 0.216.9:

- identifier: `forge` in game name: Forge
- identifier: `blackforge` in game name: Black Forge
- identifier: `piece_workbench` in game name: Workbench
- identifier: `piece_cauldron` in game name: Cauldron
- identifier: `piece_stonecutter` in game name: Stonecutter
- identifier: `piece_artisanstation` in game name: Artisan table
- identifier: `piece_magetable` in game name: Galdr table

    * Default Value: piece_cauldron

`zDebug`

DebugAlwaysDumpAnalysisContext [Synced with Server]

* If enabled will dump a complete detailed recycling report every time. This is taxing in terms
  of performance and should only be used when debugging issues.
    * Default Value: Off

DebugAllowSpammyLogs [Synced with Server]

* If enabled, will spam recycling checks to the console.
  VERY. VERY. SPAMMY. Influences performance.
    * Default Value: Off

</details>



<p align="center">
  <img alt="2023-06-22_11-33-22" src="https://github.com/AzumattDev/SkillManagerModTemplate/assets/80414405/2dbbe853-eccc-460c-bf41-111f38d0a6ac"/>
</p>
<img src="https://github.com/AzumattDev/Recycle_N_Reclaim/assets/80414405/cd4e10be-1728-435a-ae3f-47b7daa2f3cd" alt="E1CB4F06-72CE-43BC-9FFC-1E53E89640EE-min" width="100%" style="max-width: 1280px; height: auto;">

<img src="https://github.com/AzumattDev/Recycle_N_Reclaim/assets/80414405/102410f4-4b8a-4cd4-919e-421ddb33db38" alt="5B0B26B8-8064-421E-8888-6D40E71C56DC-min" width="100%" style="max-width: 1280px; height: auto;">


`Feel free to reach out to me on discord if you need manual download assistance.`

# Author Information

### Azumatt

`DISCORD:` Azumatt#2625

`STEAM:` https://steamcommunity.com/id/azumatt/

For Questions or Comments, find me in the Odin Plus Team Discord or in mine:

[![https://i.imgur.com/XXP6HCU.png](https://i.imgur.com/XXP6HCU.png)](https://discord.gg/Pb6bVMnFb2)
<a href="https://discord.gg/pdHgy6Bsng"><img src="https://i.imgur.com/Xlcbmm9.png" href="https://discord.gg/pdHgy6Bsng" width="175" height="175"></a>