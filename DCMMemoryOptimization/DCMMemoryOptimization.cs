using BepInEx;
using BepInEx.Configuration;
using BepInEx.Logging;
using HarmonyLib;
using System;
using System.Reflection;
using System.Security;
using System.Security.Permissions;
using UnityEngine;

[assembly: AssemblyVersion(COM3D2.DCMMemoryOptimization.Plugin.PluginInfo.PLUGIN_VERSION + ".*")]
[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

namespace COM3D2.DCMMemoryOptimization.Plugin
{
    public static class PluginInfo
    {
        public const string PLUGIN_GUID = "COM3D2.DCMMemoryOptimization";
        public const string PLUGIN_NAME = "DCMMemoryOptimization";
        public const string PLUGIN_VERSION = "1.0";
    }

    [BepInPlugin(PluginInfo.PLUGIN_GUID, PluginInfo.PLUGIN_NAME, PluginInfo.PLUGIN_VERSION)]
    public sealed class DCMMemoryOptimization : BaseUnityPlugin
    {
        public static DCMMemoryOptimization Instance { get; private set; }
        private ManualLogSource _Logger => base.Logger;
        private float lastExecutionTime;
        internal static new ManualLogSource Logger => Instance?._Logger;
        internal static bool gcStopped = false;
        internal static bool gcOptimize;
        internal static bool gcAvoidVirtualMemory;
        internal static float gcSuspendLimit;

        internal static ConfigEntry<bool> gcOptimizeConfig;
        internal static ConfigEntry<bool> gcAvoidVirtualMemoryConfig;
        internal static ConfigEntry<string> gcSuspendLimitConfig;

        private void Awake()
        {
            Instance = this;
            gcOptimizeConfig = Config.Bind("Memory Optimization", "Enable GC Optimize", true, "Enable GC Optimize");
            gcAvoidVirtualMemoryConfig = Config.Bind("Memory Optimization", "GC Avoid Virtual Memory", true, "GC Avoid Virtual Memory");
            gcSuspendLimitConfig = Config.Bind("Memory Optimization", "GC Suspend Limit", "40%", "GC Suspend Limit");
            gcOptimize = gcOptimizeConfig.Value;
            gcAvoidVirtualMemory = gcAvoidVirtualMemoryConfig.Value;
            gcSuspendLimit = CalcMemory(gcSuspendLimitConfig.Value);
            gcOptimizeConfig.SettingChanged += (s, e) => gcOptimize = gcOptimizeConfig.Value;
            gcAvoidVirtualMemoryConfig.SettingChanged += (s, e) => gcAvoidVirtualMemory = gcAvoidVirtualMemoryConfig.Value;
            gcSuspendLimitConfig.SettingChanged += (s, e) => gcSuspendLimit = CalcMemory(gcSuspendLimitConfig.Value);
            if (MemoryInfo.gcOpInit())
            {
                lastExecutionTime = Time.time;
                Harmony.CreateAndPatchAll(typeof(DCMMemoryOptimization));
            }
            else
            {
                gcOptimizeConfig.Value = false;
                Logger.LogError("GC Optimize Error.");
            }
        }

        private void Update()
        {
            if (gcOptimize && gcStopped && Time.time - lastExecutionTime >= 3f)
            {
                if ((gcSuspendLimit > 0 && GetGarbageSize() > gcSuspendLimit) || (gcAvoidVirtualMemory && GetAvailPhyMemorySize() < 512 * 1024 * 1024))
                {
                    EnableGarbageCollector(true);
                    DoGarbageCollect();
                    EnableGarbageCollector(false);
                }
                lastExecutionTime = Time.time;
            }
        }

        private static float CalcMemory(string m)
        {
            if (!string.IsNullOrEmpty(m))
            {
                try
                {
                    string ml = m.Trim().ToLower();
                    string ms = ml.Substring(0, ml.Length - 1);
                    if (ms.Length > 0)
                    {
                        if (m.EndsWith("m"))
                        {
                            return float.Parse(ms) * 1024 * 1024;
                        }
                        else if (m.EndsWith("g"))
                        {
                            return float.Parse(ms) * 1024 * 1024 * 1024;
                        }
                        else if (m.EndsWith("%"))
                        {
                            return float.Parse(ms) / 100 * (float)GetTotalPhyMemorySize();
                        }
                    }
                }
                catch
                {
                    Logger.LogError($"Failed to Parse Config: Value={m}, Set to Default(40%)");
                }
            }
            else
            {
                return 0;
            }
            return 0.4f * (float)GetTotalPhyMemorySize();
        }

        public static ulong GetWorkingSetMemorySize()
        {
            return MemoryInfo.QueryProcessMemStatus().WorkingSetSize;
        }
        public static ulong GetAvailPhyMemorySize()
        {
            return MemoryInfo.QuerySystemMemStatus().ullAvailPhys;
        }

        public static ulong GetTotalPhyMemorySize()
        {
            return MemoryInfo.QuerySystemMemStatus().ullTotalPhys;
        }

        public static void DoGarbageCollect()
        {
            GC.Collect(GC.MaxGeneration);
        }

        public static long GetGarbageSize()
        {
            return GC.GetTotalMemory(false);
        }

        public static bool EnableGarbageCollector(bool enable)
        {
            gcStopped = !enable;
            return MemoryInfo.gcSetStatusX(enable);
        }

        [HarmonyPatch(typeof(COM3D2.DanceCameraMotion.Plugin.DanceCameraMotion), "StartFreeDance")]
        [HarmonyPostfix]
        private static void StartFreeDancePostfix()
        {
            if (gcOptimize)
            {
                EnableGarbageCollector(false);
            }
        }

        [HarmonyPatch(typeof(COM3D2.DanceCameraMotion.Plugin.DanceCameraMotion), "EndFreeDance")]
        [HarmonyPostfix]
        private static void EndFreeDancePostfix()
        {
            EnableGarbageCollector(true);
        }
    }
}
