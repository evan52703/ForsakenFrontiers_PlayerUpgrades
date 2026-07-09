using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using static PlayerUpgrades.Core;

namespace PlayerUpgrades
{
    public static class UpgradeMenu
    {
        private static GUIStyle _menuStyle;
        private static GUIStyle _statsStyle;

        public static int x = 10;
        public static int y = 20;
        public static float dropY = 320 + y;
        public static int spacing = 0;

        private static float _errorTimer = 0f;
        private static string _errorMessage = "";
        private static GUIStyle errorStyle = new GUIStyle(GUI.skin.label);
        private static float alpha;

        //current
        public static string prependCurrent1, postpendCurrent1 = null;
        public static string prependCurrent2, postpendCurrent2 = null;

        //future
        public static string prependFuture1, postpendFuture1 = null;
        public static string prependFuture2, postpendFuture2 = null;

        // Pass the Core's variables as arguments to keep things synced
        public static void Draw(List<Core.Upgrade> upgrades, ref Core.Upgrade selected)
        {
            InitStyles();
            spacing = 0;

            // Header Box
            GUI.backgroundColor = Color.red;
            GUI.Box(new Rect(x, y, 250, 300), "Upgrade Menu", _menuStyle);

            // Vertical Button List
            foreach (var upgrade in upgrades)
            {
                GUI.backgroundColor = upgrade.boxColor;
                if (GUI.Button(new Rect(40 + x, 40 + y + spacing, 210, 40), upgrade.upgName))
                {
                    selected = (selected == upgrade) ? null : upgrade;
                }
                spacing += 50;
            }

            if (selected != null)
            {
                DrawUpgradeDetails(selected);
            }
        }

        private static void DrawUpgradeDetails(Core.Upgrade upgrade)
        {
            //Draw Name
            GUI.Box(new Rect(x, 10 + dropY, 300, 320), upgrade.upgName, _menuStyle);

            // Draw Texture
            if (upgrade.upgImg != null)
                GUI.DrawTexture(new Rect(x + 90, dropY + 45, 100, 100), upgrade.upgImg);
            else
            {
                upgrade.upgImg = UpgradeInit.loadTextures(upgrade.upgName);
                GUI.DrawTexture(new Rect(x + 90, dropY + 45, 100, 100), upgrade.upgImg);
            }

            // Draw cost
            if (upgrade.upgLvl != upgrade.upgLvlMax)
                GUI.Label(new Rect(x + 30, dropY + 105, 200, 100), $"Current Level: {upgrade.upgLvl}", _statsStyle);
            // Draw cost
            if (upgrade.upgLvl != upgrade.upgLvlMax)
                GUI.Label(new Rect(x + 30, dropY +120, 200, 100), $"Next Level Cost: {upgrade.initCost + (upgrade.costScaler * (upgrade.upgLvl))} Credits", _statsStyle);

            //Error Logic
            if (Time.time < _errorTimer)
            {
                errorStyle.normal.textColor = new Color(1f,0f,0f,alpha);
                errorStyle.alignment = TextAnchor.MiddleCenter;
                GUI.Label(new Rect(x, (spacing + dropY) + 80, 300, 40), _errorMessage, errorStyle);
            }

            // Buy Button logic
            if (upgrade.upgLvl < upgrade.upgLvlMax)
            {
                if (GUI.Button(new Rect(40 + x, (spacing + dropY) + 40, 210, 40), "Buy"))
                {
                    if (Core.train.Credits >= upgrade.initCost + (upgrade.costScaler * (upgrade.upgLvl)))
                    {
                        UpgradeApplier.RequestUpgradePurchase(upgrades.IndexOf(upgrade));
                    }
                    else
                        TriggerError("Not enough credits...");
                }
            }
            //Upgrade Effects
            DisplayUpgradeBenefits(upgrade);

            {
                alpha -= 0.001f;
            }
        }

        private static void InitStyles()
        {
            _menuStyle = new GUIStyle(GUI.skin.box)
            {
                fontSize = 24,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.UpperCenter
            };
            _menuStyle.normal.textColor = Color.white;

            _statsStyle = new GUIStyle(GUI.skin.label)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter
            };
        }
        private static void DisplayUpgradeBenefits(Core.Upgrade upgrade)
        {
            bool isPrecursor = false;
            bool hasSecondArg = false;
            
            // Clear strings so old data doesn't stick around
            prependCurrent1 = ""; postpendCurrent1 = "";
            prependCurrent2 = ""; postpendCurrent2 = "";
            prependFuture1 = ""; postpendFuture1 = "";
            prependFuture2 = ""; postpendFuture2 = "";

            if (upgrade.upgName == "Efficient")
            {
                prependCurrent1 = "Max Speed +";
                postpendCurrent1 = "%";
                prependCurrent2 = "Stamina Drain -";
                postpendCurrent2 = "%";

                prependFuture1 = "+";
                postpendFuture1 = "% Max Speed";
                prependFuture2 = "-";
                postpendFuture2 = "% Stamina Drain";

                hasSecondArg = true;
            }
            else if (upgrade.upgName == "Elusive")
            {
                prependCurrent1 = "N/A";
                postpendCurrent1 = "N/A";
                prependCurrent2 = "N/A";
                postpendCurrent2 = "N/A";

                prependFuture1 = "N/A";
                postpendFuture1 = "N/A";
                prependFuture2 = "N/A";
                postpendFuture2 = "N/A";

                hasSecondArg = false;
            }
            else if (upgrade.upgName == "Tinkerer")
            {
                prependCurrent1 = "N/A";
                postpendCurrent1 = "N/A";
                prependCurrent2 = "N/A";
                postpendCurrent2 = "N/A";

                prependFuture1 = "N/A";
                postpendFuture1 = "N/A";
                prependFuture2 = "N/A";
                postpendFuture2 = "N/A";

                hasSecondArg = false;
            }
            else if (upgrade.upgName == "Rummager")
            {
                prependCurrent1 = "N/A";
                postpendCurrent1 = "N/A";
                prependCurrent2 = "N/A";
                postpendCurrent2 = "N/A";

                prependFuture1 = "N/A";
                postpendFuture1 = "N/A";
                prependFuture2 = "N/A";
                postpendFuture2 = "N/A";

                hasSecondArg = false;
            }
            else if (upgrade.upgName == "Precursor")
            {
                prependCurrent1 = "Current Arrival Time: ";
                postpendCurrent1 = ":00 AM";

                prependFuture1 = "Arrive +";
                postpendFuture1 = " Hour Earlier";

                isPrecursor = true;
            }

            GUI.Label(new Rect(x + 20, dropY + 185, 100, 40), "Current Effects:", _statsStyle);
            GUI.Label(new Rect(x + 150, dropY + 185, 130, 40), "Next Level Effects:", _statsStyle);
            _statsStyle.normal.textColor = upgrade.boxColor;

            if (!isPrecursor)
            {
                GUI.Label(new Rect(x + 20, dropY + 195, 100, 40), $"{prependCurrent1}{upgrade.upgLvl * upgrade.upgScaler}{postpendCurrent1}", _statsStyle);
                if (hasSecondArg) GUI.Label(new Rect(x+20, dropY + 210, 100, 40), $"{prependCurrent2}{upgrade.upgLvl * upgrade.upgScaler}{postpendCurrent2}", _statsStyle);

                GUI.Label(new Rect(x + 150, dropY + 195, 100, 40), $"{prependFuture1}{upgrade.upgScaler}{postpendFuture1}", _statsStyle);
                if (hasSecondArg) GUI.Label(new Rect(x + 150, dropY + 210, 100, 40), $"{prependFuture2}{upgrade.upgScaler}{postpendFuture2}", _statsStyle);
            }
            else
            {
                GUI.Label(new Rect(x + 20, dropY + 215, 100, 40), $"{prependCurrent1}{6 - upgrade.upgLvl}{postpendCurrent1}", _statsStyle);
                GUI.Label(new Rect(x + 150, dropY + 215, 100, 40), $"{prependFuture1}{upgrade.upgScaler}{postpendFuture1}", _statsStyle);
            }
               
        }
        private static void TriggerError(string message)
        {
            _errorMessage = message;
            _errorTimer = Time.time + 3f; // Show for 3 seconds
            alpha = 1;
        }
    }
}
