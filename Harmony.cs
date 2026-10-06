using HarmonyLib;
using Il2CppFishNet;
using Il2CppFishNet.Example.ColliderRollbacks;
using Il2CppFishNet.Managing;
using Il2CppFishNet.Object;
using Il2CppGameKit.Utilities.Types;
using Il2Cppmadeinfairyland.fairyengine;
using Il2Cppmadeinfairyland.fairyengine.generation;
using Il2Cppmadeinfairyland.fairyengine.ui;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.equipment;
using Il2Cppmadeinfairyland.forsakenfrontiers.hazards;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2Cppmadeinfairyland.forsakenfrontiers.ui.mainmenu;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using UnityEngine;
using static PlayerUpgrades.Core;
using static System.Net.Mime.MediaTypeNames;

namespace PlayerUpgrades
{
    internal class Harmony
    {
        public static bool localDebug = true;
        public static int playerCount = 0;
        public static float spawnDistance = 1f;
        public static float upwardOffset = 1.0f;

        //not doors
        [HarmonyPatch(typeof(FFPlayer), nameof(FFPlayer.obr_AllPlayersDown))]
        public static class AllDeadPrefix
        {
            [HarmonyPostfix]
            public static void AllDeadPrefixHarmony(FFPlayer __instance)
            {
                MelonLogger.Msg($"[ALL PLAYERS DEAD]");
                arrivingToPOI = !arrivingToPOI;
            }
        }

        [HarmonyPatch(typeof(FFTrainBrake), nameof(FFTrainBrake.OnUsed))]
        public static class BrakePrefix
        {
            [HarmonyPrefix]
            public static void BrakePrefixServer(FFTrainBrake __instance)
            {
                amIHost = SteamIDUses.IsHost(localSteamID);
                arrivingToPOI = !arrivingToPOI;
                if (localDebug)
                {
                    MelonLogger.Msg($"\n\nEntered 'FFTrainBrake.OnUsed' Prefix.");
                    if (arrivingToPOI) MelonLogger.Msg($"Traveling to POI; Disabling Door Decoder\n\n");
                    else MelonLogger.Msg($"Returning from POI; Enabling Door Decoder\n\n");
                }
            }
        }

        [HarmonyPatch(typeof(FFTrader), nameof(FFTrader.BuyItem))]
        public static class BuyItemPrefix
        {
            [HarmonyPostfix]
            public static void BuyItemForDupe(FFTrader __instance, FFTrader.ShopItem shopItem, FFPlayer playerWhoPurchased)
            {
                MelonLogger.Msg($"[ITEM BOUGHT: {shopItem.OriginalItemName}]");
                if (SteamIDUses.IsHost(localSteamID) && upgrades[2].upgLvl >= 5)
                {
                    int randomInt = UnityEngine.Random.Range(1, 101);

                    //dupe item
                    if (randomInt >= 50)
                    {
                        Vector3 spawnPos = Vector3.zero;
                        if (__instance != null)
                        {
                            spawnPos = __instance.transform.position + (__instance.transform.forward * spawnDistance) + (Vector3.up * upwardOffset);
                        }
                        else if (playerWhoPurchased != null)
                        {
                            spawnPos = playerWhoPurchased.transform.position;
                        }

                        //get item network object
                        FFEquipment networkItem;
                        if (shopItem.OriginalItemName == "glowstick") networkItem = UpgradeApplier.mainGlowstick;
                        else if (shopItem.OriginalItemName == "lantern") networkItem = UpgradeApplier.mainLantern;
                        else if (shopItem.OriginalItemName == "flashlight") networkItem = UpgradeApplier.mainFlashlight;
                        else if (shopItem.OriginalItemName == "medkit") networkItem = UpgradeApplier.mainMedkit;
                        else if (shopItem.OriginalItemName == "adrenaline") networkItem = UpgradeApplier.mainAdrenaline;
                        else if (shopItem.OriginalItemName == "walkie-talkie") networkItem = UpgradeApplier.mainWalkie;
                        else if (shopItem.OriginalItemName == "boltcutters") networkItem = UpgradeApplier.mainBoltcutter;
                        else if (shopItem.OriginalItemName == "sledgehammer") networkItem = UpgradeApplier.mainSledgehammer;
                        else if (shopItem.OriginalItemName == "firecrackers") networkItem = UpgradeApplier.mainFirecrackers;
                        else if (shopItem.OriginalItemName == "environment marker") networkItem = UpgradeApplier.mainMarker;
                        else if (shopItem.OriginalItemName == "gasmask") networkItem = UpgradeApplier.mainGasMask;
                        else if (shopItem.OriginalItemName == "soda") networkItem = UpgradeApplier.mainSoda;
                        else if (shopItem.OriginalItemName == "dynamite") networkItem = UpgradeApplier.mainDynamite;
                        else networkItem = UpgradeApplier.mainGlowstick;

                        GameObject dupedItem = UnityEngine.Object.Instantiate(networkItem.gameObject, spawnPos, Quaternion.identity);

                        NetworkObject nob = dupedItem.GetComponent<NetworkObject>();
                        InstanceFinder.ServerManager.Spawn(nob);
                        MelonLogger.Msg($"[ITEM BOUGHT] ITEM DUPED!");
                    }
                    else MelonLogger.Msg($"[ITEM BOUGHT] Chance failed.");
                }
                else
                {
                    MelonLogger.Msg($"[ITEM BOUGHT] Not host or not lvl 5");
                }
            }
        }

        //doors
        [HarmonyPatch(typeof(FFTrain), nameof(FFTrain.OpenDoors))]
            public static class DoorsPostfix
            {

                //#1
                //Not At POI
                //
                [HarmonyPostfix]
                public static void DoorsPostfixServer(FFTrain __instance)
                {
                    if (localDebug) MelonLogger.Msg($"Entered 'FFTrain.OpenDoors' Postfix.");

                    if (!arrivingToPOI && !train.IsStoppedAtPOI)
                    {
                        if (localDebug)
                        {
                            MelonLogger.Msg($"[NOT AT POI] Doors Opened. Attempting UpgradeRequest Read/Decode/Apply\n");
                            MelonLogger.Msg($"[SERVER:BEFORE] __instance.Credits: {__instance.Credits}");
                            MelonLogger.Msg($"[SERVER:BEFORE] train.Credits: {train.Credits}\n");
                        }
                        int index = -1;

                        //////////////////////////////////
                        //DECODER: Upgrade Sync || 9
                        index = __instance.Credits % 10;
                        MelonLogger.Msg($"[Index found = [{index}] in [{train.Credits}] credits.]");


                        if (index == 9)
                        {
                            MelonLogger.Msg($"[Index 9] Credits to decode: {__instance.Credits}");
                            __instance.Credits -= 9;
                            __instance.Credits /= 10;

                            foreach (var upg in upgrades)
                            {
                                //
                                // !!! This might apply upgrades in REVERSE ORDER
                                //
                                int upgLevelFromIndex = __instance.Credits % 10;
                                upg.upgLvl = upgLevelFromIndex;
                                //upg.upgLvl = 10;  //test
                                __instance.Credits -= upgLevelFromIndex;
                                __instance.Credits /= 10;
                                MelonLogger.Msg($"[Index 9] Extracked Upgrade: {upgLevelFromIndex}");
                            }

                            //apply upgrades on each local machine
                            train.gameObject.name = "UPG:" + string.Join(",", upgrades.Select(u => u.upgLvl));
                            UpgradeApplier.ApplyUpgradesServer();

                            MelonLogger.Msg($"[Index 9] Extracked and Applied All Upgrades\n");
                            return;
                        }

                        //////////////////////////////////
                        //DECODER: Seed Sync
                        if (index == 7)
                        {
                            __instance.Credits -= 7;
                            __instance.Credits /= 10;

                            gameSeed = __instance.Credits;
                            updateSeed();
                            MelonLogger.Msg($"[Index 7] Seed Synced to Server\n");
                            return;
                        }

                        //////////////////////////////////
                        //DECODER: Credit Sync
                        if (index == 8)
                        {
                            __instance.Credits -= 8;
                            __instance.Credits /= 10;
                            if (__instance.Credits == 1) __instance.Credits -= 1; //0 check
                            MelonLogger.Msg($"[Index 8] Credits Synced to Server\n");
                            return;
                        }



                        //////////////////////////////////
                        //DECODER: Upgrade Increment & Sync
                        index = __instance.Credits % 1000;
                        __instance.Credits -= index;
                        __instance.Credits /= 1000;
                        if (__instance.Credits == 1) __instance.Credits -= 1; //0 check

                        train.Credits = __instance.Credits;

                        if (localDebug)
                        {
                            MelonLogger.Msg($"[SERVER:AFTER] Processing upgrade index: {index}");
                            MelonLogger.Msg($"[SERVER:AFTER] __instance.Credits: {__instance.Credits}");
                            MelonLogger.Msg($"[SERVER:AFTER] train.Credits: {train.Credits}\n");
                        }
                        //no request
                        if (index == -1) return;

                        //update server only
                        if (index == 9)
                        {
                            train.gameObject.name = "UPG:" + string.Join(",", upgrades.Select(u => u.upgLvl));
                            UpgradeApplier.ApplyUpgradesServer();
                            return;
                        }

                        //
                        //Each client updates their local upgrades
                        //
                        // get and level up upgrade
                        var upgrade = upgrades[index];

                        // adjust costs
                        int cost = upgrade.initCost + (upgrade.costScaler * upgrade.upgLvl);

                        // update local level and credits
                        upgrade.upgLvl++;

                        // Adjust server credits        || Optionally update datadeck in future update. Too tall an order currently
                        __instance.Credits -= cost;

                        // Change server name of train
                        train.gameObject.name =
                        "UPG:" +
                            string.Join(",", upgrades.Select(u => u.upgLvl));

                        if (localDebug)
                        {
                            MelonLogger.Msg($"[SERVER:AFTER_UPGRADED] cost: {cost}");
                            MelonLogger.Msg($"[SERVER:AFTER_UPGRADED] __instance.Credits: {__instance.Credits}");
                            MelonLogger.Msg($"[SERVER:AFTER_UPGRADED] train.Credits: {train.Credits}\n");
                        }

                        //every instance launches upgrade
                        UpgradeApplier.ApplyUpgradesServer();
                    }
                
                }


                //#2
                //At POI (once)
                //

                //LURKER
                [HarmonyPostfix]
                private static void ApplyLurkerToAllWorldLandmines()
                {
                    if (!arrivingToPOI || !train.IsStoppedAtPOI) return;
                    if (localDebug) MelonLogger.Msg($"[AT POI] Doors Opened. Attempting Lurker buff.");

                    if (SteamIDUses.IsHost(localSteamID) && upgrades[1].upgLvl == 5)
                    {
                        FFLandmine[] landmines = UnityEngine.Object.FindObjectsOfType<FFLandmine>();

                        if (landmines == null || landmines.Length == 0)
                        {
                            MelonLogger.Warning("[LURKER] No spawned FFLandmines were found in the scene.");
                            return;
                        }

                        //count var
                        int mineCount = 0;

                        //adjust each landmine
                        foreach (var mine in landmines)
                        {
                            //set mine to false
                            mine.Disarmed = true;

                            Transform lightTransform = mine.transform.Find("Light (4)");
                            if (lightTransform != null)
                            {
                                //get UL_FastLight from transformobject
                                var fastLight = lightTransform.GetComponent<Il2Cpp.UL_FastLight>();
                                if (fastLight != null)
                                {
                                    fastLight.color = Color.green;
                                }
                            }
                            mineCount++;
                        }
                        if (localDebug) MelonLogger.Msg($"[LURKER] Successfully disarmed {mineCount} landmines.");
                    }
                }
                    //FORERUNNER
                    [HarmonyPostfix]
                private static void ApplyForerunnerToWorldTIme()
                {
                    if (!arrivingToPOI || !train.IsStoppedAtPOI) return;
                    if (localDebug) MelonLogger.Msg($"[AT POI] Doors Opened. Attempting Forerunner buff.");

                    if (SteamIDUses.IsHost(localSteamID) && !worldGenUpgradesSet)
                    {
                        //adjust world time based on Forerunner lvl
                        if (upgrades[4].upgLvl == 5) world.Hour -= 3;
                        else if (upgrades[4].upgLvl >= 3) world.Hour -= 2;
                        else if (upgrades[4].upgLvl >= 1) world.Hour -= 1;

                        if (localDebug) MelonLogger.Msg($"Forerunner Upg Set.");
                    }
                    else
                    {
                        if (localDebug) MelonLogger.Msg($"Not Host/Already applied worldGen Buffs.");
                    }
                }
                //RANSACKER
                [HarmonyPostfix]
                private static void ApplyRansackerToAllWorldLoot()
                {
                    if (!arrivingToPOI || !train.IsStoppedAtPOI) return;
                    if (localDebug) MelonLogger.Msg($"\n[AT POI] Doors Opened. Attempting Ransacker buff.\n");


                    if (!SteamIDUses.IsHost(localSteamID) || worldGenUpgradesSet)
                    {
                        if (localDebug) MelonLogger.Msg($"\n[RANSACKER] Not host or worldGenUpgradesSet var not reset.\n");
                        if (localDebug) MelonLogger.Msg($"\n[RANSACKER] worldGenUpgradesSet = {worldGenUpgradesSet}.\n");
                        return;
                    }

                    //set global buffs attempted var to true HERE
                    //tick applied var
                    worldGenUpgradesSet = true;

                    int ransackerLevel = upgrades[3].upgLvl;
                    if (ransackerLevel <= 0)
                    {
                        if (localDebug) MelonLogger.Msg("[RANSACKER] Upgrade level is 0. Skipping loot buff.");
                        return;
                    }

                    // Find all active, spawned loot instances currently sitting on the map
                    FFLootItem[] spawnedItems = UnityEngine.Object.FindObjectsOfType<FFLootItem>();

                    if (spawnedItems == null || spawnedItems.Length == 0)
                    {
                        MelonLogger.Warning("[RANSACKER] No spawned FFLootItems were found in the scene.");
                    }

                    // Find all active, spawned loot container instances currently sitting on the map
                    FFGenerator[] spawnedContainers = UnityEngine.Object.FindObjectsOfType<FFGenerator>();

                    if (spawnedContainers == null || spawnedContainers.Length == 0)
                    {
                        MelonLogger.Warning("[RANSACKER] No spawned FFLootContainers were found in the scene.");
                    }

                    int buffedCount = 0;
                    int buffedLuckyCount = 0;
                    float multiplier = 1f;

                    int containerCount = 0;
                    int ruinedContainerCount = 0;
                    int containerRandAdd = 0;

                    //exposed loot value
                    if (ransackerLevel >= 3) multiplier = 1.25f;
                    else if (ransackerLevel >= 1) multiplier = 1.1f;

                    //container value
                    if (ransackerLevel >= 2) containerRandAdd = 2;
                    if (ransackerLevel >= 4) containerRandAdd = 4;
                    if (ransackerLevel >= 5) containerRandAdd = 5;


                    //adjust each item
                    foreach (var item in spawnedItems)
                    {
                        // Ensure object exists and is an active scene object (ignores uninstantiated asset prefabs)
                        if (item == null || item.gameObject == null || !item.gameObject.scene.isLoaded)
                            continue;

                        // Only buff items that actually have a positive value assigned by the game
                        if (item.value > 0)
                        {
                            //add lucky loot chance here>
                            int randNum = UnityEngine.Random.Range(0, 100);
                            if (randNum == 0 && ransackerLevel == 5) //1%
                            {
                                item.value = Mathf.CeilToInt(item.value * 5);
                                buffedLuckyCount++;
                                if (localDebug) MelonLogger.Msg($"[RANSACKER] Lucky Multiplier!");
                            }
                            else
                            {
                                item.value = Mathf.CeilToInt(item.value * multiplier);
                            }
                            buffedCount++;
                        }
                    }
                    //adjust each item container
                    foreach (var container in spawnedContainers)
                    {
                        // Ensure object exists and is an active scene object (ignores uninstantiated asset prefabs)
                        if (container == null || container.gameObject == null || !container.gameObject.scene.isLoaded)
                            continue;

                        int randNum = UnityEngine.Random.Range(0, containerRandAdd + 1);

                        // min/max changes
                        container.amount += randNum;

                        containerCount++;
                    }

                
            
            
                float overallLuckyChance = 100 * (buffedLuckyCount / buffedCount);

                    if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {buffedCount}/{spawnedItems.Length} loot items by {multiplier}x multiplier.");
                    if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {buffedLuckyCount}/{spawnedItems.Length} loot items by 5x lucky multiplier.");
                
                    if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {containerCount} loot container spots to have between 0-{containerRandAdd} Max possible items.");

                        if (localDebug) MelonLogger.Msg($"All World UpgradesSet.\n");
                }
            }
        }
}
