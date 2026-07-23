using HarmonyLib;
using Il2CppFishNet;
using Il2CppFishNet.Example.ColliderRollbacks;
using Il2CppFishNet.Object;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.equipment;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.fairyengine.actor;
using Il2Cppmadeinfairyland.forsakenfrontiers.demo;
using Il2Cppmadeinfairyland.forsakenfrontiers.hazards;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2Cppmadeinfairyland.forsakenfrontiers.ui.PauseMenu.PlayerList;
using Il2CppSystem.Collections.Generic;
using Il2CppSystem.Data;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.PlayerLoop;
using UnityEngine.UIElements;
using static PlayerUpgrades.Core;

namespace PlayerUpgrades
{
    public class UpgradeApplier
    {
        //upgs
        public static int indexOfUpgrade = -1;
        public static int costOfUpgrade = -1;
        public static int testVar = 100;

        //hacker init
        //items
        public static FFSprayTool mainMarker;
        public static FFBoltcutters mainBoltcutter;
        public static FFSledgehammer mainSledgehammer;
        public static FFStunLight mainStunlight;

        public static FFGlowstick mainGlowstick;
        public static FFLantern mainLantern;
        public static FFFlashlight mainFlashlight;

        public static int[] originalItemDurabilityValues = { 32, 8, 15, 5 };
        public static double[] originalItemLightValues = { 6, 15, 28 };

        //loot items
        //public static FFLootItem[] originalLootItemList;
        //public static int[] originalLootItemValuesList;

        //debug
        public static bool localDebug = true;
        public static int ransackerApplied = 0;

        public static FFEquipment[] equipment;


        public static void ServerApplyUpgrade(int index)
        {

            if(localDebug) MelonLogger.Msg($"Triggering train.OpenDoors & requesting credits * 1000.");
            // open doors for all players on server 
            //ENCODER
            if (train.Credits == 0) train.Credits++; //0 check

            int encoded = (train.Credits * 999) + index;

            if (localDebug)
            {
                MelonLogger.Msg($"ENCODE:");
                MelonLogger.Msg($"Credits: {train.Credits}");
                MelonLogger.Msg($"Index: {index}");
                MelonLogger.Msg($"Encoded: {encoded}");
            }
            train.RpcWriter___Server_svr_RequestAddCredits_3316948804(encoded);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(true);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(false); //simultaneously open and close doors for trigger
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
            [HarmonyPostfix]
            public static void DoorsPostfixServer(FFTrain __instance)
            {
                if (localDebug) MelonLogger.Msg($"Entered 'FFTrain.OpenDoors' Postfix.");

                //not at POI check for not triggering during at POI gameplay
                if (!arrivingToPOI && !train.IsStoppedAtPOI)
                {
                    if (localDebug)
                    {
                        MelonLogger.Msg($"[NOT AT POI] Doors Opened. Attempting UpgradeRequest Read/Decode/Apply\n");
                        MelonLogger.Msg($"[SERVER:BEFORE] __instance.Credits: {__instance.Credits}");
                        MelonLogger.Msg($"[SERVER:BEFORE] train.Credits: {train.Credits}\n");
                    }
                    int index = -1;

                    //DECODER
                    if (__instance.Credits >= 1000)
                    {
                        //DECODER
                        index = __instance.Credits % 1000;
                        __instance.Credits -= index;
                        __instance.Credits /= 1000;
                        if (__instance.Credits == 1) __instance.Credits -= 1; //0 check

                        train.Credits = __instance.Credits;
                    }

                    if (localDebug)
                    {
                        MelonLogger.Msg($"[SERVER:AFTER] Processing upgrade index: {index}");
                        MelonLogger.Msg($"[SERVER:AFTER] __instance.Credits: {__instance.Credits}");
                        MelonLogger.Msg($"[SERVER:AFTER] train.Credits: {train.Credits}\n");
                    }
                    //no request
                    if (index == -1) return;

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
                    ApplyUpgradesServer();
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
                if (localDebug) MelonLogger.Msg($"[RANSACKER] Successfully buffed {buffedCount}/{spawnedItems.Length} loot items by {multiplier*10}x lucky multiplier.");

                if (localDebug) MelonLogger.Msg($"All World UpgradesSet.\n");
            }
        }


        public static void ApplyUpgradesServer()
        {


            // get my player
            FFPlayer myPlayer = SteamIDUses.findMyPlayer(Core.localSteamID);


            // parse train name
            string rawData = train.gameObject.name.Substring(4); // strip "UPG:"
            string[] levelStrings = rawData.Split(',');


            // update upgrades based on train name
            for (int i = 0; i < upgrades.Count && i < levelStrings.Length; i++)
            {
                if (int.TryParse(levelStrings[i], out int parsedLvl))
                {
                    upgrades[i].upgLvl = parsedLvl;
                }
            }

            if (localDebug) MelonLogger.Msg($"Parsed Train Name.\n");

            //find applicable objects
            findWorldObjects();
            if (localDebug) MelonLogger.Msg($"Found World Objects.\n");


            // apply upgrades
            applyUpgradeEvader(
                upgrades[0].upgLvl,
                upgrades[0].upgScaler,
                myPlayer
            );

            applyUpgradeLurker(
                upgrades[1].upgLvl,
                upgrades[1].upgScaler,
                myPlayer
            );

            applyUpgradeHacker(
                upgrades[2].upgLvl,
                upgrades[2].upgScaler,
                myPlayer
            );


            if (localDebug) MelonLogger.Msg($"All upgrades applied.");
        }

        private static void findWorldObjects()
        {
            //hacker
            equipment = UnityEngine.Object.FindObjectsOfType<FFEquipment>();

        }

        private static void applyUpgradeEvader(int lvl, float scaler, FFPlayer player)
        {
            if (lvl == 0) return;
            float statUp = 1 + (lvl * (scaler / 100));

            player.maxStableMoveSpeed = player.StartingMaxStableMoveSpeed * statUp;
            player.staminaDrainRate = player.startingStaminaDrainRate / statUp;
        }


        private static void applyUpgradeLurker(int lvl, float scaler, FFPlayer player)
        {
            //TODO
        }


        private static void applyUpgradeHacker(float lvl, float scaler, FFPlayer player)
        {
            if (lvl == 0) return;

            if (localDebug) MelonLogger.Msg($"[BEFORE] mainMarker.paint = {mainMarker.paint}");
            if (localDebug) MelonLogger.Msg($"[BEFORE] mainBoltcutter.maxDurability = {mainBoltcutter.maxDurability}");
            if (localDebug) MelonLogger.Msg($"[BEFORE] mainSledgehammer.maxDurability = {mainSledgehammer.maxDurability}");
            if (localDebug) MelonLogger.Msg($"[BEFORE] mainStunlight.maxDurability = {mainStunlight.maxDurability}");

            if (localDebug) MelonLogger.Msg($"[BEFORE] mainGlowstick.light.range = {mainGlowstick.light.range}");
            if (localDebug) MelonLogger.Msg($"[BEFORE] mainLantern.light.range = {mainLantern.light.range}");
            if (localDebug) MelonLogger.Msg($"[BEFORE] mainFlashlight.light.range = {mainFlashlight.light.range}\n");

            if (localDebug) MelonLogger.Msg($"Applying Hacker Buff\n");

            //use equipment
            float statUp = (1 + (lvl * (scaler / 100)));
            int paintUseIncrease = (int)Math.Ceiling(originalItemDurabilityValues[0] * statUp) - originalItemDurabilityValues[0];
            int boltUseIncrease = (int)Math.Ceiling(originalItemDurabilityValues[1] * statUp) - originalItemDurabilityValues[1];
            int sledgeUseIncrease = (int)Math.Ceiling(originalItemDurabilityValues[2] * statUp) - originalItemDurabilityValues[2];
            int stunUseIncrease = (int)Math.Ceiling(originalItemDurabilityValues[3] * statUp) - originalItemDurabilityValues[3];

            mainMarker.paint = originalItemDurabilityValues[0] + paintUseIncrease;
            mainBoltcutter.maxDurability = originalItemDurabilityValues[1] + boltUseIncrease;
            mainSledgehammer.maxDurability = originalItemDurabilityValues[2] + sledgeUseIncrease;
            mainStunlight.maxDurability = originalItemDurabilityValues[3] + stunUseIncrease;

            if (localDebug) MelonLogger.Msg($"[AFTER] mainMarker.paint = {mainMarker.paint}; +{paintUseIncrease} charges");
            if (localDebug) MelonLogger.Msg($"[AFTER] mainBoltcutter.maxDurability = {mainBoltcutter.maxDurability}; +{boltUseIncrease} charges");
            if (localDebug) MelonLogger.Msg($"[AFTER] mainSledgehammer.maxDurability = {mainSledgehammer.maxDurability}; +{sledgeUseIncrease} charges");
            if (localDebug) MelonLogger.Msg($"[AFTER] mainStunlight.maxDurability = {mainStunlight.maxDurability}; +{stunUseIncrease} charges");


            //light equipment
            double glowIncrease = (double)Math.Ceiling(originalItemLightValues[0] * statUp) - originalItemLightValues[0];
            double lanternIncrease = (double)Math.Ceiling(originalItemLightValues[1] * statUp) - originalItemLightValues[1];
            double flashIncrease = (double)Math.Ceiling(originalItemLightValues[2] * statUp) - originalItemLightValues[2];

            mainGlowstick.light.range = (float)(originalItemLightValues[0] + glowIncrease);
            mainLantern.light.range = (float)(originalItemLightValues[1] + lanternIncrease);
            mainFlashlight.light.range = (float)(originalItemLightValues[2] + flashIncrease);

            if (localDebug) MelonLogger.Msg($"[AFTER] mainGlowstick.light.range = {mainGlowstick.light.range}; +{glowIncrease}%");
            if (localDebug) MelonLogger.Msg($"[AFTER] mainLantern.light.range = {mainLantern.light.range}; +{lanternIncrease}%");
            if (localDebug) MelonLogger.Msg($"[AFTER] mainFlashlight.light.range = {mainFlashlight.light.range}; +{flashIncrease}%\n");

            //maybe special could be a chance to not use durability for tools

        }


    }
}

