using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Item", menuName = "Inventory/Inventory Item")]
public class InventoryItem : ScriptableObject 
{
    public string itemName = "New Item";
    public Sprite icon;
    // Opcjonalnie: Mo�esz doda� referencj� do prefabrykatu, je�li chcesz m�c upu�ci� przedmiot
    // public GameObject itemPrefab;
}