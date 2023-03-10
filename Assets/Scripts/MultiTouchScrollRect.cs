/*
 * The following code was taken from StackOverflow with light modification.
 * All code placed in StackOverflow is licenced under CC BY-SA 4.0 and
 * must be attributed properly.
 *
 * Attribution:
 * This work, "MultiTouchScrollRect", is a derivative of "MultiTouchScrollRect.cs script" by Daniel on StackOverflow, used under CC BY-SA 4.0.
 *  "MultiTouchScrollRect" is licensed under CC BY-SA 4.0 by David Porfirio.
 *  Original URL: https://stackoverflow.com/questions/56221113/fix-for-scrollrect-multi-touch-in-unity
 *  Author URL: https://stackoverflow.com/users/7840247/daniel
 *  License URL: https://stackoverflow.com/help/licensing
 * 
 * ShareAlike terms:
 * This code therefore has the following license as the original:
 * CC BY-SA 4.0
 * The license can be found in the third_party_licenses.txt under "MultiTouchScrollRect.cs script".
 *
 * + + + + + + + + + + + + + + 
 *
 * The author of the code on StackOverflow, from which the modified version
 * below was derived, originally obtained a version of the code from the
 * Unity-UI-Extensions bitbucket page:
 * https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/wiki/Controls/MultiTouchScrollRect
 * 
 * All code in the bitbucket page is licenced under BSD3
 * https://bitbucket.org/UnityUIExtensions/unity-ui-extensions/src/release/LICENSE.md
 *
 * The license can be found in the third_party_licenses.txt under "Unity-UI-Extensions"
 * 
 */

using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class MultiTouchScrollRect : ScrollRect
{
    private int minimumTouchCount = 2, maximumTouchCount = 2, pointerId = -100;

    public Vector2 MultiTouchPosition
    {
        get
        {
            Vector2 position = Vector2.zero;
            for (int i = 0; i < Input.touchCount && i < maximumTouchCount; i++)
            {
                position += Input.touches[i].position;
            }
            position /= ((Input.touchCount <= maximumTouchCount) ? Input.touchCount : maximumTouchCount);
            return position;
        }
    }

    public override void OnBeginDrag(PointerEventData eventData)
    {
        if (Input.touchCount >= minimumTouchCount)
        {
            pointerId = eventData.pointerId;
            eventData.position = MultiTouchPosition;
            base.OnBeginDrag(eventData);
        }
    }
    public override void OnDrag(PointerEventData eventData)
    {
        if (Input.touchCount >= minimumTouchCount)
        {
            eventData.position = MultiTouchPosition;
            if (pointerId == eventData.pointerId)
            {
                base.OnDrag(eventData);
            }
        }
    }
    public override void OnEndDrag(PointerEventData eventData)
    {
        if (Input.touchCount >= minimumTouchCount)
        {
            pointerId = -100;
            eventData.position = MultiTouchPosition;
            base.OnEndDrag(eventData);
        }
    }
}