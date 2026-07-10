using MelonLoader;
using System;
using System.Collections.Generic;
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
        public static List<Upgrade> initUpgrades(List<Upgrade> upgrades)
        {
            //upgrade lists
            if (upgrades == null) upgrades = new List<Upgrade>();
            else upgrades.Clear();

            AddUpgrade(upgrades, "Evader", "Master maneuverability.", loadTextures("evader"),
                //upgrade start, upgrade max
                0, 20, new Color(0.737f, 0.718f, 0.353f, 1f),
                //cost init, cost scaler
                1000, 500,
                //upgrade scaler
                5f)
                ;

            AddUpgrade(upgrades, "Lurker", "Become undetectable.", loadTextures("lurker"),
                0, 3, new Color(0.765f, 0.894f, 0.839f, 1f),
                //cost init, cost scaler
                99999, 500,
                //upgrade scaler
                0.1f)
                ;

            AddUpgrade(upgrades, "Tinkerer", "Fully utilize resources.", loadTextures("tinkerer"),
                0, 10, new Color(0.5f, 0.235f, 0.235f, 1f),
                //cost init, cost scaler
                99999, 500,
                //upgrade scaler
                0.1f)
                ;

            AddUpgrade(upgrades, "Rummager", "Scavenge greater loot.", loadTextures("rummager"),
                0, 10, new Color(0.54f, 0.43f, 0.19f, 1f),
                //cost init, cost scaler
                99999, 500,
                //upgrade scaler
                0.01f)
                ;

            AddUpgrade(upgrades, "Forerunner", "Ahead of the game.", loadTextures("forerunner"),
                0, 5, new Color(0.25f, 0.25f, 0.455f, 1f),
                //cost init, cost scaler
                3000, 1500,
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
