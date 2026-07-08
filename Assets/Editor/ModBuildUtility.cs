using System.IO;
using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Build;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Editor
{
    public static class ModBuildUtility
    {
        [MenuItem("Tools/Build Mod Group")]
        public static void BuildSelectedModGroup()
        {
            // Get current selection
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("No Addressables settings found.");
                return;
            }

            if (Selection.activeObject == null)
            {
                Debug.LogError("Select an Addressables group in the Groups window first.");
                return;
            }

            var group = Selection.activeObject as AddressableAssetGroup;
            if (group == null)
            {
                Debug.LogError("Selection is not an AddressableAssetGroup.");
                return;
            }

            // Use group name as mod folder
            string modName = group.Name;
            string buildPath = Path.Combine(Application.dataPath, "..", "Mods", modName);

            if (!Directory.Exists(buildPath))
                Directory.CreateDirectory(buildPath);

            // Set custom build/load paths for this group
            var schema = group.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                schema = group.AddSchema<BundledAssetGroupSchema>();
            }

            schema.BuildPath.SetVariableByName(settings, buildPath);
            schema.LoadPath.SetVariableByName(settings, buildPath);

            Debug.Log($"Building mod group '{modName}' into: {buildPath}");

            // Build just this group
            AddressableAssetSettings.BuildPlayerContent(out AddressablesPlayerBuildResult result);

            if (!string.IsNullOrEmpty(result.Error))
            {
                Debug.LogError($"Mod build failed: {result.Error}");
            }
            else
            {
                Debug.Log($"Mod build completed. Catalog + bundles written to: {buildPath}");
            }
        }
    }
}
