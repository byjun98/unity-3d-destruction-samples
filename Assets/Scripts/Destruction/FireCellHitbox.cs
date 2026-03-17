using UnityEngine;

public sealed class FireCellHitbox : MonoBehaviour
{
    [SerializeField] private int x;
    [SerializeField] private int y;

    public int X => x;
    public int Y => y;

    public void SetCoordinates(int cellX, int cellY)
    {
        x = cellX;
        y = cellY;
    }
}
