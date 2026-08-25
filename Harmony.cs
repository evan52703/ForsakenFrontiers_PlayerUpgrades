using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using MelonLoader;
using HarmonyLib;
using System;
using System.Collections.Generic;
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
using Il2CppFishNet.Managing;

namespace PlayerUpgrades
{
    internal class Harmony
    {
        public static bool localDebug = true;
        public static int playerCount = 0;

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

                    //DECODER: Upgrade Sync || 9
                    index = __instance.Credits % 10;
                    MelonLogger.Msg($"[Index found = [{index}] in [{train.Credits}] credits.]");
                    if (index == 9)
                    {
                        MelonLogger.Msg($"[Index 9] Credits to decode: {__instance.Credits}");
                        MelonLogger.Msg($"[Index 9] Expected to decode: 1000009");
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

                    //DECODER: Credit Sync
                    if (index == 8)
                    {
                        __instance.Credits -= 8;
                        __instance.Credits /= 10;
                        if (__instance.Credits == 1) __instance.Credits -= 1; //0 check
                        MelonLogger.Msg($"[Index 8] Credits Synced to Server\n");
                        return;
                    }



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
            //FORERUNNER
            [HarmonyPostfix]
            private static void ApplyForerunnerToWorldTIme()
            {
                if (!arrivingToPOI || !train.IsStoppedAtPOI) return;
                if (localDebug) MelonLogger.Msg($"[AT POI] Doors Opened. Attempting Forerunner buff.");

                if (SteamIDUses.IsHost(localSteamID) && !worldGenUpgradesSet)
                {
                    //adjust world time based on Forerunner lvl
                    world.Hour -= upgrades[4].upgLvl;
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


                if (!SteamIDUses.IsHost(localSteamID) || worldGenUpgradesSet) return;

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
                    return;
                }

                int buffedCount = 0;
                int buffedLuckyCount = 0;
                float multiplier = 1f + (ransackerLevel * (upgrades[3].upgScaler / 100f));

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
                        if (randNum == 0) //1%
                        {
                            item.value = Mathf.CeilToInt(item.value * (multiplier * 10));
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
                if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {buffedCount}/{spawnedItems.Length} loot items by {multiplier}x multiplier.");
                if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {buffedCount}/{spawnedItems.Length} loot items by {multiplier * 10}x lucky multiplier.");

                if (localDebug) MelonLogger.Msg($"All World UpgradesSet.\n");
            }
        }

        /*
        [HarmonyPatch(typeof(FFWorld), nameof(FFCoreManager.OnSaveGame))]
        public static class UpgradeSaveToFile
        {
            [HarmonyPostfix]
            public static void UpgradeSaveToFileHost(FFWorld __instance)
            {
                if (!SteamIDUses.IsHost(localSteamID)) return;

                //int saveFileNumber = FFCoreManager.ELoadSaveType;
                int saveFileNumber = FFCoreManager.file;

                string path = System.IO.Path.Combine(Application.dataPath, "../UserLibs/FileSaves", "save" + saveFileNumber + ".txt");
                if (!System.IO.File.Exists(path))
                {
                    MelonLogger.Msg($"Upg save file does not exist; creating one...");
                    System.IO.File.Create(path);
                }

                System.IO.File.WriteAtPath(path, train.name);
            }

        }
        */
    }
}
