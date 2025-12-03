using UnityEngine;

[CreateAssetMenu(fileName = "ElementType", menuName = "Systems/Elements/New Element Type")]
public class ElementType : ScriptableObject, IElementType
{
    [TextArea]
    public string Description;
}
