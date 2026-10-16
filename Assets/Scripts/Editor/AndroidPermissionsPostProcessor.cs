using UnityEditor;
using UnityEditor.Android;
using UnityEngine;
using System.IO;
using System.Xml;

/// <summary>
/// Automatically adds required Android permissions for UDP networking.
/// This runs AFTER Unity generates the Gradle project and modifies the AndroidManifest.xml.
/// This is the official Unity way to add custom Android permissions without manifest conflicts.
/// </summary>
public class AndroidPermissionsPostProcessor : IPostGenerateGradleAndroidProject
{
    public int callbackOrder => 1; // Run after Unity's default processors

    public void OnPostGenerateGradleAndroidProject(string path)
    {
        string manifestPath = path + "/src/main/AndroidManifest.xml";

        if (!File.Exists(manifestPath))
        {
            Debug.LogWarning($"[AndroidPermissionsPostProcessor] ⚠️ AndroidManifest.xml not found at: {manifestPath}");
            return;
        }

        Debug.Log($"[AndroidPermissionsPostProcessor] 📱 Adding UDP network permissions to: {manifestPath}");

        try
        {
            XmlDocument manifest = new XmlDocument();
            manifest.Load(manifestPath);

            XmlElement manifestElement = manifest.DocumentElement;

            if (manifestElement == null)
            {
                Debug.LogError("[AndroidPermissionsPostProcessor] ❌ Could not find manifest root element");
                return;
            }

            XmlNamespaceManager nsmgr = new XmlNamespaceManager(manifest.NameTable);
            nsmgr.AddNamespace("android", "http://schemas.android.com/apk/res/android");

            // Add required permissions for UDP networking
            AddPermissionIfMissing(manifest, manifestElement, nsmgr, "android.permission.ACCESS_NETWORK_STATE");
            AddPermissionIfMissing(manifest, manifestElement, nsmgr, "android.permission.ACCESS_WIFI_STATE");
            AddPermissionIfMissing(manifest, manifestElement, nsmgr, "android.permission.CHANGE_WIFI_STATE");
            AddPermissionIfMissing(manifest, manifestElement, nsmgr, "android.permission.WAKE_LOCK");

            manifest.Save(manifestPath);
            Debug.Log("[AndroidPermissionsPostProcessor] ✅ Successfully added UDP network permissions!");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[AndroidPermissionsPostProcessor] ❌ Failed to add permissions: {e.Message}\n{e.StackTrace}");
        }
    }

    private void AddPermissionIfMissing(XmlDocument manifest, XmlElement manifestElement, XmlNamespaceManager nsmgr, string permissionName)
    {
        // Check if permission already exists using proper namespace handling
        string xpath = $"//uses-permission[@android:name='{permissionName}']";
        XmlNode existingPermission = manifest.SelectSingleNode(xpath, nsmgr);

        if (existingPermission != null)
        {
            Debug.Log($"[AndroidPermissionsPostProcessor] ✓ Permission already exists: {permissionName}");
            return;
        }

        // Create new permission element with proper Android namespace
        XmlElement permissionElement = manifest.CreateElement("uses-permission");
        XmlAttribute nameAttribute = manifest.CreateAttribute("android", "name", "http://schemas.android.com/apk/res/android");
        nameAttribute.Value = permissionName;
        permissionElement.Attributes.Append(nameAttribute);

        // Insert before the <application> tag (best practice)
        XmlNode applicationNode = manifest.SelectSingleNode("//application");
        if (applicationNode != null)
        {
            manifestElement.InsertBefore(permissionElement, applicationNode);
        }
        else
        {
            manifestElement.AppendChild(permissionElement);
        }

        Debug.Log($"[AndroidPermissionsPostProcessor] ✅ Added permission: {permissionName}");
    }
}
