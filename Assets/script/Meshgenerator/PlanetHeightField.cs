using System;
using UnityEngine;
using UnityEngine.Rendering;

/// <summary>
/// Champ de hauteur planétaire en projection équirectangulaire.
/// X = longitude (0..2π, wrap), Y = latitude (-π/2..π/2, clamp aux pôles).
/// Le relief n'est pas appliqué à la géométrie: il est exposé via une Texture2D
/// RFloat consommée par le shader (normales par différences finies + rampe de couleur).
/// La croissance est progressive: currentHeight -> targetHeight via StepGrowth().
/// </summary>
public class PlanetHeightField : IDisposable
{
    private readonly int width;
    private readonly int height;

    private readonly float[] currentHeight;
    private readonly float[] targetHeight;

    // Vitesse relative de croissance par texel (volcan rapide, continent lent).
    private readonly float[] growthRate;

    private Texture2D heightTex;
    private Texture2D stagingTex;
    private float[] stagingBuffer;

    // Région (circulaire en X) restant à faire converger.
    private bool hasActiveRegion;
    private int activeStartX;
    private int activeSpanX;
    private int activeMinY;
    private int activeMaxY;

    // Nombre de lignes forcées plates à chaque pôle (évite les artefacts de convergence).
    private readonly int poleRows;
    private readonly float poleHeight;

    public int Width => width;
    public int Height => height;
    public Texture2D HeightTex => heightTex;
    public bool HasActiveRegion => hasActiveRegion;
    public Vector3 NoiseOffset { get; set; } = Vector3.zero;

    public event Action OnCleared;

    public PlanetHeightField(int width = 512, int height = 0, int poleRows = 2, float poleHeight = 0f)
    {
        this.width = Mathf.Max(8, width);
        this.height = height > 0 ? Mathf.Max(4, height) : Mathf.Max(4, this.width / 2);
        this.poleRows = Mathf.Clamp(poleRows, 0, this.height / 4);
        this.poleHeight = poleHeight;

        currentHeight = new float[this.width * this.height];
        targetHeight = new float[this.width * this.height];
        growthRate = new float[this.width * this.height];

        CreateTexture();
        FlattenPoles();
        UploadAll();
    }

    private void CreateTexture()
    {
        heightTex = new Texture2D(width, height, TextureFormat.RFloat, false, true)
        {
            name = "PlanetHeightField",
            wrapModeU = TextureWrapMode.Repeat,   // couture de longitude
            wrapModeV = TextureWrapMode.Clamp,    // pôles
            filterMode = FilterMode.Bilinear,
            anisoLevel = 0
        };
    }

    public float GetCurrent(int x, int y) => currentHeight[Index(x, y)];
    public float GetTarget(int x, int y) => targetHeight[Index(x, y)];

    private int Index(int x, int y)
    {
        x = WrapX(x);
        y = Mathf.Clamp(y, 0, height - 1);
        return x + y * width;
    }

    private int WrapX(int x)
    {
        x %= width;
        if (x < 0) x += width;
        return x;
    }

    /// <summary>Longitude (radians, 0..2π) du centre du texel x.</summary>
    public float LongitudeOf(int x) => (x + 0.5f) / width * (2f * Mathf.PI);

    /// <summary>Latitude (radians, -π/2..π/2) du centre du texel y.</summary>
    public float LatitudeOf(int y) => ((y + 0.5f) / height - 0.5f) * Mathf.PI;

    /// <summary>
    /// Volcan: cône + petite caldera centrale (même logique que AddVolcanoLocalFace,
    /// mais en distance angulaire sur la sphère pour rester correct près des pôles).
    /// </summary>
    /// <param name="longitude">Longitude en radians.</param>
    /// <param name="latitude">Latitude en radians (-π/2..π/2).</param>
    /// <param name="radius">Rayon angulaire en radians.</param>
    /// <param name="peakHeight">Hauteur au sommet.</param>
    /// <param name="rate">Vitesse relative de croissance (multipliée par le speed de StepGrowth).</param>
    public void AddVolcano(float longitude, float latitude, float radius, float peakHeight, float rate = 1f)
    {
        radius = Mathf.Max(1e-4f, radius);

        StampAngular(longitude, latitude, radius, rate, (t, d) =>
        {
            float cone = t * peakHeight;
            float caldera = d < radius * 0.35f ? -peakHeight * 0.25f : 0f;
            return cone + caldera;
        });
    }

    /// <summary>
    /// Cratère d'impact radial : dépression centrale en forme de bol parabolique
    /// et lèvre/bordure surélevée culminant à 0.75 du rayon de l'impact.
    /// </summary>
    public void AddCrater(float longitude, float latitude, float radius, float depth, float rimHeight, float rate = 1f)
    {
        radius = Mathf.Max(1e-4f, radius);

        StampAngular(longitude, latitude, radius, rate, (t, d) =>
        {
            float u = d / radius;
            if (u < 0.6f)
            {
                // Cavité : forme de bol descendant jusqu'à -depth
                float bowl = u / 0.6f;
                return -depth * (1f - bowl * bowl);
            }
            else if (u < 1.0f)
            {
                // Bordure : lèvre surélevée culminant à u = 0.75
                if (u < 0.75f)
                {
                    float bowl = (u - 0.6f) / 0.15f;
                    return rimHeight * Mathf.SmoothStep(0f, 1f, bowl);
                }
                else
                {
                    float bowl = (u - 0.75f) / 0.25f;
                    return rimHeight * Mathf.SmoothStep(1f, 0f, bowl);
                }
            }
            return 0f;
        });
    }

    /// <summary>
    /// Continent: relief large et doux avec bruit 3D, destiné à une croissance lente.
    /// </summary>
    public void AddContinent(float longitude, float latitude, float radius, float plateauHeight, float rate = 0.1f)
    {
        radius = Mathf.Max(1e-4f, radius);

        StampAngularContinent(longitude, latitude, radius, plateauHeight, rate);
    }

    private static float Hash3D(int x, int y, int z)
    {
        long h = (x * 73856093L) ^ (y * 19349663L) ^ (z * 83492791L);
        h = (h ^ (h >> 16)) * 0x85ebca6bL;
        h = (h ^ (h >> 13)) * 0xc2b2ae35L;
        float val = (h & 0xfffffff) / (float)0xfffffff;
        return val * 2.0f - 1.0f;
    }

    private static float Noise3D(float x, float y, float z)
    {
        int ix = Mathf.FloorToInt(x);
        int iy = Mathf.FloorToInt(y);
        int iz = Mathf.FloorToInt(z);

        float fx = x - ix;
        float fy = y - iy;
        float fz = z - iz;

        float ux = fx * fx * (3.0f - 2.0f * fx);
        float uy = fy * fy * (3.0f - 2.0f * fy);
        float uz = fz * fz * (3.0f - 2.0f * fz);

        float n000 = Hash3D(ix,     iy,     iz);
        float n100 = Hash3D(ix + 1, iy,     iz);
        float n010 = Hash3D(ix,     iy + 1, iz);
        float n110 = Hash3D(ix + 1, iy + 1, iz);
        float n001 = Hash3D(ix,     iy,     iz + 1);
        float n101 = Hash3D(ix + 1, iy,     iz + 1);
        float n011 = Hash3D(ix,     iy + 1, iz + 1);
        float n111 = Hash3D(ix + 1, iy + 1, iz + 1);

        float n00 = Mathf.Lerp(n000, n100, ux);
        float n10 = Mathf.Lerp(n010, n110, ux);
        float n01 = Mathf.Lerp(n001, n101, ux);
        float n11 = Mathf.Lerp(n011, n111, ux);

        float n0 = Mathf.Lerp(n00, n10, uy);
        float n1 = Mathf.Lerp(n01, n11, uy);

        return Mathf.Lerp(n0, n1, uz);
    }

    private static float Fbm3D(float x, float y, float z, int octaves = 4)
    {
        float value = 0f;
        float amplitude = 0.5f;
        float frequency = 1.0f;
        for (int i = 0; i < octaves; i++)
        {
            value += amplitude * Noise3D(x * frequency, y * frequency, z * frequency);
            frequency *= 2.0f;
            amplitude *= 0.5f;
        }
        return value;
    }

    /// <summary>
    /// Applique un motif de continent avec déformation de domaine 3D (domain warping)
    /// et bruit multi-échelle pour éliminer les formes circulaires régulières et
    /// générer des masses continentales naturelles (péninsules, golfes, baies).
    /// </summary>
    private void StampAngularContinent(float longitude, float latitude, float radius, float plateauHeight, float rate)
    {
        rate = Mathf.Max(1e-4f, rate);
        latitude = Mathf.Clamp(latitude, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        float cosLat = Mathf.Cos(latitude);
        float sinLat = Mathf.Sin(latitude);

        // Direction 3D du centre du continent
        Vector3 centerDir = new Vector3(
            cosLat * Mathf.Cos(longitude),
            sinLat,
            cosLat * Mathf.Sin(longitude)
        );

        int centerX = Mathf.RoundToInt(longitude / (2f * Mathf.PI) * width);
        int centerY = Mathf.RoundToInt((latitude / Mathf.PI + 0.5f) * height);

        // Augmentation de l'emprise pour englober la déformation de domaine (domain warp)
        float spanFactor = 2.2f;
        int spanY = Mathf.CeilToInt((radius * spanFactor) / Mathf.PI * height) + 1;
        int minY = Mathf.Clamp(centerY - spanY, 0, height - 1);
        int maxY = Mathf.Clamp(centerY + spanY, 0, height - 1);

        int spanX;
        float cosClamped = Mathf.Max(cosLat, 1e-3f);
        float angularSpanX = (radius * spanFactor) / cosClamped;
        if (angularSpanX >= Mathf.PI)
        {
            spanX = width / 2;
        }
        else
        {
            spanX = Mathf.CeilToInt(angularSpanX / (2f * Mathf.PI) * width) + 1;
        }

        int startX = WrapX(centerX - spanX);
        int columns = Mathf.Min(width, spanX * 2 + 1);

        float warpStrength = radius * 0.45f;

        for (int y = minY; y <= maxY; y++)
        {
            if (IsPoleRow(y)) continue;

            float lat = LatitudeOf(y);
            float cosRowLat = Mathf.Cos(lat);
            float sinRowLat = Mathf.Sin(lat);

            float yDir = sinRowLat;

            for (int c = 0; c < columns; c++)
            {
                int x = WrapX(startX + c);
                float lon = LongitudeOf(x);

                float xDir = cosRowLat * Mathf.Cos(lon);
                float zDir = cosRowLat * Mathf.Sin(lon);

                Vector3 pos = new Vector3(xDir, yDir, zDir);

                // 1. Déformation de domaine (3D Domain Warping) à basse fréquence
                float wx = Fbm3D((pos.x + NoiseOffset.x + 13.5f) * 2.2f, (pos.y + NoiseOffset.y + 27.1f) * 2.2f, (pos.z + NoiseOffset.z + 41.8f) * 2.2f, 3);
                float wy = Fbm3D((pos.x + NoiseOffset.x + 52.3f) * 2.2f, (pos.y + NoiseOffset.y + 68.9f) * 2.2f, (pos.z + NoiseOffset.z + 84.2f) * 2.2f, 3);
                float wz = Fbm3D((pos.x + NoiseOffset.x + 91.7f) * 2.2f, (pos.y + NoiseOffset.y + 14.3f) * 2.2f, (pos.z + NoiseOffset.z + 36.6f) * 2.2f, 3);

                Vector3 warpedPos = (pos + new Vector3(wx, wy, wz) * warpStrength).normalized;

                // Distance en espace déformé par rapport au centre du continent
                float cosD = Vector3.Dot(warpedPos, centerDir);
                float d = Mathf.Acos(Mathf.Clamp(cosD, -1f, 1f));

                // 2. Bruit de côtes fractales à haute fréquence
                float coastlineNoise = Fbm3D((pos.x + NoiseOffset.x) * 8.0f, (pos.y + NoiseOffset.y) * 8.0f, (pos.z + NoiseOffset.z) * 8.0f, 4);
                float perturbedRadius = radius * (1.0f + coastlineNoise * 0.35f);

                if (d > perturbedRadius) continue;

                float t = 1f - d / perturbedRadius;
                float heightFactor = Mathf.SmoothStep(0f, 1f, t);

                // Relief intérieur (plateaux, vallées et chaînes montagneuses)
                float internalNoise = Fbm3D((pos.x + NoiseOffset.x) * 12.0f, (pos.y + NoiseOffset.y) * 12.0f, (pos.z + NoiseOffset.z) * 12.0f, 4);
                float finalHeight = heightFactor * plateauHeight * (1.0f + internalNoise * 0.25f);

                int i = x + y * width;
                targetHeight[i] += finalHeight;
                growthRate[i] = Mathf.Max(growthRate[i], rate);
            }
        }

        MarkActive(startX, columns, minY, maxY);
    }

    /// <summary>
    /// Applique un motif radial sur targetHeight autour de (longitude, latitude).
    /// contribution(t, d): t = 1 au centre -> 0 au bord, d = distance angulaire.
    /// </summary>
    private void StampAngular(float longitude, float latitude, float radius, float rate, Func<float, float, float> contribution)
    {
        rate = Mathf.Max(1e-4f, rate);

        latitude = Mathf.Clamp(latitude, -Mathf.PI * 0.5f, Mathf.PI * 0.5f);

        float cosLat = Mathf.Cos(latitude);
        float sinLat = Mathf.Sin(latitude);

        int centerX = Mathf.RoundToInt(longitude / (2f * Mathf.PI) * width);
        int centerY = Mathf.RoundToInt((latitude / Mathf.PI + 0.5f) * height);

        int spanY = Mathf.CeilToInt(radius / Mathf.PI * height) + 1;
        int minY = Mathf.Clamp(centerY - spanY, 0, height - 1);
        int maxY = Mathf.Clamp(centerY + spanY, 0, height - 1);

        // Extension en longitude: la maille se resserre vers les pôles.
        int spanX;
        float cosClamped = Mathf.Max(cosLat, 1e-3f);
        float angularSpanX = radius / cosClamped;
        if (angularSpanX >= Mathf.PI)
        {
            spanX = width / 2;
        }
        else
        {
            spanX = Mathf.CeilToInt(angularSpanX / (2f * Mathf.PI) * width) + 1;
        }

        int startX = WrapX(centerX - spanX);
        int columns = Mathf.Min(width, spanX * 2 + 1);

        for (int y = minY; y <= maxY; y++)
        {
            if (IsPoleRow(y)) continue;

            float lat = LatitudeOf(y);
            float cosRowLat = Mathf.Cos(lat);
            float sinRowLat = Mathf.Sin(lat);

            for (int c = 0; c < columns; c++)
            {
                int x = WrapX(startX + c);
                float lon = LongitudeOf(x);

                // Distance angulaire (loi des cosinus sphérique).
                float cosD = sinLat * sinRowLat + cosLat * cosRowLat * Mathf.Cos(lon - longitude);
                float d = Mathf.Acos(Mathf.Clamp(cosD, -1f, 1f));
                if (d > radius) continue;

                float t = 1f - d / radius;
                int i = x + y * width;
                targetHeight[i] += contribution(t, d);
                growthRate[i] = Mathf.Max(growthRate[i], rate);
            }
        }

        MarkActive(startX, columns, minY, maxY);
    }

    /// <summary>
    /// Fait converger currentHeight vers targetHeight sur la région active,
    /// puis n'uploade que la sous-région modifiée du heightmap.
    /// </summary>
    public void StepGrowth(float speed, float dt)
    {
        if (!hasActiveRegion || heightTex == null) return;

        float step = Mathf.Abs(speed) * Mathf.Max(0f, dt);
        if (step <= 0f) return;

        bool anyChange = false;
        bool anyPending = false;

        int newMinY = int.MaxValue;
        int newMaxY = int.MinValue;
        int firstChangedColumn = int.MaxValue;
        int lastChangedColumn = int.MinValue;

        for (int y = activeMinY; y <= activeMaxY; y++)
        {
            int row = y * width;
            for (int c = 0; c < activeSpanX; c++)
            {
                int i = WrapX(activeStartX + c) + row;

                float cur = currentHeight[i];
                float tgt = targetHeight[i];
                if (Mathf.Approximately(cur, tgt)) continue;

                float maxDelta = step * Mathf.Max(growthRate[i], 1e-4f);
                currentHeight[i] = Mathf.MoveTowards(cur, tgt, maxDelta);
                anyChange = true;

                if (!Mathf.Approximately(currentHeight[i], tgt))
                {
                    anyPending = true;
                    if (y < newMinY) newMinY = y;
                    if (y > newMaxY) newMaxY = y;
                    if (c < firstChangedColumn) firstChangedColumn = c;
                    if (c > lastChangedColumn) lastChangedColumn = c;
                }
            }
        }

        if (anyChange)
        {
            UploadRegion(activeStartX, activeSpanX, activeMinY, activeMaxY);
        }

        if (anyPending)
        {
            // Rétrécit la région active autour de ce qui reste à faire converger.
            activeStartX = WrapX(activeStartX + firstChangedColumn);
            activeSpanX = lastChangedColumn - firstChangedColumn + 1;
            activeMinY = newMinY;
            activeMaxY = newMaxY;
        }
        else
        {
            hasActiveRegion = false;
        }
    }

    /// <summary>Applique instantanément la cible (utile pour l'initialisation).</summary>
    public void SnapToTarget()
    {
        Array.Copy(targetHeight, currentHeight, currentHeight.Length);
        FlattenPoles();
        UploadAll();
        hasActiveRegion = false;
    }

    public void Clear(float value = 0f)
    {
        for (int i = 0; i < currentHeight.Length; i++)
        {
            currentHeight[i] = value;
            targetHeight[i] = value;
            growthRate[i] = 0f;
        }

        FlattenPoles();
        UploadAll();
        hasActiveRegion = false;

        OnCleared?.Invoke();
    }

    private bool IsPoleRow(int y) => y < poleRows || y >= height - poleRows;

    private void FlattenPoles()
    {
        if (poleRows <= 0) return;

        for (int y = 0; y < height; y++)
        {
            if (!IsPoleRow(y)) continue;

            int row = y * width;
            for (int x = 0; x < width; x++)
            {
                currentHeight[x + row] = poleHeight;
                targetHeight[x + row] = poleHeight;
            }
        }
    }

    private void MarkActive(int startX, int spanX, int minY, int maxY)
    {
        spanX = Mathf.Clamp(spanX, 1, width);
        startX = WrapX(startX);
        minY = Mathf.Clamp(minY, 0, height - 1);
        maxY = Mathf.Clamp(maxY, 0, height - 1);

        if (!hasActiveRegion)
        {
            hasActiveRegion = true;
            activeStartX = startX;
            activeSpanX = spanX;
            activeMinY = minY;
            activeMaxY = maxY;
            return;
        }

        UnionCircularX(activeStartX, activeSpanX, startX, spanX, out activeStartX, out activeSpanX);
        activeMinY = Mathf.Min(activeMinY, minY);
        activeMaxY = Mathf.Max(activeMaxY, maxY);
    }

    /// <summary>Union de deux intervalles circulaires en X: garde le plus petit englobant.</summary>
    private void UnionCircularX(int startA, int spanA, int startB, int spanB, out int start, out int span)
    {
        int spanFromA = Mathf.Max(spanA, WrapX(startB - startA) + spanB);
        int spanFromB = Mathf.Max(spanB, WrapX(startA - startB) + spanA);

        if (spanFromA <= spanFromB)
        {
            start = startA;
            span = Mathf.Min(width, spanFromA);
        }
        else
        {
            start = startB;
            span = Mathf.Min(width, spanFromB);
        }
    }

    private void UploadAll()
    {
        if (heightTex == null) return;

        heightTex.SetPixelData(currentHeight, 0);
        heightTex.Apply(false, false);
    }

    /// <summary>
    /// Upload LOCAL: copie uniquement la sous-région (gère le wrap en X via deux copies).
    /// </summary>
    private void UploadRegion(int startX, int spanX, int minY, int maxY)
    {
        if (heightTex == null) return;

        spanX = Mathf.Clamp(spanX, 1, width);
        int rows = Mathf.Clamp(maxY - minY + 1, 1, height);

        if ((SystemInfo.copyTextureSupport & CopyTextureSupport.Basic) == 0)
        {
            UploadAll();
            return;
        }

        EnsureStaging(spanX, rows);

        for (int r = 0; r < rows; r++)
        {
            int srcRow = (minY + r) * width;
            int dstRow = r * spanX;
            for (int c = 0; c < spanX; c++)
            {
                stagingBuffer[dstRow + c] = currentHeight[WrapX(startX + c) + srcRow];
            }
        }

        stagingTex.SetPixelData(stagingBuffer, 0);
        stagingTex.Apply(false, false);

        int firstPart = Mathf.Min(spanX, width - startX);
        Graphics.CopyTexture(stagingTex, 0, 0, 0, 0, firstPart, rows, heightTex, 0, 0, startX, minY);

        int secondPart = spanX - firstPart;
        if (secondPart > 0)
        {
            Graphics.CopyTexture(stagingTex, 0, 0, firstPart, 0, secondPart, rows, heightTex, 0, 0, 0, minY);
        }
    }

    private void EnsureStaging(int spanX, int rows)
    {
        if (stagingTex != null && stagingTex.width == spanX && stagingTex.height == rows) return;

        if (stagingTex != null)
        {
            DestroyTexture(stagingTex);
        }

        stagingTex = new Texture2D(spanX, rows, TextureFormat.RFloat, false, true)
        {
            name = "PlanetHeightFieldStaging",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp,
            anisoLevel = 0
        };

        stagingBuffer = new float[spanX * rows];
    }

    private static void DestroyTexture(Texture2D texture)
    {
        if (texture == null) return;

        if (Application.isPlaying)
        {
            UnityEngine.Object.Destroy(texture);
        }
        else
        {
            UnityEngine.Object.DestroyImmediate(texture);
        }
    }

    public void Dispose()
    {
        DestroyTexture(heightTex);
        DestroyTexture(stagingTex);
        heightTex = null;
        stagingTex = null;
        stagingBuffer = null;
    }
}
