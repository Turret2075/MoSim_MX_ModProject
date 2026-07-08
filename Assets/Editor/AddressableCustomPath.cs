using UnityEditor;
using UnityEditor.AddressableAssets;
using UnityEditor.AddressableAssets.Settings;
using UnityEditor.AddressableAssets.Settings.GroupSchemas;
using UnityEngine;

namespace Editor
{
    public class AddressableCustomPath
    {
        [MenuItem("Tools/Addressables/Set Custom Addressable Paths")]
        public static void SetCustomAddressablePaths()
        {
            var settings = AddressableAssetSettingsDefaultObject.Settings;
            if (settings == null)
            {
                Debug.LogError("Addressable Asset Settings not found.");
                return;
            }

            var selectedGroup = Selection.activeObject as AddressableAssetGroup;
            if (selectedGroup == null)
            {
                Debug.LogError("Please select an Addressable Asset Group in the Project window.");
                return;
            }

            if (selectedGroup.ReadOnly)
            {
                Debug.LogWarning("${selectedGroup.Name} is read-only and cannot be modified.");
                return;
            }

            var schema = selectedGroup.GetSchema<BundledAssetGroupSchema>();
            if (schema == null)
            {
                Debug.LogWarning($"{selectedGroup.Name} does not have a BundledAssetGroupSchema.");
                return;
            }

            var groupName = selectedGroup.Name;
            var customPath = $"Assets/AddressableAssets/{groupName}";

            // schema.BuildPath.SetVariableByName(settings, $"{Application.dataPath}/../Mods/{groupName}");
            // schema.LoadPath.SetVariableByName(settings, $"{Application.persistentDataPath}/Mods/{groupName}");
            schema.BuildPath.SetVariableByName(settings, $"{groupName}_BuildPath");
            schema.LoadPath.SetVariableByName(settings, $"{groupName}_LoadPath");

            var variableNames = settings.profileSettings.GetVariableNames();
            if (!variableNames.Contains($"{groupName}_BuildPath"))
            {
                // settings.profileSettings.CreateValue($"{groupName}_BuildPath",
                //     Application.dataPath + "/../Mods/" + groupName);
                settings.profileSettings.CreateValue($"{groupName}_BuildPath",
                    "{UnityEngine.Application.dataPath}/../Mods/" + groupName);
            }
            if (!variableNames.Contains($"{groupName}_LoadPath"))
            {
                // settings.profileSettings.CreateValue($"{groupName}_LoadPath",
                //     Application.persistentDataPath + "/Mods/" + groupName);
                settings.profileSettings.CreateValue($"{groupName}_LoadPath",
                    "{UnityEngine.Application.persistentDataPath}/Mods/" + groupName);
            }
            
            EditorUtility.SetDirty(selectedGroup);
            AssetDatabase.SaveAssets();
            
            Debug.Log($"Set custom build and load paths for group {groupName}");
        }
    }
}