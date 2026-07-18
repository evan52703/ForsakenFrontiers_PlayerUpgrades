using HarmonyLib;
using Il2CppFishNet;
using Il2CppFishNet.Example.ColliderRollbacks;
using Il2CppFishNet.Object;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
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
        public static int indexOfUpgrade = -1;
        public static int costOfUpgrade = -1;
        public static int testVar = 100;

        public static bool localDebug = false;


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
                //FORERUNNER
                else
                {
                    if (localDebug) MelonLogger.Msg($"[AT POI] Doors Opened. Attempting Forerunner buff.\n");

                    if (SteamIDUses.IsHost(localSteamID) && !forerunnerUpgradeSet)
                    {
                        //adjust world time based on Forerunner lvl
                        world.Hour -= upgrades[4].upgLvl;

                        //tick applied var
                        forerunnerUpgradeSet = true;
                        if (localDebug) MelonLogger.Msg($"Forerunner Upg Set.\n");
                    }
                    else
                    {
                        if (localDebug) MelonLogger.Msg($"Not Host/Already applied Forerunner Buff.\n");
                    }
                }
            }
        }


        private static void ApplyUpgradesServer()
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


            // reset vars
            resetAllVars(myPlayer);


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

            applyUpgradeTinkerer(
                upgrades[2].upgLvl,
                upgrades[2].upgScaler,
                myPlayer
            );

            applyUpgradeRummager(
                upgrades[3].upgLvl,
                upgrades[3].upgScaler,
                myPlayer
            );


            if (localDebug) MelonLogger.Msg($"All upgrades applied.");
        }



        //################################################################################################


        private static void resetAllVars(FFPlayer player)
        {
            player.maxStableMoveSpeed = player.StartingMaxStableMoveSpeed;
            player.staminaDrainRate = player.startingStaminaDrainRate;
        }


        private static void applyUpgradeEvader(int lvl, float scaler, FFPlayer player)
        {
            float statUp = 1 + (lvl * (scaler / 100));

            player.maxStableMoveSpeed *= statUp;
            player.staminaDrainRate /= statUp;
        }


        private static void applyUpgradeLurker(int lvl, float scaler, FFPlayer player)
        {
            //TODO
        }


        private static void applyUpgradeTinkerer(int lvl, float scaler, FFPlayer player)
        {
            //TODO
        }


        private static void applyUpgradeRummager(int lvl, float scaler, FFPlayer player)
        {
            //TODO
        }
    }
}


//// upgrade requested user gets their local machine
//if (requestSteamID == localSteamID)
//{
//    train.indexHolder = indexOfUpgrade;
//}

//requestSteamID = 0;


//if (train.indexHolder < 0 || train.indexHolder >= upgrades.Count)
//    return;


//// get and level up upgrade
//var upgrade = upgrades[train.indexHolder];

//if (upgrade.upgLvl >= upgrade.upgLvlMax)
//    return;


//// adjust costs
//int cost = upgrade.initCost + upgrade.costScaler * upgrade.upgLvl;

//if (train.Credits < cost)
//    return;


//costOfUpgrade = cost;


//// update local train level
//upgrade.upgLvl++;


//MelonLogger.Msg($"Train Name before: {train.gameObject.name}");
//MelonLogger.Msg($"Credits before: {train.Credits}");


//// Change server name of train
//train.gameObject.name =
//    "UPG:" +
//    string.Join(",", upgrades.Select(u => u.upgLvl));


//// Adjust server credits
//train.Credits -= costOfUpgrade;


// apply upgrades locally