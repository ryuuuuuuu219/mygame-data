using UnityEngine;

[System.Serializable]
public class CloudPreset
{
    public string name;
    public int sizeX;
    public int sizeY;
    public int sizeZ;
    public float cellSize;
    public bool[,,] cells;

    public CloudPreset(string name, int sizeX, int sizeY, int sizeZ, float cellSize)
    {
        this.name = name;
        this.sizeX = sizeX;
        this.sizeY = sizeY;
        this.sizeZ = sizeZ;
        this.cellSize = cellSize;
        cells = new bool[sizeX, sizeY, sizeZ];
    }

    public bool IsFilled(int x, int y, int z)
    {
        if (!Contains(x, y, z))
        {
            return false;
        }

        return cells[x, y, z];
    }

    public void SetFilled(int x, int y, int z, bool filled)
    {
        if (!Contains(x, y, z))
        {
            return;
        }

        cells[x, y, z] = filled;
    }

    public bool Contains(int x, int y, int z)
    {
        return x >= 0 && x < sizeX &&
            y >= 0 && y < sizeY &&
            z >= 0 && z < sizeZ;
    }
}
