using UnityEngine;

[CreateAssetMenu(fileName = "New Inventory Item", menuName = "Inventory/Inventory Item")]
public class InventoryItem : ScriptableObject 
{
    public string itemName = "New Item";
    public Sprite icon;
    // Opcjonalnie: Mo¿esz dodaæ referencjê do prefabrykatu, jeœli chcesz móc upuœciæ przedmiot
    // public GameObject itemPrefab;
}