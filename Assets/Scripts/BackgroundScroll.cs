using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Class <c>BackgroundScroll</c> sets the position of the regions to be
///  the same as the positions of the background panel.
/// </summary>
public class BackgroundScroll : MonoBehaviour, IDragHandler
{
    public RectTransform regions;
    public RectTransform backgroundPanel;

    public virtual void OnDrag(PointerEventData eventData)
    {
        regions.position = backgroundPanel.position;
    }
}
