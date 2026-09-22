using HarmonyLib;
using Il2CppFishNet;
using Il2CppFishNet.Example.ColliderRollbacks;
using Il2CppFishNet.Object;
using Il2Cppmadeinfairyland.fairyengine.actor;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.butcher;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Carrier;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Mist;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Packrat;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.shambler;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.theabandonedone;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.ventress;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.equipment;
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

        //lurker init
        //enemies
        public static FFAICore[] enemyAIRadius;
        public static FFAICore[] enemyAIDelay;

        public static FFSpiderAI mainSpider;
        public static FFVentressAI mainVentress;
        public static FFButcherAI mainNButcher;
        public static FFForsakenAI mainForsaken;
        public static FFShambler mainShambler;
        public static FFTheAbandonedOneAI mainAbandoned;
        public static FFForsakenAI mainPatient;
        public static FFCarrierAI mainCarrier;
        public static FFButcherAI mainButcher;
        public static FFShambler mainNShambler;
        public static FFSpiderAI mainNSpider;
        public static FFMistAI mainShepherd;
        public static FFPackratAI mainPackrat;


        //old values
        public static float[] initDetectRadius;
        public static int[] initDetectChance;
        public static float[] initDetectDelay;

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

                    //update images
                    string textureName = upgrades[i].upgName + (parsedLvl+1).ToString() + "-crt";
                    upgrades[i].upgImg = UpgradeInit.loadTextures(textureName);
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

            //forerunner
            if (upgrades[4].upgLvl >= 1) hour = 5;
            if (upgrades[4].upgLvl >= 2) brokenMinChance = 5;
            if (upgrades[4].upgLvl >= 3) hour = 4;
            if (upgrades[4].upgLvl >= 4) brokenMinChance = 12;
            if (upgrades[4].upgLvl >= 5)
            {
                hour = 3;
                brokenMinConsecutive = true;
            }

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

            float statUpSpeed = 1;
            float statUpStam = 1;

            //use equipment
            if (lvl >= 1) statUpSpeed = 1.10f;
            if (lvl >= 2) statUpStam = 1.10f;
            if (lvl >= 3) statUpSpeed = 1.15f;
            if (lvl >= 4) statUpStam = 1.15f;
            if (lvl >= 5)
            {
                statUpSpeed = 1.35f;
                statUpStam = 1.35f;
                player.crawlMoveSpeedSubtractive = 1.23f;
            }
            
            //upgrade
            player.maxStableMoveSpeed = player.StartingMaxStableMoveSpeed * statUpSpeed;
            player.staminaDrainRate = player.startingStaminaDrainRate / statUpStam;
        }


        private static void applyUpgradeLurker(int lvl, float scaler, FFPlayer player)
        {
            float statUpCrouch = 1;
            float statDistReduce = 1;



            foreach (var value in initDetectRadius ) MelonLogger.Msg($"[LURKER] OLD Detect Radius: [{value}]");
            MelonLogger.Msg($"\n");
            foreach (var value in initDetectChance) MelonLogger.Msg($"[LURKER] OLD Search Chance: [{value}]");
            MelonLogger.Msg($"\n");
            foreach (var value in initDetectDelay) MelonLogger.Msg($"[LURKER] OLD Detect Delay: [{value}]");
            MelonLogger.Msg($"\n");

            //use equipment
            if (lvl >= 1) statUpCrouch = 0.963f; //from 1.2; 1.25x
            if (lvl >= 2) statDistReduce = 0.85f;
            if (lvl >= 3) statUpCrouch = 0.608f; //from 1.2; 1.5x
            if (lvl >= 4) statDistReduce = 0.65f;
            if (lvl >= 5)
            {
                statUpCrouch = 0.085f; //from 1.2; 2x
                statDistReduce = 0.60f;
                //landmine immunity (in harmony)
            }

            //upgrade
                //player
                player.crouchMoveSpeedSubtractive = statUpCrouch;


            //monsters
                //Each monster's player detection range, chance, and frequency reduced by X%
                FFAICore[] activeEnemies = new FFAICore[]
                {
                    mainSpider, mainVentress, mainNButcher, mainForsaken,
                    mainShambler, mainAbandoned, mainPatient, mainCarrier,
                    mainButcher, mainNShambler, mainShepherd, mainPackrat
                };

                //monster detect radius
                for (int i = 0; i < activeEnemies.Length; i++)
                {
                    activeEnemies[i].checkFarPlayerRadius = (int)Math.Ceiling(initDetectRadius[i] * statDistReduce);
                    activeEnemies[i].checkFarPlayerChance = (int)Math.Ceiling(initDetectChance[i] * statDistReduce);
                    activeEnemies[i].checkFarPlayersDelay = (int)Math.Ceiling(initDetectDelay[i] * statDistReduce);
                }

                foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Detect Radius: [{enemy.checkFarPlayerRadius}]");
                MelonLogger.Msg("\n");
                foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Search Chance: [{(int)enemy.checkFarPlayerChance}]");
                MelonLogger.Msg("\n");
                foreach (var enemy in activeEnemies) MelonLogger.Msg($"[LURKER] NEW Detect Delay: [{enemy.checkFarPlayersDelay}]");
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

    float statUp = 1;
    float statUpFlash = 1;

    //use equipment
    if (lvl >= 1) statUp = 1.20f;
    if (lvl >= 2) statUpFlash = 1.25f;
    if (lvl >= 3) statUp = 1.50f;
    if (lvl >= 4) statUpFlash = 1.60f;
    if (lvl >= 5)
    {
        statUp = 2f;
        statUpFlash = 2f;
        //double tool chance (in harmony)
    }
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
    double glowIncrease = (double)Math.Ceiling(originalItemLightValues[0] * statUpFlash) - originalItemLightValues[0];
    double lanternIncrease = (double)Math.Ceiling(originalItemLightValues[1] * statUpFlash) - originalItemLightValues[1];
    double flashIncrease = (double)Math.Ceiling(originalItemLightValues[2] * statUpFlash) - originalItemLightValues[2];

    mainGlowstick.light.range = (float)(originalItemLightValues[0] + glowIncrease);
    mainLantern.light.range = (float)(originalItemLightValues[1] + lanternIncrease);
    mainFlashlight.light.range = (float)(originalItemLightValues[2] + flashIncrease);

    if (localDebug)
    {
        MelonLogger.Msg($"[AFTER] mainGlowstick.light.range = {mainGlowstick.light.range}; +{glowIncrease}%");
        MelonLogger.Msg($"[AFTER] mainLantern.light.range = {mainLantern.light.range}; +{lanternIncrease}%");
        MelonLogger.Msg($"[AFTER] mainFlashlight.light.range = {mainFlashlight.light.range}; +{flashIncrease}%\n");
    }


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

