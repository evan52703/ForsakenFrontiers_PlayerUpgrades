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

namespace PlayerUpgrades
{
    internal static class UpgradeInit
    {

        //get all clonable item objects
        public static void GetMainMenuItems()
        {
            UpgradeApplier.mainMarker = FindObjectByName<FFSprayTool>("SprayMark");
            UpgradeApplier.mainBoltcutter = FindObjectByName<FFBoltcutters>("Buoltcutters");
            UpgradeApplier.mainSledgehammer = FindObjectByName<FFSledgehammer>("SledgeHammer");
            UpgradeApplier.mainStunlight = FindObjectByName<FFStunLight>("StunLight");

            UpgradeApplier.mainGlowstick = FindObjectByName<FFGlowstick>("Glowstick");
            UpgradeApplier.mainLantern = FindObjectByName<FFLantern>("Lantern");
            UpgradeApplier.mainFlashlight = FindObjectByName<FFFlashlight>("Flashlight");
        }
        ////get all clonable loot item objects
        //public static void GetMainMenuLootItems()
        //{
        //    FFLootItem[] allItems = UnityEngine.Resources.FindObjectsOfTypeAll<FFLootItem>();
        //    if (allItems == null)
        //    {
        //        MelonLogger.Msg($"NULL");
        //        return;
        //    }
        //    UpgradeApplier.originalLootItemList = allItems;

        //    int count = 0;
        //    foreach (var item in allItems)
        //    {
        //        //sift through valid list
        //        if (item == null) continue;

        //        try
        //        {
        //            if (item.gameObject != null)
        //            {
        //                MelonLogger.Msg($"Item[{count}]: {item.name}; Value: {item.value}");
        //                UpgradeApplier.originalLootItemValuesList[count] = item.value;
        //            }
        //        }
        //        catch
        //        {
        //            continue;
        //        }
        //        count++;
        //    }
        //    MelonLogger.Msg($"Entire Item Value List:");
        //}
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


        //upgradestter
        public static List<Upgrade> initUpgrades(List<Upgrade> upgrades)
        {
            //upgrade lists
            if (upgrades == null) upgrades = new List<Upgrade>();
            else upgrades.Clear();

            AddUpgrade(upgrades, "Evader", "Run from the inevitable.", loadTextures("evader"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //3500, 2000, //real
                3500, 3000, //test

                //upgrade scaler
                8f)
                ;

            AddUpgrade(upgrades, "Lurker", "Become undetectable.", loadTextures("lurker"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //2500, 1500, //real
                10, 50, //test

                //upgrade scaler
                0.1f)
                ;

            AddUpgrade(upgrades, "Hacker", "Fully utilize your limited resources.", loadTextures("hacker"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //3000, 2500, //real
                3000, 2500, //test

                //upgrade scaler
                33f)
                ;

            AddUpgrade(upgrades, "Ransacker", "Scavenge greater loot.", loadTextures("ransacker"),
                //upgrade start, upgrade max. color
                0, 5, new Color(1f, 1f, 1f, 1f),

                //cost init, cost scaler
                //5000, 5000, //real
                5000, 5000, //test

                //upgrade scaler
                //0.05f)
                20f) //test
                ;

            AddUpgrade(upgrades, "Forerunner", "Get ahead of your opposition.", loadTextures("forerunner"),
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

        public static Texture2D loadTextures(string name)
        {
            //get path to images
            string path = System.IO.Path.Combine(Application.dataPath, "../UserLibs/Upgrades", name + ".png");
            if (!System.IO.File.Exists(path))
            {
                MelonLogger.Warning($"Image not found at {path}");
                return null;
            }

            byte[] data = System.IO.File.ReadAllBytes(path);
            Texture2D tex = new Texture2D(2, 2);
            ImageConversion.LoadImage(tex, data);
            tex.filterMode = FilterMode.Point; // Makes pixels sharp instead of blurry
            return tex;
        }
    }
}
