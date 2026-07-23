using HarmonyLib;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.forsakenfrontiers.hazards;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using ImageConversion = UnityEngine.ImageConversion;
using Il2CppFishNet.Broadcast;

[assembly: MelonInfo(typeof(PlayerUpgrades.Core), "PlayerUpgrades", "0.5.0", "evan527", null)]
[assembly: MelonGame("made in fairyland", "Forsaken Frontiers")]


namespace PlayerUpgrades
{
    public class Core : MelonMod
    {
        //################################################################################################################
        //Vars

        //upgrade var init
        public static int[] UpgradeLevels = new int[5];
        private Upgrade upgradeSelected = null;
        private int x_adj = 10;
        private int y_adj = 20;
        public static bool upgradePrepped = false;
        public static List<Upgrade> upgrades;
        public static List<int> GlobalUpgradeLevels = new List<int> { 0, 0, 0, 0, 0 };

            //worldgen
            public static bool worldGenUpgradesSet = false;

        //upgrade class
        public class Upgrade
        {
            public string upgName;
            public string upgDcr;
            public Texture2D upgImg;
            public int upgLvl;
            public int upgLvlMax;
            public Color boxColor;
            public int initCost;
            public int costScaler;
            public float upgScaler;
        };

        //menus
        private bool _menuEnabled = false;
        public bool inGame = false;
        private KeyCode menuKey = KeyCode.F2;
        private KeyCode testingKey = KeyCode.T;

        //steam
        public static ulong localSteamID
        {
            get
            {
                try
                {
                    return Il2CppSteamworks.SteamUser.GetSteamID().m_SteamID;
                }
                catch
                {
                    return 0;
                }
            }
        }

        //other vars
        public static bool debug = false;
        public static FFTrain train;
        public static FFWorld world;
        private bool waitingForSceneObjects = false;
        public static bool arrivingToPOI = false;
        public static bool amIHost = false;

        //################################################################################################################
        //Methods

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main Menu")
            {
                //get non-clone objects
                //also sets boltcutter var to test
                UpgradeInit.GetMainMenuItems();
                MelonLogger.Msg("Items set.");
                //UpgradeInit.GetMainMenuLootItems();
                //MelonLogger.Msg("Loot Items set.");
            }
            if (sceneName == "Forsaken Frontiers")
            {
                //once scene loaded, allow OnUpdate()
                amIHost = SteamIDUses.IsHost(localSteamID);
                MelonLogger.Msg($"Host?: {amIHost}");
                waitingForSceneObjects = true;
                inGame = false;
            }
            else
            {
                //reset vars if not in-game
                train = null;
                world = null;
                inGame = false;
                upgradeSelected = null;
                _menuEnabled = false;
                waitingForSceneObjects = false;
            }
        }

        //triggers every frame
        public override void OnUpdate()
        {
            // load world and train into vars
            if (waitingForSceneObjects)
            {
                world = UnityEngine.Object.FindObjectOfType<FFWorld>();
                train = UnityEngine.Object.FindObjectOfType<FFTrain>();

                if (world != null && train != null)
                {
                    MelonLogger.Msg($"TRAIN_NAME: {train.name}");
                    // Only format train name if it hasn't been set yet
                    if (!train.name.StartsWith("UPG:"))
                    {
                        train.name = "UPG:0,0,0,0,0";
                        upgrades = UpgradeInit.initUpgrades(upgrades);
                    }
                    //if train name exists, apply upgrades locally
                    else
                    {
                        UpgradeApplier.ApplyUpgradesServer();
                    }

                    waitingForSceneObjects = false; //set to false to avoid infinite loop
                    inGame = true; // run mod features
                }
                return;
            }
            if (! waitingForSceneObjects )

            if (Input.GetKeyDown(testingKey))
            {
                //get non-clone objects
                //also sets boltcutter var to test
                UpgradeInit.GetMainMenuItems();
                //UpgradeInit.GetMainMenuLootItems();
                MelonLogger.Msg("Objects set.");
            }

            if (!inGame || train == null) return;

            //forerunner var reset
            if (!train.IsStoppedAtPOI && worldGenUpgradesSet)
            {
                worldGenUpgradesSet = false;
            }


            //enable/disable menu
            if (Input.GetKeyDown(menuKey) && inGame)
            {
                _menuEnabled = !_menuEnabled;
            }
            //testing key
            if (Input.GetKeyDown(testingKey) && inGame)
            {
                if (SteamIDUses.IsHostList(localSteamID))
                {
                    MelonLogger.Msg("You the host chief.");
                }
                else
                {
                    MelonLogger.Msg("You no host.");
                }
            }

        }

        public override void OnGUI()
        {
            // Silent return. No console spam while waiting for components to load.
            if (!inGame || train == null)
            {
                return;
            }

            if (train.IsStoppedAtPOI)
            {
                _menuEnabled = false; //closes menu when not at POI
                upgradeSelected = null; //resets selection when not at POI
                GUI.Box(new Rect(x_adj, y_adj, 180, 50), "<b>Player Upgrades</b>\nUnavailable");
            }
            else if (!_menuEnabled)
            {
                GUI.Box(new Rect(x_adj, y_adj, 180, 50), $"<b>Player Upgrades</b>\n[{menuKey}] to Open");
            }
            else
            {
                // Hand off the work to our separate class
                UpgradeMenu.Draw(upgrades, ref upgradeSelected);
            }
        }
    }
}