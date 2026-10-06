using Il2Cppmadeinfairyland.forsakenfrontiers;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player.equipment;
using Il2CppSteamworks;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using static Il2CppSystem.Net.Http.Headers.Parser;
using static PlayerUpgrades.Core;
using System.IO;
using System.Reflection;
using System.IO;
using System.Linq;
using System.Reflection;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Packrat;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Mist;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.Carrier;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.theabandonedone;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.shambler;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.ventress;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai.butcher;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.ai;

namespace PlayerUpgrades
{
    internal static class UpgradeInit
    {
        public static string[] evaderDescriptions = {
            "All movement speed increased by 10%",
            "Sprinting stamina decreases 10% slower",
            "All movement speed increased by 12%\n(22% total)",
            "Sprinting stamina decreases 12% slower\n(22% total)",
            "Crawl speed increased by 300%\n\nAll movement speed increased by 8%\n(30% total)\n\nSprinting stamina decreases 8% slower\n(30% total)"
        };
        public static string[] lurkerDescriptions = {
            "+30% crouched movement speed",
            "Monster's player detection range, chance, and frequency reduced by 15%",
            "+45% crouched movement speed\n(75% total)",
            "Monster's player detection range, chance, and frequency reduced by 20%\n(35% total)",
            "Landmine and tripwire immunity\n\n+25% crouched movement speed\n(100% total)\n\nMonster's player detection range, chance, and frequency reduced by 5%\n(40% total)"
        };
        public static string[] supplierDescriptions = {
            "+30% tool equipment durability",
            "+25% light equipment range and brightness",
            "+45% tool equipment durability\n(75% total)",
            "+35% light equipment range and brightness\n(60% total)",
            "+50% chance for shop items purchased to be duplicated\n\n+75% tool equipment durability\n(150% total)\n\n+40% light equipment range and brightness\n(100% total)"
        };
        public static string[] ransackerDescriptions = {
            "Non-container loot items are 10% more valuable",
            "Loot containers contain up to 2 additional loot items\n(+0-2 possible)",
            "Non-container loot items are 15% more valuable\n(25% total)",
            "Loot containers contain up to 4 additional loot items\n(+0-4 possible)",
            "Non-container loot items have a 1% chance to have 500% value\n\nLoot containers contain at least 1 additional loot item\n(+1-4 possible)"
        };
        public static string[] forerunnerDescriptions = {
            "Arrive to each destination 1 hour earlier\n(6:00am -> 5:00am)",
            "+5% chance for a minute to not pass when it normally world\n(Cannot occur consecutively)",
            "Arrive to each destination 1 hour earlier\n(5:00am -> 4:00am)",
            "+7% chance for a minute to not pass when it normally world\n(12% total)\n(Cannot occur consecutively)",
            "Minutes not passing can occur consecutively\n\nArrive to each destination 1 hour earlier\n(4:00am -> 3:00am)"
        };

        //get all clonable item objects
        public static void GetMainMenuItems()
        {
            //uses
            UpgradeApplier.mainMarker = FindObjectByName<FFSprayTool>("SprayMark");
            UpgradeApplier.mainBoltcutter = FindObjectByName<FFBoltcutters>("Buoltcutters");
            UpgradeApplier.mainSledgehammer = FindObjectByName<FFSledgehammer>("SledgeHammer");
            UpgradeApplier.mainStunlight = FindObjectByName<FFStunLight>("StunLight");

            //lights
            UpgradeApplier.mainGlowstick = FindObjectByName<FFGlowstick>("Glowstick");
            UpgradeApplier.mainLantern = FindObjectByName<FFLantern>("Lantern");
            UpgradeApplier.mainFlashlight = FindObjectByName<FFFlashlight>("Flashlight");

            //other
            //UpgradeApplier.mainMedkit = FindObjectByName<FFEquipment>("FirstAidKit");
            UpgradeApplier.mainAdrenaline = FindObjectByName<FFAdrenalineShot>("Adrenaline Shot");
            UpgradeApplier.mainWalkie = FindObjectByName<FFWalkieTalkie>("Walkie Talkie");
            UpgradeApplier.mainFirecrackers = FindObjectByName<FFFirecrackers>("Firecrackers");
            UpgradeApplier.mainGasMask = FindObjectByName<FFGasmask>("Gasmask");
            UpgradeApplier.mainSoda = FindObjectByName<FFNoiseMaker>("Noisemaker (soda can)");
            UpgradeApplier.mainDynamite = FindObjectByName<FFDynamite>("Dynamite");

        }
        //GetMainMenuItems/GetMainMenuLootItems helper
        public static T FindObjectByName<T>(string name) where T : UnityEngine.Object
        {
            foreach (var obj in UnityEngine.Resources.FindObjectsOfTypeAll<T>())
            {
                if (obj.name.Equals(name, System.StringComparison.OrdinalIgnoreCase))
                {
                    MelonLogger.Msg($"[{name}] loaded!");
                    return obj;
                }
            }
            return null;
        }
        public static void InitializeCodex()
        {
            // Call your existing loadTextures method
            _menuTexture = UpgradeInit.loadTextures("codex_long");

            if (_menuTexture != null)
            {
                // Create the Sprite once from the loaded texture
                _menuSprite = Sprite.Create(
                    _menuTexture,
                    new Rect(0, 0, _menuTexture.width, _menuTexture.height),
                    new Vector2(0.5f, 0.5f)
                );
            }
        }

        //get all clonable enemy objects
        public static void GetMainMenuEnemies()
        {
            UpgradeApplier.mainSpider = FindObjectByName<FFSpiderAI>("The Spider");
            UpgradeApplier.mainVentress = FindObjectByName<FFVentressAI>("The Ventress");
            UpgradeApplier.mainNButcher = FindObjectByName<FFButcherAI>("Nightmare Butcher");
            UpgradeApplier.mainForsaken = FindObjectByName<FFForsakenAI>("The Forsaken ECD");
            UpgradeApplier.mainShambler = FindObjectByName<FFShambler>("The Shambler");
            UpgradeApplier.mainAbandoned = FindObjectByName<FFTheAbandonedOneAI>("The Abandoned One");
            UpgradeApplier.mainPatient = FindObjectByName<FFForsakenAI>("The Forsaken (Patient)");
            UpgradeApplier.mainCarrier = FindObjectByName<FFCarrierAI>("The Carrier");
            UpgradeApplier.mainButcher = FindObjectByName<FFButcherAI>("The Butcher");
            UpgradeApplier.mainNShambler = FindObjectByName<FFShambler>("Nightmare Shambler");
            UpgradeApplier.mainNSpider = FindObjectByName<FFSpiderAI>("Nightmare Spider");
            UpgradeApplier.mainShepherd = FindObjectByName<FFMistAI>("The Shepherd");
            UpgradeApplier.mainPackrat = FindObjectByName<FFPackratAI>("The Packrat");

            UpgradeApplier.initDetectRadius = [
                UpgradeApplier.mainSpider.checkFarPlayerRadius,
                UpgradeApplier.mainVentress.checkFarPlayerRadius,
                UpgradeApplier.mainNButcher.checkFarPlayerRadius,
                UpgradeApplier.mainForsaken.checkFarPlayerRadius,
                UpgradeApplier.mainShambler.checkFarPlayerRadius,
                UpgradeApplier.mainAbandoned.checkFarPlayerRadius,
                UpgradeApplier.mainPatient.checkFarPlayerRadius,
                UpgradeApplier.mainCarrier.checkFarPlayerRadius,
                UpgradeApplier.mainButcher.checkFarPlayerRadius,
                UpgradeApplier.mainNShambler.checkFarPlayerRadius,
                UpgradeApplier.mainShepherd.checkFarPlayerRadius,
                UpgradeApplier.mainPackrat.checkFarPlayerRadius
            ]
            ;
            UpgradeApplier.initDetectChance = [

                (int)UpgradeApplier.mainSpider.checkFarPlayerChance,
                (int)UpgradeApplier.mainVentress.checkFarPlayerChance,
                (int)UpgradeApplier.mainNButcher.checkFarPlayerChance,
                (int)UpgradeApplier.mainForsaken.checkFarPlayerChance,
                (int)UpgradeApplier.mainShambler.checkFarPlayerChance,
                (int)UpgradeApplier.mainAbandoned.checkFarPlayerChance,
                (int)UpgradeApplier.mainPatient.checkFarPlayerChance,
                (int)UpgradeApplier.mainCarrier.checkFarPlayerChance,
                (int)UpgradeApplier.mainButcher.checkFarPlayerChance,
                (int)UpgradeApplier.mainNShambler.checkFarPlayerChance,
                (int)UpgradeApplier.mainShepherd.checkFarPlayerChance,
                (int)UpgradeApplier.mainPackrat.checkFarPlayerChance
            ];
            UpgradeApplier.initDetectDelay = [

                UpgradeApplier.mainSpider.checkFarPlayersDelay,
                UpgradeApplier.mainVentress.checkFarPlayersDelay,
                UpgradeApplier.mainNButcher.checkFarPlayersDelay,
                UpgradeApplier.mainForsaken.checkFarPlayersDelay,
                UpgradeApplier.mainShambler.checkFarPlayersDelay,
                UpgradeApplier.mainAbandoned.checkFarPlayersDelay,
                UpgradeApplier.mainPatient.checkFarPlayersDelay,
                UpgradeApplier.mainCarrier.checkFarPlayersDelay,
                UpgradeApplier.mainButcher.checkFarPlayersDelay,
                UpgradeApplier.mainNShambler.checkFarPlayersDelay,
                UpgradeApplier.mainShepherd.checkFarPlayersDelay,
                UpgradeApplier.mainPackrat.checkFarPlayersDelay
            ];
        }


        //upgradestter
        public static List<Upgrade> initUpgrades(List<Upgrade> upgrades)
        {
            //upgrade lists
            if (upgrades == null) upgrades = new List<Upgrade>();
            else upgrades.Clear();

            AddUpgrade(upgrades, "Evader", "Run from the inevitable.", loadTextures("evader1-crt"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //3500, 2000, //real
                3500, 3000, //test

                //upgrade scaler
                8f)
                ;

            AddUpgrade(upgrades, "Lurker", "Become undetectable.", loadTextures("lurker1-crt"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //2500, 1500, //real
                10, 50, //test

                //upgrade scaler
                0.1f)
                ;

            AddUpgrade(upgrades, "Supplier", "Fully utilize your limited resources.", loadTextures("supplier1-crt"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //3000, 2500, //real
                3000, 2500, //test

                //upgrade scaler
                33f)
                ;

            AddUpgrade(upgrades, "Ransacker", "Scavenge greater loot.", loadTextures("ransacker1-crt"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //5000, 5000, //real
                5000, 5000, //test

                //upgrade scaler
                //0.05f)
                20f) //test
                ;

            AddUpgrade(upgrades, "Forerunner", "Get ahead of your opposition.", loadTextures("forerunner1-crt"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //5000, 2500, //real
                5000, 2500, //test

                //upgrade scaler
                1f)
                ;

            return upgrades;
        }


        private static void AddUpgrade(List<Upgrade> list, string name, string description, Texture2D img, int lvl, int lvlMax, Color color, int costInit, int costScaler, float upgScaler)
        {
            var u = new Upgrade();
            u.upgName = name;
            u.upgDcr = description;
            u.upgImg = img;
            u.upgLvl = lvl;
            u.upgLvlMax = lvlMax;
            u.boxColor = color;
            u.initCost = costInit;
            u.costScaler = costScaler;
            u.upgScaler = upgScaler;

            list.Add(u);
        }

        public static void UpdateCostsAccToPlayerCount(int numberOfValidPlayers)
        {
            MelonLogger.Msg($"[Adjusting LOCAL Upgrade costs based on *{numberOfValidPlayers}* players.]\n\n");


            MelonLogger.Msg($"[Initial Upgrade Costs before:    [{upgrades[0].initCost}, {upgrades[1].initCost}, {upgrades[2].initCost}, {upgrades[3].initCost}, {upgrades[4].initCost}]]");
            MelonLogger.Msg($"[Subsequent Upgrade Costs before: [{upgrades[0].costScaler}, {upgrades[1].costScaler},{upgrades[2].costScaler},{upgrades[3].costScaler},{upgrades[4].costScaler},]]");

            int numOfPlayersForCalc = (numberOfValidPlayers - 1);
            double multiplyValue = 1;
            if (numOfPlayersForCalc == 0)
            {
                multiplyValue = 1;
            }
            else
            {
                multiplyValue = (1 + (numOfPlayersForCalc * 0.25f));
            }
            //1 = *1
            //2 = *1.5
            //3 = *1.75
            //4 = *2

            //Evader
            upgrades[0].initCost = (int)Math.Ceiling(3500 * multiplyValue);
            upgrades[0].costScaler = (int)Math.Ceiling(3000 * multiplyValue);
            //Lurker
            upgrades[1].initCost = (int)Math.Ceiling(10 * multiplyValue);
            upgrades[1].costScaler = (int)Math.Ceiling(50 * multiplyValue);
            //Hacker
            upgrades[2].initCost = (int)Math.Ceiling(3000 * multiplyValue);
            upgrades[2].costScaler = (int)Math.Ceiling(2500 * multiplyValue);
            //Ransacker
            upgrades[3].initCost = (int)Math.Ceiling(5000 * multiplyValue);
            upgrades[3].costScaler = (int)Math.Ceiling(5000 * multiplyValue);
            //Forerunner
            upgrades[4].initCost = (int)Math.Ceiling(5000 * multiplyValue);
            upgrades[4].costScaler = (int)Math.Ceiling(2500 * multiplyValue);

            MelonLogger.Msg($"[Initial Upgrade Costs after:    [{upgrades[0].initCost}, {upgrades[1].initCost}, {upgrades[2].initCost}, {upgrades[3].initCost}, {upgrades[4].initCost}]]");
            MelonLogger.Msg($"[Subsequent Upgrade Costs after: [{upgrades[0].costScaler}, {upgrades[1].costScaler},{upgrades[2].costScaler},{upgrades[3].costScaler},{upgrades[4].costScaler},]]");

        }


    public static Texture2D loadTextures(string name)
        {
            Assembly assembly = Assembly.GetExecutingAssembly();

            string resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(x => x.EndsWith($"{name}.png", System.StringComparison.OrdinalIgnoreCase));

            if (resourceName == null)
            {
                MelonLogger.Warning($"Embedded image not found: {name}.png");
                return null;
            }

            using (Stream stream = assembly.GetManifestResourceStream(resourceName))
            {
                if (stream == null)
                {
                    MelonLogger.Warning($"Could not open embedded image: {resourceName}");
                    return null;
                }

                using (MemoryStream memoryStream = new MemoryStream())
                {
                    stream.CopyTo(memoryStream);

                    byte[] data = memoryStream.ToArray();

                    Texture2D tex = new Texture2D(2, 2);

                    if (!ImageConversion.LoadImage(tex, data))
                    {
                        MelonLogger.Warning($"Failed to load embedded image: {resourceName}");
                        return null;
                    }

                    tex.filterMode = FilterMode.Point;

                    return tex;
                }
            }
        }
    }
}
