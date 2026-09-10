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

        //store
        public static int originalCredits;

        //debug
        public static bool localDebug = true;
        public static int ransackerApplied = 0;

        public static FFEquipment[] equipment;


        public static void ServerApplyUpgrade(int index)
        {

            if (localDebug) MelonLogger.Msg($"Triggering train.OpenDoors & requesting credits * 1000.");
            // open doors for all players on server 
            //ENCODER
            if (train.Credits == 0) train.Credits++; //0 check

            //index
            // 9 = server upg sync
            // 8 = server credit sync
            // 0-4 = specific server upgrade (!)
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



        public static void ApplyUpgradesServer()
        {
            //overwrite save file upgrade list
            //Host only overwrites save
            if (SteamIDUses.IsHost(localSteamID) && playerInitSettledSettled)
            {
                string savePath = System.IO.Path.Combine(Application.dataPath, "../UserLibs/Saves", currentHover + ".txt");
                string contentToWrite = string.Join("", upgrades.Take(5).Select(u => u.upgLvl));

                System.IO.File.WriteAllText(savePath, contentToWrite);
                MelonLogger.Msg($"OVERWROTE {currentHover}.\n");
            }

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

            if (localDebug)
            {
                MelonLogger.Msg($"[BEFORE] mainMarker.paint = {mainMarker.paint}");
                MelonLogger.Msg($"[BEFORE] mainBoltcutter.maxDurability = {mainBoltcutter.maxDurability}");
                MelonLogger.Msg($"[BEFORE] mainSledgehammer.maxDurability = {mainSledgehammer.maxDurability}");
                MelonLogger.Msg($"[BEFORE] mainStunlight.maxDurability = {mainStunlight.maxDurability}");

                MelonLogger.Msg($"[BEFORE] mainGlowstick.light.range = {mainGlowstick.light.range}");
                MelonLogger.Msg($"[BEFORE] mainLantern.light.range = {mainLantern.light.range}");
                MelonLogger.Msg($"[BEFORE] mainFlashlight.light.range = {mainFlashlight.light.range}\n");

                MelonLogger.Msg($"Applying Hacker Buff\n");
            }

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

            if (localDebug)
            {
                MelonLogger.Msg($"[AFTER] mainMarker.paint = {mainMarker.paint}; +{paintUseIncrease} charges");
                MelonLogger.Msg($"[AFTER] mainBoltcutter.maxDurability = {mainBoltcutter.maxDurability}; +{boltUseIncrease} charges");
                MelonLogger.Msg($"[AFTER] mainSledgehammer.maxDurability = {mainSledgehammer.maxDurability}; +{sledgeUseIncrease} charges");
                MelonLogger.Msg($"[AFTER] mainStunlight.maxDurability = {mainStunlight.maxDurability}; +{stunUseIncrease} charges");
            }

            //light equipment
            double glowIncrease = (double)Math.Ceiling(originalItemLightValues[0] * statUp) - originalItemLightValues[0];
            double lanternIncrease = (double)Math.Ceiling(originalItemLightValues[1] * statUp) - originalItemLightValues[1];
            double flashIncrease = (double)Math.Ceiling(originalItemLightValues[2] * statUp) - originalItemLightValues[2];

            mainGlowstick.light.range = (float)(originalItemLightValues[0] + glowIncrease);
            mainLantern.light.range = (float)(originalItemLightValues[1] + lanternIncrease);
            mainFlashlight.light.range = (float)(originalItemLightValues[2] + flashIncrease);

            if (localDebug)
            {
                MelonLogger.Msg($"[AFTER] mainGlowstick.light.range = {mainGlowstick.light.range}; +{glowIncrease}%");
                MelonLogger.Msg($"[AFTER] mainLantern.light.range = {mainLantern.light.range}; +{lanternIncrease}%");
                MelonLogger.Msg($"[AFTER] mainFlashlight.light.range = {mainFlashlight.light.range}; +{flashIncrease}%\n");
            }

            //maybe special could be a chance to not use durability for tools

        }

        public static void SyncUpgradesAcrossServer()
        {
            //credit save
            originalCredits = train.Credits;

            // #1 Server Upg Sync
            //


            //index dictionary
            // 9 = server upg sync (!)
            // 8 = server credit sync
            // 0-4 = specific server upgrade
            int index = 9; //server upg sync index

            // parse train name
            string rawData = train.gameObject.name.Substring(4); // strip "UPG:"
            string[] levelStrings = rawData.Split(',');

            int compressedUpgInt = 10;

            // update upgrades based on train name
            for (int i = upgrades.Count-1; i >= 0; i--)
            {
                if (i < levelStrings.Length && int.TryParse(levelStrings[i], out int parsedLvl))
                {
                    compressedUpgInt += parsedLvl;
                    compressedUpgInt *= 10;
                }
            }

            //complete encoded message for door trigger
            int encoded = compressedUpgInt + index;
            encoded -= train.Credits; //credits addition bug workaround

            if (localDebug)
            {
                MelonLogger.Msg($"[ENCODE]");
                MelonLogger.Msg($"Credits: {train.Credits}");
                MelonLogger.Msg($"Index: {index}");
                MelonLogger.Msg($"Encoded: {encoded}");
            }
            train.RpcWriter___Server_svr_RequestAddCredits_3316948804(encoded);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(true);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(false); //simultaneously open and close doors for trigger

            triggerCreditUpdateSoon = true;

        }

        public static void SyncUpgradesAcrossServer2()
        {

            // #2 Server Credit Sync
            // Credits are messed up across the server now from step 1
            // Sync them back using previously noted original credits and new index

            //index dictionary
            // 9 = server upg sync
            // 8 = server credit sync (!)
            // 0-4 = specific server upgrade
            int index = 8; //skip index
            int encoded2 = (originalCredits * 10) + index - 1; //idek why 3500 is being added. ugp cost?
                                                           // that is ONLY the evader init cost

            if (localDebug)
            {
                MelonLogger.Msg($"[ENCODE]");
                MelonLogger.Msg($"Credits: {train.Credits}");
                MelonLogger.Msg($"Index: {index}");
                MelonLogger.Msg($"Encoded: {encoded2}");
            }
            train.RpcWriter___Server_svr_RequestAddCredits_3316948804(encoded2);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(true);
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(false); //simultaneously open and close doors for trigger
        }
    }
}

