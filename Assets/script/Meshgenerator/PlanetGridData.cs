using UnityEngine;

public class PlanetGridData
{
    public const int FaceCount = 6;

    private readonly int resolution;
    private readonly float[,] heights0;
    private readonly float[,] heights1;
    private readonly float[,] heights2;
    private readonly float[,] heights3;
    private readonly float[,] heights4;
    private readonly float[,] heights5;

    public int Resolution => resolution;

    public PlanetGridData(int resolution)
    {
        this.resolution = Mathf.Max(2, resolution);

        heights0 = new float[this.resolution, this.resolution];
        heights1 = new float[this.resolution, this.resolution];
        heights2 = new float[this.resolution, this.resolution];
        heights3 = new float[this.resolution, this.resolution];
        heights4 = new float[this.resolution, this.resolution];
        heights5 = new float[this.resolution, this.resolution];
    }

    public void Clear(float value = 0f)
    {
        for (int f = 0; f < FaceCount; f++)
        {
            for (int y = 0; y < resolution; y++)
            {
                for (int x = 0; x < resolution; x++)
                {
                    SetHeight(f, x, y, value);
                }
            }
        }
    }

    public float GetHeight(int face, int x, int y)
    {
        x = Mathf.Clamp(x, 0, resolution - 1);
        y = Mathf.Clamp(y, 0, resolution - 1);

        return face switch
        {
            0 => heights0[x, y],
            1 => heights1[x, y],
            2 => heights2[x, y],
            3 => heights3[x, y],
            4 => heights4[x, y],
            5 => heights5[x, y],
            _ => 0f
        };
    }

    public void SetHeight(int face, int x, int y, float value)
    {
        x = Mathf.Clamp(x, 0, resolution - 1);
        y = Mathf.Clamp(y, 0, resolution - 1);

        switch (face)
        {
            case 0: heights0[x, y] = value; break;
            case 1: heights1[x, y] = value; break;
            case 2: heights2[x, y] = value; break;
            case 3: heights3[x, y] = value; break;
            case 4: heights4[x, y] = value; break;
            case 5: heights5[x, y] = value; break;
        }
    }

    public void AddHeight(int face, int x, int y, float delta)
    {
        SetHeight(face, x, y, GetHeight(face, x, y) + delta);
    }

    // Version minimale: volcan sur UNE face uniquement (sans traverser les bords)
    public void AddVolcanoLocalFace(int face, int centerX, int centerY, int radius, float peakHeight)
    {
        radius = Mathf.Max(1, radius);
        int r2 = radius * radius;

        for (int y = centerY - radius; y <= centerY + radius; y++)
        {
            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                if (x < 0 || x >= resolution || y < 0 || y >= resolution) continue;

                int dx = x - centerX;
                int dy = y - centerY;
                int d2 = dx * dx + dy * dy;
                if (d2 > r2) continue;

                float t = 1f - Mathf.Sqrt(d2) / radius; // 1 au centre, 0 au bord
                float cone = t * peakHeight;

                // Petite caldera au centre
                float caldera = (d2 < (radius * radius * 0.12f)) ? -peakHeight * 0.25f : 0f;

                AddHeight(face, x, y, cone + caldera);
            }
        }
    }
}