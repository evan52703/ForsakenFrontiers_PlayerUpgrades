using HarmonyLib;
using Il2CppFishNet.Broadcast;
using Il2Cppmadeinfairyland.fairyengine;
using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.datadeck;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.equipment;
using Il2Cppmadeinfairyland.forsakenfrontiers.hazards;
using Il2Cppmadeinfairyland.forsakenfrontiers.train;
using Il2Cppmadeinfairyland.forsakenfrontiers.ui.mainmenu;
using Il2CppSteamworks;
using Il2CppSystem.IO;
using Il2CppSystem.Runtime.Remoting.Messaging;
using MelonLoader;
using MelonLoader.Utils;
using System.IO;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;
using static Il2CppSystem.Array;
using ImageConversion = UnityEngine.ImageConversion;

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

        public static bool doNotProcessCreditsOrUpgrades = false;

        public static int settleTimer = 0;
        public static bool playerInitSettled = false;

        public static int triggerTimer = 0;
        public static bool triggerCreditUpdateSoon = false;

        //saves
        public static FairyCoreManager coreManager;
        public static FFSaveFileButton[] saveButtons;
        public static string currentHover;
        public static bool currentSaveFound;
        public static bool saveLoaded = false;
        public static int settleSettleTimer = 0;
        public static bool playerInitSettledSettled = false;
        public static FFSaveFileButton currentSave = null;

        //player
        FFPlayer[] players;
        public static int truePlayerCount = 0; //host only
        public static float playerCheckTimer;

        //################################################################################################################
        //Methods

        public override void OnSceneWasLoaded(int buildIndex, string sceneName)
        {
            if (sceneName == "Main Menu")
            {
               //main menu items init
                UpgradeInit.GetMainMenuItems();
                //waitingForSceneObjects = true;

                //Find Save Buttons
                coreManager = UnityEngine.Object.FindObjectOfType<FairyCoreManager>();
                saveButtons = UnityEngine.Object.FindObjectsOfType<FFSaveFileButton>();
                saveButtons = saveButtons.OrderBy(b => ExtractSaveNumber(b.saveFile)).ToArray();

                //display each save
                int count = 0;
                foreach (var save in saveButtons)
                {
                    MelonLogger.Msg($"{save.saveFile}");
                }

                /////////////////////////////////////
                /////
                /////   Now we have each save button in 'saveButtons'
                /////   and whether or not they are being used.
                /////   
                /////   How to make saving work:
                /////
                /////   1. Use On Update to find: any save file buttons having StartSelected as true
                /////   2. As soon as you do, break from that; then: 
                /////       Find the corresponding "SaveFile" string name of the StartSelected SaveFileButton.
                /////       EX: if SaveFileButton 1 has StartSelected & Found as true from On Update, fileName = SaveFound 
                /////
                /////           if (save.StartSelected)
                /////               if (save.Found)
                /////                   string saveName = SaveNames[saveWeAreTesting'sIndex]
                /////                   ***saveName = save_3*** = example
                /////          CRITICAL: store saveName in local var for step 5. so we dont have to search again
                /////                   LoadTrainNameInSave(saveName)
                /////
                /////   3. LoadTrainNameInSave function reads directory created file for save_3 when 
                /////       game last closed for that save, reads and stores train name in local variable.
                /////
                /////   4. When scene loads and train is initialized
                /////       - make it's name = the local variable from step 3.
                /////       - run OpenDoors Harmony with index == 9 for upgrade sync
                /////
                ///// ---------------------------------------------------------------------
                /////
                /////   5. On ingame close, read CRITICAL local var with saveName from step 3,
                /////       write to that save locally with current train name, then safely exit.
                /////   
                //////////////////////////////////////

            }

            int ExtractSaveNumber(string saveFileName)
            {
                //splits save_1 for example to 2 strings: "save" and "1"
                string[] parts = saveFileName.Split('_');

                //TryParse converts a string numeral to its 32bit int counterpart
                return int.TryParse(parts[1], out int id) ? id : 0;
            }

            if (sceneName == "Forsaken Frontiers")
            {
                //once scene loaded, allow OnUpdate()
                amIHost = SteamIDUses.IsHost(localSteamID);
                waitingForSceneObjects = true;
                inGame = false;
                MelonLogger.Msg($"Save File to Load: {currentHover}");
                MelonLogger.Msg($"Save File Found? {currentSaveFound}");

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

                settleTimer = 0;
                truePlayerCount = 0;
                playerInitSettled = false;

                saveLoaded = false;
                settleSettleTimer = 0;
                playerInitSettledSettled = false;
                currentSave = null;

            }
        }

        //triggers every frame
        public override void OnUpdate()
        {
            //detect save file
            if (!inGame)
            {
                foreach (var save in saveButtons)
                {
                    //any save
                    if (save.IsHovering && save.SaveFile != currentHover)
                    {
                        currentHover = save.saveFile;
                        currentSave = save;
                        MelonLogger.Msg($"Save file: '{currentHover}' hovered.");
                    }
                    //special case for deleting previous save and never touching other buttons
                    else if (save.IsHovering && save.SaveFile == currentHover)
                    {
                        currentHover = save.saveFile;
                        currentSave = save;
                        MelonLogger.Msg($"Save file: '{currentHover}' REhovered.");
                    }
                }
                if (currentSave != null)
                {
                    //check every frame
                    currentSaveFound = currentSave.FoundSave;
                    MelonLogger.Msg($"Save found? '{currentSaveFound}'");
                }
            }


            // load world and train into vars
            if (waitingForSceneObjects)
            {
                world = UnityEngine.Object.FindObjectOfType<FFWorld>();
                train = UnityEngine.Object.FindObjectOfType<FFTrain>();

                if (world != null && train != null)
                {
                    upgrades = UpgradeInit.initUpgrades(upgrades);

                    if (!train.name.StartsWith("UPG:"))
                    {
                        train.name = "UPG:0,0,0,0,0";
                    }
                    else
                    {
                        UpgradeApplier.ApplyUpgradesServer();
                    }

                    waitingForSceneObjects = false; //set to false to avoid infinite loop
                    inGame = true; // run mod features
                }
                return;
            }

            if (!inGame || train == null) return;

            //forerunner var reset
            if (!train.IsStoppedAtPOI && worldGenUpgradesSet)
            {
                worldGenUpgradesSet = false;
            }

            //player detector
            playerCheckTimer += Time.deltaTime;
            if (!train.IsStoppedAtPOI && playerCheckTimer >= 1.0f)
            {
                playerCheckTimer = 0f;
                players = UnityEngine.Object.FindObjectsOfType<FFPlayer>();
            }

            if (settleTimer < 6) settleTimer++;
            else playerInitSettled = true;

            if (inGame && playerInitSettled && !train.IsStoppedAtPOI)
            {
                //check if player count changes every frame
                int playerCountCheck = players.Length;
                if (playerCountCheck != truePlayerCount)
                {
                    //if it does, trigger host/non-host respective changes
                    //Host trigger
                    if (SteamIDUses.IsHost(localSteamID))
                    {
                        if (playerCountCheck > truePlayerCount) MelonLogger.Msg($"[HOST] PlayerCount Increased from [{truePlayerCount} -> {playerCountCheck}] ");
                        else if (playerCountCheck < truePlayerCount) MelonLogger.Msg($"[HOST] PlayerCount Decreased from [{truePlayerCount} -> {playerCountCheck}] ");

                        //Update true player count
                        truePlayerCount = playerCountCheck;

                        //Door Trigger Code 9: Sync current server upgrade levels across all players
                        UpgradeApplier.SyncUpgradesAcrossServer();

                        //Update local upgrade cost values based on truePlayerCount
                        UpgradeInit.UpdateCostsAccToPlayerCount(truePlayerCount);
                        return;
                    }
                    //Non-Host trigger
                    else
                    {
                        if (playerCountCheck > truePlayerCount) MelonLogger.Msg($"PlayerCount Increased from [{truePlayerCount} -> {playerCountCheck}] ");
                        else if (playerCountCheck < truePlayerCount) MelonLogger.Msg($"PlayerCount Decreased from [{truePlayerCount} -> {playerCountCheck}] ");

                        //Update true player count
                        truePlayerCount = playerCountCheck;

                        //Update local upgrade cost values based on truePlayerCount
                        UpgradeInit.UpdateCostsAccToPlayerCount(truePlayerCount);
                        return;
                    }
                }
            }

            if (triggerCreditUpdateSoon)
            {
                triggerTimer++;
                if (triggerTimer >= 30)
                {
                    MelonLogger.Msg($"\n TRIGGER TIMER 30\n");
                    UpgradeApplier.SyncUpgradesAcrossServer2();
                    triggerCreditUpdateSoon = false;
                    triggerTimer = 0;
                }
            }

            if (settleSettleTimer < 60) settleSettleTimer++;
            else playerInitSettledSettled = true;

            //save loader
            if (playerInitSettledSettled && currentSaveFound && !saveLoaded)
            {
                saveLoaded = true;
                MelonLogger.Msg("Entered SaveLoader WITH SAVE...");

                //Host only loads save
                if (SteamIDUses.IsHost(localSteamID))
                {
                    string path = System.IO.Path.Combine(Application.dataPath, "../UserLibs/Saves", currentHover + ".txt");

                    if (!System.IO.File.Exists(path))
                    {
                        MelonLogger.Msg("[!] FILE NOT FOUND");
                        return;
                    }

                    string fileContent = System.IO.File.ReadAllText(path);

                    int[] numbers = Regex.Matches(fileContent, @"\d")
                            .Cast<Match>()
                            .Select(m => int.Parse(m.Value))
                            .Take(5)
                            .ToArray();

                    for (var i = 0; i < 5; i++)
                    {
                        upgrades[i].upgLvl = numbers[i];
                    }

                    // Change server name of train
                    train.gameObject.name =
                    "UPG:" +
                        string.Join(",", upgrades.Select(u => u.upgLvl));

                    MelonLogger.Msg($"[!] UPGRADES FOUND: {numbers[0]},{numbers[1]},{numbers[2]},{numbers[3]},{numbers[4]}");
                    MelonLogger.Msg($"[!] TRAIN NAME: {train.gameObject.name}");
                    UpgradeApplier.SyncUpgradesAcrossServer();
                }
                else
                {
                    MelonLogger.Msg("NOT HOST");
                }

            }

            //save doesnt exist and file has upgs in it
            else if (playerInitSettledSettled && !currentSaveFound && !saveLoaded)
            {
                saveLoaded = true;
                MelonLogger.Msg("Entered SaveLoader WITHOUT SAVE...");

                //Host only loads save
                if (SteamIDUses.IsHost(localSteamID))
                {
                    string path = System.IO.Path.Combine(Application.dataPath, "../UserLibs/Saves", currentHover + ".txt");

                    string contentToWrite = "00000";
                    System.IO.File.WriteAllText(path, contentToWrite);

                    MelonLogger.Msg($"[!] {currentHover} RESET");
                }
                else
                {
                    MelonLogger.Msg("NOT HOST");
                }

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
        public static void UpdateCostsAccToPlayerCount(int playerCount)
        {
            float costScalePercent = 0.25f;

            float costScaleMult = 1f + ((playerCount - 1) * costScalePercent);

            foreach (var upg in upgrades)
            {
                upg.initCost *= (int)costScaleMult;
                upg.costScaler *= (int)costScaleMult;
            }
        }
    }

}