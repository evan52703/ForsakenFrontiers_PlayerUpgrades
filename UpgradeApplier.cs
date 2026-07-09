using HarmonyLib;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.forsakenfrontiers.demo;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2Cppmadeinfairyland.forsakenfrontiers.ui.PauseMenu.PlayerList;
using Il2CppSystem.Collections.Generic;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.PlayerLoop;
using static PlayerUpgrades.Core;

namespace PlayerUpgrades
{

    public class UpgradeApplier
    {
        public static int indexOfUpgrade = -1;
        public static int costOfUpgrade = -1;

        public static void ServerApplyUpgrade(int index)
        {
            //set global index identifier
            indexOfUpgrade = index;

            //if (!Core.IsHost()) return;
            if (index < 0 || index >= upgrades.Count) return;

            //get and level up upgrade
            var upgrade = upgrades[index];
            if (upgrade.upgLvl >= upgrade.upgLvlMax) return;

            //adjust costs
            int cost = upgrade.initCost + upgrade.costScaler * upgrade.upgLvl;
            if (train.Credits < cost) return;

            //set global index identifier
            costOfUpgrade = cost;

            //Update local train level to set to train name on server
            upgrade.upgLvl++;

            //logger breiefing
            MelonLogger.Msg($"Attempting to trigger FFTrain.GetEndGameStats.\n");

            //trigger method to trigger harmony
            train.GetEndGameStats();
        }

        [HarmonyPatch(typeof(FFTrain), nameof(FFTrain.GetEndGameStats))]
        public static class GameStatsUpgradeTrigger
        {
            [HarmonyPostfix]
            public static void UpdateTrainNameFromServer(FFTrain __instance)
            {
                //logger before n after
                MelonLogger.Msg($"Train Name before: {train.gameObject.name}.");
                MelonLogger.Msg($"Credits before: {train.Credits}.\n");

                //SERVER CHANGES!
                //Change server name of train
                train.gameObject.name = "UPG:" + string.Join(",", upgrades.Select(u => u.upgLvl));
                //Adjust server credits
                train.Credits -= costOfUpgrade;


                //logger before n after
                MelonLogger.Msg($"Train Name after: {train.gameObject.name}.");
                MelonLogger.Msg($"Credits after: {train.Credits}.\n");

                //Call this local command for each mod to read the updated train name and apply their
                //updated upgrade levels
                ApplyUpgradesServer();

            }

        }
        // Remove the parameter entirely
        private static void ApplyUpgradesServer()
        {
            //get my player
            FFPlayer myPlayer = SteamIDUses.findMyPlayer(Core.localSteamID);


            //parse train name to upgrade string
            string rawData = train.gameObject.name.Substring(4); // strip "UPG:"
            string[] levelStrings = rawData.Split(',');

            //set upgrades based on pulled upgrade string
            for (int i = 0; i < upgrades.Count && i < levelStrings.Length; i++)
            {
                if (int.TryParse(levelStrings[i], out int parsedLvl))
                {
                    upgrades[i].upgLvl = parsedLvl;
                }
            }

            //finally, with each level updated locally, reset vars and apply new upgrades

            //reset my vars
            resetAllVars(myPlayer);

            //apply upgrades
            applyUpgradeEfficient(upgrades[0].upgLvl, upgrades[0].upgScaler, myPlayer);
            applyUpgradeElusive(upgrades[1].upgLvl, upgrades[1].upgScaler, myPlayer);
            applyUpgradeTinkerer(upgrades[2].upgLvl, upgrades[2].upgScaler, myPlayer);
            applyUpgradeRummager(upgrades[3].upgLvl, upgrades[3].upgScaler, myPlayer);


            MelonLogger.Msg($"All upgrades applied.\n");
        }

        //################################################################################################

        private static void resetAllVars(FFPlayer player)
        {
            player.maxStableMoveSpeed = player.StartingMaxStableMoveSpeed;
            player.staminaDrainRate = player.startingStaminaDrainRate;
        }
        private static void applyUpgradeEfficient(int lvl, float scaler, FFPlayer player)
        {
            //apply changes
            float statUp = 1+(lvl*(scaler/100));
            player.maxStableMoveSpeed *= statUp;
            player.staminaDrainRate /= statUp;

        }
        private static void applyUpgradeElusive(int lvl, float scaler, FFPlayer player)
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
