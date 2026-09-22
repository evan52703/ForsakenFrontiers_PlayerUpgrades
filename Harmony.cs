using HarmonyLib;
using Il2CppFishNet.Managing;
using Il2CppGameKit.Utilities.Types;
using Il2Cppmadeinfairyland.fairyengine;
using Il2Cppmadeinfairyland.fairyengine.generation;
using Il2Cppmadeinfairyland.fairyengine.ui;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
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

            ////LURKER
            //[HarmonyPostfix]
            //private static void ApplyLurkerToWorldEnemies()
            //{

            //    if (!arrivingToPOI && !train.IsStoppedAtPOI && SteamIDUses.IsHost(localSteamID))
            //    {

            //        int evaderLvl = upgrades[1].upgLvl;

            //        float statUpCrouch = 1;
            //        float statDistReduce = 1;

            //        //use equipment
            //        if (evaderLvl >= 1) statUpCrouch = 0.963f; //from 1.2; 1.25x
            //        if (evaderLvl >= 2) statDistReduce = 0.85f;
            //        if (evaderLvl >= 3) statUpCrouch = 0.608f; //from 1.2; 1.5x
            //        if (evaderLvl >= 4) statDistReduce = 0.65f;
            //        if (evaderLvl >= 5)
            //        {
            //            statUpCrouch = 0.085f; //from 1.2; 2x
            //            statDistReduce = 0.60f;
            //            //landmine immunity (in harmony)
            //        }


            //        //monsters
            //        //Each monster's player detection range, chance, and frequency reduced by X%
            //        FFAICore[] activeEnemies = new FFAICore[]
            //        {
            //        UpgradeApplier.mainSpider, UpgradeApplier.mainVentress, UpgradeApplier.mainNButcher, UpgradeApplier.mainForsaken,
            //        UpgradeApplier.mainShambler, UpgradeApplier.mainAbandoned, UpgradeApplier.mainPatient, UpgradeApplier.mainCarrier,
            //        UpgradeApplier.mainButcher, UpgradeApplier.mainNShambler, UpgradeApplier.mainShepherd, UpgradeApplier.mainPackrat
            //        };

            //        //monster detect radius
            //        for (int i = 0; i < activeEnemies.Length; i++)
            //        {
            //            activeEnemies[i].checkFarPlayerRadius = (int)Math.Ceiling(UpgradeApplier.initDetectRadius[i] * statDistReduce);
            //            activeEnemies[i].checkFarPlayerChance = (int)Math.Ceiling(UpgradeApplier.initDetectChance[i] * statDistReduce);
            //            activeEnemies[i].checkFarPlayersDelay = (int)Math.Ceiling(UpgradeApplier.initDetectDelay[i] * statDistReduce);
            //        }

            //        if (localDebug) foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Detect Radius: [{enemy.checkFarPlayerRadius}]");
            //        MelonLogger.Msg("\n");
            //        if (localDebug) foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Search Chance: [{(int)enemy.checkFarPlayerChance}]");
            //        MelonLogger.Msg("\n");
            //        if (localDebug) foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Detect Delay: [{enemy.checkFarPlayersDelay}]");
            //    }
            //}

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
