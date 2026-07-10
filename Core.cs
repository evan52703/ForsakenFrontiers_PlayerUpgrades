using HarmonyLib;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using MelonLoader.Utils;
using UnityEngine;
using ImageConversion = UnityEngine.ImageConversion;

[assembly: MelonInfo(typeof(PlayerUpgrades.Core), "PlayerUpgrades", "0.3.0", "evan527", null)]
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
        public static FFTrain train;
        public static FFWorld world;
        private bool waitingForSceneObjects = false;

        //################################################################################################################
        //Methods

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Forsaken Frontiers")
            {
                //once scene loaded, allow OnUpdate()
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
                    train.name = "UPG:0,0,0,0,0";
                    upgrades = UpgradeInit.initUpgrades(upgrades);

                    waitingForSceneObjects = false; //set to false to avoid infinite loop
                    inGame = true; // run mod features
                }
                return;
            }

            //enable/disable menu
            if (Input.GetKeyDown(menuKey) && inGame)
            {
                _menuEnabled = !_menuEnabled;
            }

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