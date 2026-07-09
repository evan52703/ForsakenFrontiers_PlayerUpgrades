using Il2Cppmadeinfairyland.fairyengine.actor.player;
using Il2Cppmadeinfairyland.forsakenfrontiers.actor.player;
using MelonLoader;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnityEngine.Rendering.PostProcessing;

namespace PlayerUpgrades
{
    internal class SteamIDUses
    {
        public static FFPlayer findMyPlayer(ulong localSteamID)
        {
            FFPlayer[] players = UnityEngine.Object.FindObjectsOfType<FFPlayer>();

            foreach (var p in players)
            {
                if (p.SteamID.m_SteamID == localSteamID)
                {
                    return p;
                }
            }
            MelonLogger.Warning($"ERROR: No matching Steam ID found across all players.");
            return null;
        }
        public static bool IsHost(ulong localSteamID)
        {
            FFPlayer[] players = UnityEngine.Object.FindObjectsOfType<FFPlayer>();

            MelonLogger.Msg($"Player's Local Steam ID: {localSteamID}\n");
            foreach (var p in players)
            {
                MelonLogger.Msg($"Comparing to Steam ID: {localSteamID}...");
                if (p.SteamID.m_SteamID == localSteamID)
                {
                    MelonLogger.Msg($" TRUE. ");
                }
                MelonLogger.Msg($"Checking if host: ...");
                if (p.IsHost)
                {
                    MelonLogger.Msg($" TRUE. Host found. Test Concluding.\n");
                    return true;
                }
                MelonLogger.Msg($" FALSE. Next player...\n");
            }
            //should fail for everybody but host
            return false;
        }

        //exact copy of above but lists all player and tests them
        public static bool IsHostList(ulong localSteamID)
        {
            FFPlayer[] players = UnityEngine.Object.FindObjectsOfType<FFPlayer>();

            //success flag
            var hostFound = false;
            var counter = 0;

            MelonLogger.Msg($"HOST TEST\n");
            MelonLogger.Msg($"Players ingame: {players.Length}\n\n");

            MelonLogger.Msg($"Player's Local Steam ID: {localSteamID}\n");
            foreach (var p in players)
            {
                //inc counter
                counter++;

                MelonLogger.Msg($"Test {counter}/{players.Length}");
                MelonLogger.Msg($"Comparing to Steam ID: {localSteamID}...");
                if (p.SteamID.m_SteamID == localSteamID)
                {
                    MelonLogger.Msg($" TRUE. ");
                }
                MelonLogger.Msg($"Checking if host: ...");
                if (p.IsHost)
                {
                    MelonLogger.Msg($" TRUE. Host found.\n");
                    hostFound = true;
                    continue;
                }
                MelonLogger.Msg($" FALSE.\n");
            }

            if (hostFound) return true;
            return false;
        }
    }
}
