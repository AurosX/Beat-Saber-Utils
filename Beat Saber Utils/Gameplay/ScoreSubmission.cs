﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using Logger = BS_Utils.Utilities.Logger;
using LogLevel = IPA.Logging.Logger.Level;

namespace BS_Utils.Gameplay
{
    public class ScoreSubmission
    {
        public static bool Disabled { get { return disabled; } }
        public static bool ProlongedDisabled { get { return prolongedDisable; } }
        public static bool eventSubscribed = false;

        public static string ModString
        {
            get
            {
                string value = "";
                for (int i = 0; i < ModList.Count; i++)
                {
                    if (i == 0)
                        value += ModList[i];
                    else
                        value += ", " + ModList[i];
                }
                return value;
            }

        }

        public static string ProlongedModString
        {
            get
            {
                string value = "";
                for (int i = 0; i < ProlongedModList.Count; i++)
                {
                    if (i == 0)
                        value += ProlongedModList[i];
                    else
                        value += ", " + ProlongedModList[i];
                }
                return value;
            }

        }

        internal static bool disabled = false;
        internal static bool prolongedDisable = false;
        internal static List<string> ModList { get; set; } = new List<string>(0);
        internal static List<string> ProlongedModList { get; set; } = new List<string>(0);

        public static void DisableSubmission(string mod)
        {
            if (disabled == false)
            {
                Plugin.ApplyHarmonyPatches();

                disabled = true;
                ModList.Clear();

                if (!eventSubscribed)
                {
                    Plugin.LevelDidFinishEvent += LevelData_didFinishEvent;
                    eventSubscribed = true;
                }
            }

            if (!ModList.Contains(mod))
                ModList.Add(mod);
        }

        public static void DisableScoreSaberScoreSubmission()
        {
            Logger.Log("ScoreSubmission: DisableScoreSaberScoreSubmission called...", LogLevel.Debug);

            StandardLevelScenesTransitionSetupDataSO setupDataSO = Resources.FindObjectsOfTypeAll<StandardLevelScenesTransitionSetupDataSO>().FirstOrDefault();
            if (setupDataSO == null)
            {
                Logger.Log("ScoreSubmission: StandardLevelScenesTransitionSetupDataSO not found - exiting...", LogLevel.Debug);
                return;
            }

            LogEvents(setupDataSO, "didFinishEvent");

            Logger.Log("Removing ScoreSaber events...", LogLevel.Debug);
            DisableEvent(setupDataSO, "didFinishEvent", "Five");

            Logger.Log("ScoreSubmission: Getting list of delegates for didFinish event after removing ScoreSaber...", LogLevel.Debug);
            LogEvents(setupDataSO, "didFinishEvent");
        }

        private static void LevelData_didFinishEvent(StandardLevelScenesTransitionSetupDataSO arg1, LevelCompletionResults arg2)
        {
            switch (arg2.levelEndStateType)
            {
                case LevelCompletionResults.LevelEndStateType.Failed:
                    disabled = false;
                    ModList.Clear();
                    break;
                case LevelCompletionResults.LevelEndStateType.None:
                    disabled = false;
                    ModList.Clear();
                    break;
            }
            if (arg2.levelEndAction == LevelCompletionResults.LevelEndAction.Quit)
            {
                disabled = false;
                ModList.Clear(); 
            }
            Plugin.LevelDidFinishEvent -= LevelData_didFinishEvent;
            eventSubscribed = false;
        }

        public static void ProlongedDisableSubmission(string mod)
        {
            Logger.Log("Prolonged disable called...", LogLevel.Debug);
            if (prolongedDisable == false)
            {
                Plugin.ApplyHarmonyPatches();
                prolongedDisable = true;
            }

            if (!ProlongedModList.Contains(mod))
                ProlongedModList.Add(mod);
        }

        public static void RemoveProlongedDisable(string mod)
        {
            ProlongedModList.Remove(mod);

            if (ProlongedModList.Count == 0)
            {
                prolongedDisable = false;
            }
                
        }

        private static bool DisableEvent(object target, string eventName, string delegateName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(eventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            var eventDelegate = fieldInfo.GetValue(target) as MulticastDelegate;
            bool eventDisabled = false;
            if (eventDelegate != null)
            {
                var delegates = eventDelegate.GetInvocationList();
                foreach (var item in delegates)
                {
                    if (item.Method.Name == delegateName)
                    {
                        target.GetType().GetEvent(eventName).RemoveEventHandler(target, item);
                        eventDisabled = true;
                    }
                }
            }
            return eventDisabled;
        }

        // Used for debugging purposes
        private static void LogEvents(object target, string eventName)
        {
            FieldInfo fieldInfo = target.GetType().GetField(eventName, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static);

            var eventDelegate = fieldInfo.GetValue(target) as MulticastDelegate;
            if (eventDelegate == null)
            {
                Logger.Log("ScoreSubmission: Unable to get eventDelegate from StandardLevelScenesTransitionSetupDataSO - exiting...", LogLevel.Debug);
            }

            var delegates = eventDelegate.GetInvocationList();
            Logger.Log("ScoreSubmission: Getting list of delegates for didFinish event...", LogLevel.Debug);
            foreach (var item in delegates)
            {
                Logger.Log(String.Format("ScoreSubmission: Found delegate named '{0}' by Module '{1}', part of Assembly '{2}'", item.Method.Name, item.Method.Module.Name, item.Method.Module.Assembly.FullName), LogLevel.Debug);
            }
        }
    }
}
