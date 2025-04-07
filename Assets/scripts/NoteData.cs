using UnityEngine;

[CreateAssetMenu(fileName = "New Note", menuName = "Inventory/Note Data")]
public class NoteData : ScriptableObject
{
    [Tooltip("Unikalny numer strony, wg którego notatki bêd¹ sortowane.")]
    public int pageNumber;

    [Tooltip("Tytu³ notatki (opcjonalny, mo¿e byæ u¿yty w przysz³oœci).")]
    public string noteTitle = "Notatka";

    [Tooltip("Pe³na treœæ notatki.")]
    [TextArea(10, 20)] // Umo¿liwia wpisywanie wielu linii w Inspektorze
    public string noteContent;
}