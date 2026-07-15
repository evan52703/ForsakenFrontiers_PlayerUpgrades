using HarmonyLib;
using Il2CppFishNet;
using Il2CppFishNet.Example.ColliderRollbacks;
using Il2CppFishNet.Object;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
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


        public static void ServerApplyUpgrade(int index)
        {
            // set global index identifier
            indexOfUpgrade = index;

            MelonLogger.Msg($"Upgrade stuff adjusted. Triggering train.OpenDoors.");
            // open doors for all players on server
            train.RpcWriter___Server_svr_ToggleDoors_1140765316(true);
        }


        [HarmonyPatch(typeof(FFTrain), nameof(FFTrain.OpenDoors))]
        public static class ForerunnerUpgradeApplier
        {
            [HarmonyPostfix]
            public static void ForerunnerApplier(FFTrain __instance)
            {
                MelonLogger.Msg($"Entered 'OpenDoors' Postfix.\n");


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
                ApplyUpgradesServer();


                //FORERUNNER
                MelonLogger.Msg($"Doors Opened. Attempting Forerunner buff");


                if (SteamIDUses.IsHost(localSteamID) &&
                    !forerunnerUpgradeSet &&
                    train.IsStoppedAtPOI)
                {
                    MelonLogger.Msg($"Stopped at POI. I am Host. I set buffs");

                    world.Hour -= upgrades[4].upgLvl;
                    forerunnerUpgradeSet = true;
                }
            }
        }


        private static void ApplyUpgradesServer()
        {
            MelonLogger.Msg($"Train Name after: {train.gameObject.name}");
            MelonLogger.Msg($"Credits after: {train.Credits}\n");


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


            MelonLogger.Msg($"All upgrades applied.");
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