using System;
using System.Collections.Generic;
using System.Reflection;
using BepInEx;
using BepInEx.Bootstrap;
using BepInEx.Configuration;
using HarmonyLib;
using UnityEngine;

[BepInPlugin("org.nomidance.arm64.status", "Nomi Dance Status", "0.2.0")]
[BepInDependency("com.community.hs.NomiCantDance", BepInDependency.DependencyFlags.SoftDependency)]
public sealed class StatusOverlay : BaseUnityPlugin
{
    private static readonly HashSet<string> Expected = new HashSet<string> {
        "ZoneMgr.Awake", "GameState.SendOption", "PowerTask.DoRealTimeTask",
        "ZoneMgr.AddPredictedLocalZoneChange", "ZoneMgr.CreateLocalChangesFromTrigger",
        "ZoneMgr.MergeServerChangeList", "ZoneMgr.PostProcessServerChangeList"
    };
    private ConfigEntry<bool> visible;
    private float nextCheck;
    private string text = "Nomi Dance : vérification…";
    private Color color = Color.yellow;
    private GUIStyle style;
    private string lastLogged;

    private void Awake()
    {
        visible = Config.Bind("Display", "Visible", true, "Show the non-interactive status indicator.");
        RefreshStatus();
    }

    private void Update()
    {
        if (Time.unscaledTime < nextCheck) return;
        nextCheck = Time.unscaledTime + 2f;
        RefreshStatus();
    }

    private static bool FromModule(IEnumerable<Patch> patches, Assembly module)
    {
        foreach (Patch patch in patches)
            if (patch.PatchMethod != null && patch.PatchMethod.Module.Assembly == module) return true;
        return false;
    }

    private void RefreshStatus()
    {
        try
        {
            PluginInfo plugin;
            if (!Chainloader.PluginInfos.TryGetValue("com.community.hs.NomiCantDance", out plugin) || plugin.Instance == null)
            {
                text = "Nomi Dance : non chargé";
                color = new Color(1f, 0.45f, 0.35f);
            }
            else
            {
                Assembly assembly = plugin.Instance.GetType().Assembly;
                var found = new HashSet<string>();
                foreach (MethodBase method in Harmony.GetAllPatchedMethods())
                {
                    string key = method.DeclaringType == null ? "" : method.DeclaringType.Name + "." + method.Name;
                    if (!Expected.Contains(key)) continue;
                    Patches info = Harmony.GetPatchInfo(method);
                    if (info != null && (FromModule(info.Prefixes, assembly) || FromModule(info.Postfixes, assembly) ||
                        FromModule(info.Transpilers, assembly) || FromModule(info.Finalizers, assembly))) found.Add(key);
                }
                bool enabled = plugin.Instance.Config.Bind("Fix", "Enabled", true).Value;
                if (!enabled)
                {
                    text = "Nomi Dance : désactivé";
                    color = Color.yellow;
                }
                else if (found.Count == Expected.Count)
                {
                    text = "Nomi Dance actif · 7/7";
                    color = new Color(0.4f, 1f, 0.55f);
                }
                else
                {
                    text = "Nomi Dance : patches " + found.Count + "/7";
                    color = Color.yellow;
                }
            }
        }
        catch (Exception ex)
        {
            text = "Nomi Dance : état inconnu";
            color = Color.yellow;
            if (lastLogged != text) Logger.LogWarning("Status check failed: " + ex.GetType().Name);
        }
        if (lastLogged != text)
        {
            Logger.LogInfo(text);
            lastLogged = text;
        }
    }

    private void OnGUI()
    {
        if (visible == null || !visible.Value) return;
        if (style == null) style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.MiddleCenter, fontStyle = FontStyle.Bold };
        float scale = Mathf.Clamp(Screen.width / 1920f, 1f, 2f);
        style.fontSize = Mathf.RoundToInt(14f * scale);
        style.normal.textColor = color;
        // A label draws only: it is not a clickable control and takes no keyboard focus.
        GUI.Label(new Rect(Screen.width - 244f * scale, 12f * scale, 232f * scale, 30f * scale), text, style);
    }
}
