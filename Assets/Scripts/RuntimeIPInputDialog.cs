// RuntimeIPInputDialog.cs
// Mobile-compatible IP input dialog using Unity UI
// (C) 2024-2025 G8gaming Ltd.

using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Runtime IP input dialog that works on mobile and all platforms
/// Uses Unity's built-in UI system (Canvas, InputField, Button)
/// </summary>
public class RuntimeIPInputDialog : MonoBehaviour
{
    private InputField ipInputField;
    private System.Action<string> callback;
    private System.Action onCancel;
    private GameObject dialogPanel;

    public void Show(string defaultIP, System.Action<string> onConfirm, System.Action onCancelCallback = null)
    {
        callback = onConfirm;
        onCancel = onCancelCallback;
        CreateDialogUI(defaultIP);
    }

    private string GetLocalIPAddress()
    {
        try
        {
            // Use NetworkInterface for more reliable detection
            // This allows us to prioritize WiFi/Ethernet over virtual adapters
            var interfaces = System.Net.NetworkInformation.NetworkInterface.GetAllNetworkInterfaces();

            // First pass: Look for WiFi or Ethernet interfaces on 192.168.x.x network
            foreach (var ni in interfaces)
            {
                Debug.Log($"[RuntimeIPInputDialog] Checking interface: {ni.Name}, Type: {ni.NetworkInterfaceType}, Status: {ni.OperationalStatus}");

                // Only check active, non-loopback interfaces
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    // Prefer WiFi and Ethernet interfaces
                    bool isPreferredType =
                        ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 ||
                        ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet ||
                        ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet;

                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !System.Net.IPAddress.IsLoopback(addr.Address))
                        {
                            string ipString = addr.Address.ToString();
                            Debug.Log($"[RuntimeIPInputDialog]   Found IP: {ipString} on {ni.Name} (Type: {ni.NetworkInterfaceType})");

                            // Prioritize 192.168.x.x addresses on preferred interfaces
                            if (isPreferredType && (ipString.StartsWith("192.168.") || ipString.StartsWith("10.") || ipString.StartsWith("172.")))
                            {
                                Debug.Log($"[RuntimeIPInputDialog] ✅ Selected preferred network IP: {ipString}");
                                return ipString;
                            }
                        }
                    }
                }
            }

            // Second pass: Accept any non-loopback IP on preferred interface types
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    (ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Wireless80211 ||
                     ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.Ethernet ||
                     ni.NetworkInterfaceType == System.Net.NetworkInformation.NetworkInterfaceType.GigabitEthernet))
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !System.Net.IPAddress.IsLoopback(addr.Address))
                        {
                            string ipString = addr.Address.ToString();
                            Debug.Log($"[RuntimeIPInputDialog] ✅ Selected network IP (2nd pass): {ipString}");
                            return ipString;
                        }
                    }
                }
            }

            // Third pass: Any non-loopback IP as last resort
            foreach (var ni in interfaces)
            {
                if (ni.OperationalStatus == System.Net.NetworkInformation.OperationalStatus.Up &&
                    ni.NetworkInterfaceType != System.Net.NetworkInformation.NetworkInterfaceType.Loopback)
                {
                    foreach (var addr in ni.GetIPProperties().UnicastAddresses)
                    {
                        if (addr.Address.AddressFamily == System.Net.Sockets.AddressFamily.InterNetwork &&
                            !System.Net.IPAddress.IsLoopback(addr.Address))
                        {
                            string ipString = addr.Address.ToString();
                            Debug.Log($"[RuntimeIPInputDialog] ⚠️ Using fallback IP: {ipString}");
                            return ipString;
                        }
                    }
                }
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"Could not get local IP: {e.Message}");
        }
        return "Unknown";
    }

    private void CreateDialogUI(string defaultIP)
    {
        Canvas canvas = GetComponent<Canvas>();
        if (canvas == null)
        {
            Debug.LogError("RuntimeIPInputDialog requires a Canvas component!");
            return;
        }

        // Create background panel (larger size for better visibility)
        dialogPanel = new GameObject("Dialog Panel");
        dialogPanel.transform.SetParent(transform, false);

        RectTransform panelRect = dialogPanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0.5f);
        panelRect.anchorMax = new Vector2(0.5f, 0.5f);
        panelRect.pivot = new Vector2(0.5f, 0.5f);
        panelRect.sizeDelta = new Vector2(600, 400); // Increased from 400x300 to 600x400

        Image panelImage = dialogPanel.AddComponent<Image>();
        panelImage.color = new Color(0.2f, 0.2f, 0.2f, 0.95f);

        // Title text
        GameObject titleObj = new GameObject("Title");
        titleObj.transform.SetParent(dialogPanel.transform, false);

        RectTransform titleRect = titleObj.AddComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0, -30);
        titleRect.sizeDelta = new Vector2(550, 60);

        Text titleText = titleObj.AddComponent<Text>();
        titleText.text = "Enter Target IP Address";
        titleText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        titleText.fontSize = 32; // Increased from 24
        titleText.color = Color.white;
        titleText.alignment = TextAnchor.MiddleCenter;

        // Device IP info text
        GameObject deviceIPObj = new GameObject("Device IP");
        deviceIPObj.transform.SetParent(dialogPanel.transform, false);

        RectTransform deviceIPRect = deviceIPObj.AddComponent<RectTransform>();
        deviceIPRect.anchorMin = new Vector2(0.5f, 1f);
        deviceIPRect.anchorMax = new Vector2(0.5f, 1f);
        deviceIPRect.pivot = new Vector2(0.5f, 1f);
        deviceIPRect.anchoredPosition = new Vector2(0, -100);
        deviceIPRect.sizeDelta = new Vector2(550, 40);

        Text deviceIPText = deviceIPObj.AddComponent<Text>();
        string localIP = GetLocalIPAddress();
        deviceIPText.text = $"This device's IP: {localIP}";
        deviceIPText.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        deviceIPText.fontSize = 22; // Increased from 16
        deviceIPText.color = new Color(0.7f, 0.9f, 1f); // Light blue
        deviceIPText.alignment = TextAnchor.MiddleCenter;
        deviceIPText.fontStyle = FontStyle.Italic;

        // Input field
        GameObject inputFieldObj = new GameObject("IP Input Field");
        inputFieldObj.transform.SetParent(dialogPanel.transform, false);

        RectTransform inputRect = inputFieldObj.AddComponent<RectTransform>();
        inputRect.anchorMin = new Vector2(0.5f, 0.5f);
        inputRect.anchorMax = new Vector2(0.5f, 0.5f);
        inputRect.pivot = new Vector2(0.5f, 0.5f);
        inputRect.anchoredPosition = new Vector2(0, 20);
        inputRect.sizeDelta = new Vector2(550, 60); // Increased from 350x40

        Image inputBg = inputFieldObj.AddComponent<Image>();
        inputBg.color = Color.white;

        ipInputField = inputFieldObj.AddComponent<InputField>();
        ipInputField.textComponent = CreateInputText(inputFieldObj.transform);
        ipInputField.text = defaultIP;
        ipInputField.contentType = InputField.ContentType.Standard;
        ipInputField.characterLimit = 15; // Max length for IP (xxx.xxx.xxx.xxx)

        // Placeholder
        Text placeholder = CreatePlaceholderText(inputFieldObj.transform);
        ipInputField.placeholder = placeholder;

        // OK Button
        CreateButton("OK", new Vector2(-150, -100), OnOKPressed);

        // Cancel Button
        CreateButton("Cancel", new Vector2(150, -100), OnCancelPressed);

        Debug.Log("✅ Runtime IP input dialog created");
    }

    private Text CreateInputText(Transform parent)
    {
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(parent, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        Text text = textObj.AddComponent<Text>();
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28; // Increased from 20
        text.color = Color.black;
        text.alignment = TextAnchor.MiddleLeft;
        text.supportRichText = false;

        return text;
    }

    private Text CreatePlaceholderText(Transform parent)
    {
        GameObject placeholderObj = new GameObject("Placeholder");
        placeholderObj.transform.SetParent(parent, false);

        RectTransform placeholderRect = placeholderObj.AddComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(10, 5);
        placeholderRect.offsetMax = new Vector2(-10, -5);

        Text placeholder = placeholderObj.AddComponent<Text>();
        placeholder.text = "192.168.1.100";
        placeholder.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        placeholder.fontSize = 28; // Increased from 20
        placeholder.color = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        placeholder.alignment = TextAnchor.MiddleLeft;
        placeholder.supportRichText = false;

        return placeholder;
    }

    private void CreateButton(string buttonText, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObj = new GameObject($"{buttonText} Button");
        buttonObj.transform.SetParent(dialogPanel.transform, false);

        RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(0.5f, 0f);
        buttonRect.anchorMax = new Vector2(0.5f, 0f);
        buttonRect.pivot = new Vector2(0.5f, 0f);
        buttonRect.anchoredPosition = position;
        buttonRect.sizeDelta = new Vector2(220, 60); // Increased from 150x40

        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = buttonText == "OK" ? new Color(0.2f, 0.6f, 0.2f) : new Color(0.6f, 0.2f, 0.2f);

        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = buttonImage;
        button.onClick.AddListener(onClick);

        // Button text
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = Vector2.zero;
        textRect.offsetMax = Vector2.zero;

        Text text = textObj.AddComponent<Text>();
        text.text = buttonText;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 26; // Increased from 18
        text.color = Color.white;
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
    }

    private void OnOKPressed()
    {
        string ipAddress = ipInputField.text.Trim();

        if (string.IsNullOrEmpty(ipAddress))
        {
            Debug.LogWarning("⚠️ IP address cannot be empty!");
            return;
        }

        Debug.Log($"✅ IP address entered: {ipAddress}");
        callback?.Invoke(ipAddress);
        CloseDialog();
    }

    private void OnCancelPressed()
    {
        Debug.Log("❌ IP input cancelled");

        // Invoke cancel callback if provided
        if (onCancel != null)
        {
            onCancel.Invoke();
        }
        else
        {
            // Fallback to old behavior
            callback?.Invoke(null);
        }

        CloseDialog();
    }

    private void CloseDialog()
    {
        if (dialogPanel != null)
        {
            Destroy(dialogPanel);
        }

        // Destroy the canvas after a short delay to ensure callback completes
        Destroy(gameObject, 0.1f);
    }
}
