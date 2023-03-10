using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Class <c>PositionsManager</c> Stores the points of the original line drawn.
/// </summary>
public class PositionsManager : MonoBehaviour
{
    [SerializeField] private Vector3[] oldPositions;
    public void SetOldPositions(Vector3[] positions)
    {
        oldPositions = positions;
    }

    public Vector3[] GetOldPositions()
    {
        return oldPositions;
    }
}
