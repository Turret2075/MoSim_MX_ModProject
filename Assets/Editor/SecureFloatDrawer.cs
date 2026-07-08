// This file MUST be placed inside a folder named "Editor".

using System;
using MoSimLib;
using UnityEditor;
using UnityEngine;

namespace Editor
{
    [CustomPropertyDrawer(typeof(SecureFloat))]
    public class SecureFloatDrawer : PropertyDrawer
    {
        // Override the OnGUI method to draw the property in the Inspector.
        public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
        {
            // Use a try-catch block to handle any potential errors during drawing.
            try
            {
                // Get the serialized properties for _obfuscationKey and _encryptedValue.
                SerializedProperty obfuscationKeyProp = property.FindPropertyRelative("_obfuscationKey");
                SerializedProperty encryptedValueProp = property.FindPropertyRelative("_encryptedValue");

                // Create a temporary SecureFloat instance to work with the value.
                SecureFloat secureFloat = new SecureFloat();

                // Set the obfuscation key and encrypted value from the serialized properties.
                // This is a roundabout way to "deserialize" the struct for the drawer.
                secureFloat.Value = BitConverter.Int32BitsToSingle(encryptedValueProp.intValue ^ obfuscationKeyProp.intValue);

                // Draw a standard float field using the current value.
                float newValue = EditorGUI.FloatField(position, label, secureFloat.Value);

                // Check if the value has been changed.
                if (newValue != secureFloat.Value)
                {
                    // If the value changed, set the new value on the temporary struct.
                    secureFloat.Value = newValue;

                    // Save the newly obfuscated values back to the serialized properties.
                    obfuscationKeyProp.intValue = secureFloat._obfuscationKey;
                    encryptedValueProp.intValue = secureFloat._encryptedValue;
                }
            }
            catch (System.Exception e)
            {
                EditorGUI.LabelField(position, "Error: " + e.Message);
                Debug.LogError("SecureFloatDrawer caught an exception: " + e);
            }
        }
    }
}
