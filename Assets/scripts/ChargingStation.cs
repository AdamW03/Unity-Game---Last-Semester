using UnityEngine;
using System.Collections;

[RequireComponent(typeof(Interactable))]
public class ChargingStation : MonoBehaviour
{
    [Header("Required Items")]
    [Tooltip("Telephone.")]
    public InventoryItem requiredPhoneItemData;
    [Tooltip("Charger.")]
    public InventoryItem requiredChargerItemData;

    [Header("Station Reference")]
    [Tooltip("GameObject - smartphone model.")]
    public GameObject phoneModelVisual;
    [Tooltip("GameObject - charger model.")]
    public GameObject chargerModelVisual;
    [Tooltip("GameObject - charger model.")]
    public GameObject cabelModelVisual;
    private Interactable interactable;

    [Header("Charging Settings")]
    [Tooltip("Battery charging speed per second.")]
    public float chargingRate = 5.0f;

    [Header("Interaction Texts")]
    [Tooltip("Text when the station is empty and phone can be connected.")]
    public string promptWhenEmpty = "[F] Connect phone and charger";
    [Tooltip("Text format when phone is charging. {0} = current battery, {1} = max battery.")]
    public string promptFormatWhenCharging = "[F] Charging ({0:0}/{1:0}). Retrieve";
    [Tooltip("Text when an error occurs (e.g., missing items).")]
    public string promptError = "You need a phone and a charger";
    [Tooltip("Time the error prompt is displayed (in seconds).")]
    public float errorPromptDuration = 1.5f;

    [Header("Sounds (Optional)")]
    public AudioSource audioSource;
    public AudioClip connectSound;
    public AudioClip disconnectSound;
    public AudioClip errorSound;
    public AudioClip chargingLoopSound;

    private bool isCharging = false;
    private PhoneSystem connectedPhoneInstance = null;
    private Coroutine errorPromptCoroutine = null;

    void Awake()
    {
        interactable = GetComponent<Interactable>();
        if (interactable == null)
        {
            Debug.LogError($"ChargingStation on '{gameObject.name}' did not find the Interactable component!", this);
            enabled = false;
            return;
        }

        if (requiredPhoneItemData == null || requiredChargerItemData == null)
        {
            Debug.LogError($"ChargingStation on '{gameObject.name}': 'requiredPhoneItemData' and 'requiredChargerItemData' must be assigned!", this);
            enabled = false;
            return;
        }

        if (audioSource == null)
        {
            audioSource = GetComponent<AudioSource>();
        }

        UpdateVisuals();
        UpdateInteractionPrompt(promptWhenEmpty);
    }

    public void Interact()
    {
        if (isCharging)
        {
            TryRetrieveItems();
        }
        else
        {
            TryStartCharging();
        }
    }

    private void TryStartCharging()
    {
        if (CarouselInventory.Instance == null)
        {
            Debug.LogError("CarouselInventory.Instance not found!");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        InventoryItem selectedItem = CarouselInventory.Instance.GetSelectedItem();
        if (selectedItem != requiredPhoneItemData)
        {
            Debug.Log("Player does not have the required phone selected.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        if (!CarouselInventory.Instance.HasItem(requiredChargerItemData))
        {
            Debug.Log("Player does not have a charger in inventory.");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        PhoneSystem phoneToCharge = FindSelectedPhoneSystemInstance();

        if (phoneToCharge == null)
        {
            Debug.LogError($"Active PhoneSystem instance for {requiredPhoneItemData.itemName} not found");
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        Debug.Log($"Starting charging phone: {phoneToCharge.gameObject.name}");

        bool phoneRemoved = CarouselInventory.Instance.RemoveItem(requiredPhoneItemData);
        bool chargerRemoved = CarouselInventory.Instance.RemoveItem(requiredChargerItemData);

        if (!phoneRemoved || !chargerRemoved)
        {
            Debug.LogError("Error removing items from inventory! Reverting operation.");
            if (phoneRemoved) CarouselInventory.Instance.AddItem(requiredPhoneItemData);
            if (chargerRemoved) CarouselInventory.Instance.AddItem(requiredChargerItemData);
            ShowErrorPrompt();
            PlaySound(errorSound);
            return;
        }

        isCharging = true;
        connectedPhoneInstance = phoneToCharge;

        UpdateVisuals();
        UpdateInteractionPrompt();
        PlaySound(connectSound);
        PlayChargingLoop(true);
    }

    private PhoneSystem FindSelectedPhoneSystemInstance()
    {
        PhoneSystem[] allPhoneSystems = FindObjectsOfType<PhoneSystem>();
        foreach (PhoneSystem ps in allPhoneSystems)
        {
            if (ps.phoneItemData == requiredPhoneItemData)
            {
                return ps;
            }
        }
        return null;
    }

    private void TryRetrieveItems()
    {
        if (!isCharging || connectedPhoneInstance == null)
        {
            Debug.LogWarning("Attempted to retrieve items when station is not charging or phone reference is null.");
            return;
        }

        Debug.Log($"Retrieving phone '{connectedPhoneInstance.gameObject.name}' and charger.");

        CarouselInventory.Instance.AddItem(requiredPhoneItemData);
        CarouselInventory.Instance.AddItem(requiredChargerItemData);

        isCharging = false;
        connectedPhoneInstance = null;

        UpdateVisuals();
        UpdateInteractionPrompt(promptWhenEmpty);
        PlaySound(disconnectSound);
        PlayChargingLoop(false);
    }

    void Update()
    {
        if (isCharging && connectedPhoneInstance != null)
        {
            connectedPhoneInstance.ChargeBattery(chargingRate * Time.deltaTime);
            UpdateInteractionPrompt();
        }
    }

    private void UpdateVisuals()
    {
        if (phoneModelVisual != null)
        {
            phoneModelVisual.SetActive(isCharging);
        }
        if (chargerModelVisual != null)
        {
            chargerModelVisual.SetActive(isCharging);
        }
        if (cabelModelVisual != null)
        {
            cabelModelVisual.SetActive(isCharging);
        }
    }

    private void UpdateInteractionPrompt(string customText = null)
    {
        if (interactable == null) return;

        if (!string.IsNullOrEmpty(customText))
        {
            interactable.interactionPrompt = customText;
        }
        else
        {
            if (isCharging && connectedPhoneInstance != null)
            {
                float current = connectedPhoneInstance.GetCurrentBattery();
                float max = connectedPhoneInstance.GetMaxBattery();
                interactable.interactionPrompt = string.Format(promptFormatWhenCharging, current, max);
            }
            else
            {
                interactable.interactionPrompt = promptWhenEmpty;
            }
        }
    }

    private void ShowErrorPrompt()
    {
        if (errorPromptCoroutine != null)
        {
            StopCoroutine(errorPromptCoroutine);
        }
        errorPromptCoroutine = StartCoroutine(ErrorPromptRoutine());
    }

    IEnumerator ErrorPromptRoutine()
    {
        string originalPrompt = interactable.interactionPrompt;
        UpdateInteractionPrompt(promptError);
        yield return new WaitForSeconds(errorPromptDuration);
        if (!isCharging)
        {
            UpdateInteractionPrompt(originalPrompt);
        }
        errorPromptCoroutine = null;
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
        {
            audioSource.PlayOneShot(clip);
        }
    }

    private void PlayChargingLoop(bool play)
    {
        if (audioSource != null && chargingLoopSound != null)
        {
            if (play && !audioSource.isPlaying)
            {
                audioSource.clip = chargingLoopSound;
                audioSource.loop = true;
                audioSource.Play();
            }
            else if (!play && audioSource.clip == chargingLoopSound)
            {
                audioSource.Stop();
                audioSource.loop = false;
                audioSource.clip = null;
            }
        }
    }
}
