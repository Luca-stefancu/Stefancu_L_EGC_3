using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;

namespace Egc;

/// <summary>
/// Fabrica de forme 3D: fiecare metodă calculează vârfurile și indicii unei forme (mesh procedural)
/// și construiește un obiect Mesh. Un vârf = 6 float: x, y, z, r, g, b (ca în laboratoarele 2–4).
/// Formele sunt descrise în object space, cu centrul în origine; locul, mărimea și rotația
/// din scenă sunt date de matricea model (Transform3D).
///
/// Regula sensului de parcurgere: privită DIN AFARA obiectului, fiecare față are vârfurile
/// în sens trigonometric (invers acelor de ceasornic). Cu tasta C (eliminarea fețelor din spate)
/// fețele parcurse greșit dispar, deci greșelile se văd imediat.
/// </summary>
public static class Shapes
{
    // =====================================================================================
    // Forme pentru orientare (linii)
    // =====================================================================================

    /// <summary>
    /// Grila de pe podea (planul y = 0): linii paralele cu axele X și Z, din <paramref name="step"/>
    /// în <paramref name="step"/> unități, între -<paramref name="halfSize"/> și +<paramref name="halfSize"/>.
    /// </summary>
    public static Mesh Grid(int halfSize, float step, Color4 color)
    {
        int linesPerDirection = 2 * halfSize + 1;
        var vertices = new float[linesPerDirection * 2 * 2 * Mesh.FloatsPerVertex];   // 2 direcții × 2 capete
        var indices = new uint[linesPerDirection * 2 * 2];

        float extent = halfSize * step;
        int v = 0;
        for (int k = -halfSize; k <= halfSize; k++)
        {
            float c = k * step;
            // linie paralelă cu axa Z (x = c)
            SetVertex(vertices, v++, c, 0.0f, -extent, color);
            SetVertex(vertices, v++, c, 0.0f,  extent, color);
            // linie paralelă cu axa X (z = c)
            SetVertex(vertices, v++, -extent, 0.0f, c, color);
            SetVertex(vertices, v++,  extent, 0.0f, c, color);
        }
        for (int i = 0; i < indices.Length; i++)
        {
            indices[i] = (uint)i;   // fiecare pereche de vârfuri consecutive = o linie
        }

        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    /// <summary>Cele trei axe ale lumii, din origine: X roșu, Y verde, Z albastru.</summary>
    public static Mesh Axes(float length)
    {
        var vertices = new float[6 * Mesh.FloatsPerVertex];
        SetVertex(vertices, 0, 0, 0, 0, 1, 0, 0);           // X axis start (red)
        SetVertex(vertices, 1, length, 0, 0, 1, 0, 0);      // X axis end (red)
        SetVertex(vertices, 2, 0, 0, 0, 0, 1, 0);           // Y axis start (green)
        SetVertex(vertices, 3, 0, length, 0, 0, 1, 0);      // Y axis end (green)
        SetVertex(vertices, 4, 0, 0, 0, 0, 0, 1);           // Z axis start (blue)
        SetVertex(vertices, 5, 0, 0, length, 0, 0, 1);      // Z axis end (blue)
        uint[] indices = { 0, 1, 2, 3, 4, 5 };
        return new Mesh(vertices, indices, PrimitiveType.Lines);
    }

    // =====================================================================================
    // Forme 3D (triunghiuri)
    // =====================================================================================

    /// <summary>
    /// Piramida cu baza pătrată (latura 1, înălțimea 1), centrată în origine. EXEMPLU COMPLET:
    /// fiecare față are culoarea ei, deci vârfurile NU sunt partajate între fețe:
    /// 4 fețe laterale × 3 vârfuri + baza × 4 vârfuri = 16 vârfuri.
    /// </summary>
    public static Mesh Pyramid()
    {
        const float h = 0.5f;
        var vertices = new float[16 * Mesh.FloatsPerVertex];
        var indices = new uint[4 * 3 + 6];

        var apex = new Vector3(0, h, 0);
        Vector3 frontLeft = new(-h, -h, h), frontRight = new(h, -h, h);
        Vector3 backRight = new(h, -h, -h), backLeft = new(-h, -h, -h);

        // Fețele laterale: două colțuri ale bazei (de la stânga la dreapta, privind din afară) + vârful.
        AddTriangle(vertices, indices, 0, frontLeft, frontRight, apex, ColorUtil.FromHsv(0.00f, 0.8f, 1.0f));  // față (+Z)
        AddTriangle(vertices, indices, 1, frontRight, backRight, apex, ColorUtil.FromHsv(0.15f, 0.8f, 1.0f));  // dreapta (+X)
        AddTriangle(vertices, indices, 2, backRight, backLeft, apex,   ColorUtil.FromHsv(0.30f, 0.8f, 1.0f));  // spate (-Z)
        AddTriangle(vertices, indices, 3, backLeft, frontLeft, apex,   ColorUtil.FromHsv(0.55f, 0.8f, 1.0f));  // stânga (-X)

        // Baza (-Y), privită de dedesubt: un pătrat = 4 vârfuri (12..15) și 6 indici (12..17).
        Color4 baseColor = new(0.35f, 0.35f, 0.35f, 1.0f);
        SetVertex(vertices, 12, backLeft, baseColor);
        SetVertex(vertices, 13, backRight, baseColor);
        SetVertex(vertices, 14, frontRight, baseColor);
        SetVertex(vertices, 15, frontLeft, baseColor);
        uint[] baseIndices = { 12, 13, 14, 14, 15, 12 };
        baseIndices.CopyTo(indices, 12);

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Cubul cu latura 1, centrat în origine. Fiecare față are culoarea ei, deci fiecare față
    /// are propriile 4 vârfuri: 6 fețe × 4 = 24 de vârfuri și 6 fețe × 6 = 36 de indici.
    /// Culori: +X roșu, -X cyan, +Y verde, -Y magenta, +Z albastru, -Z galben
    /// (axa pozitivă are culoarea axei, cea negativă culoarea complementară).
    /// </summary>
    public static Mesh Cube()
    {
        const float h = 0.5f;   // jumătate din latură
        var vertices = new float[24 * Mesh.FloatsPerVertex];
        var indices = new uint[36];

        // Fiecare față: cele 4 colțuri în sens trigonometric, privind fața din afara cubului
        // (stânga-jos, dreapta-jos, dreapta-sus, stânga-sus).

        // Fața 0: +Z (față). Privită din +Z: dreapta = +X, sus = +Y.
        AddFace(vertices, indices, 0,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Color4(0.2f, 0.4f, 1.0f, 1.0f));

        // Fața 1: +X (dreapta). Privită din +X: dreapta = -Z, sus = +Y.
        AddFace(vertices, indices, 1,
                new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(h, h, -h), new Vector3(h, h, h),
                new Color4(1.0f, 0.2f, 0.2f, 1.0f));

        // Fața 2: -Z (spate). Privită din -Z: dreapta = -X, sus = +Y.
        AddFace(vertices, indices, 2,
                new Vector3(h, -h, -h), new Vector3(-h, -h, -h), new Vector3(-h, h, -h), new Vector3(h, h, -h),
                new Color4(1.0f, 1.0f, 0.2f, 1.0f));

        // Fața 3: -X (stânga). Privită din -X: dreapta = +Z, sus = +Y.
        AddFace(vertices, indices, 3,
                new Vector3(-h, -h, -h), new Vector3(-h, -h, h), new Vector3(-h, h, h), new Vector3(-h, h, -h),
                new Color4(0.2f, 1.0f, 1.0f, 1.0f));

        // Fața 4: +Y (sus). Privită din +Y: dreapta = +X, sus = -Z.
        AddFace(vertices, indices, 4,
                new Vector3(-h, h, -h), new Vector3(h, h, -h), new Vector3(h, h, h), new Vector3(-h, h, h),
                new Color4(0.2f, 1.0f, 0.2f, 1.0f));

        // Fața 5: -Y (jos). Privită din -Y: dreapta = +X, sus = +Z.
        AddFace(vertices, indices, 5,
                new Vector3(-h, -h, h), new Vector3(h, -h, h), new Vector3(h, -h, -h), new Vector3(-h, -h, -h),
                new Color4(1.0f, 0.2f, 1.0f, 1.0f));

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Sfera UV de rază <paramref name="radius"/>: o grilă de (stacks + 1) × (slices + 1) vârfuri,
    /// așezată pe sferă prin coordonatele u (de-a lungul paralelelor) și v (de-a lungul meridianelor).
    /// Culoarea vârfului arată coordonatele lui UV: roșu = u, verde = v.
    /// </summary>
    /// <param name="slices">Numărul de felii verticale (meridiane), de exemplu 32.</param>
    /// <param name="stacks">Numărul de benzi orizontale (paralele), de exemplu 16.</param>
    public static Mesh Sphere(float radius, int slices, int stacks)
    {
        int columns = slices + 1;   // ultima coloană (u = 1) dublează prima (u = 0): "cusătura"
        int rows = stacks + 1;
        var vertices = new float[rows * columns * Mesh.FloatsPerVertex];
        var indices = new uint[stacks * slices * 6];

        // 1. Vârfurile: rândul i (de jos în sus), coloana j (spre est).
        for (int i = 0; i < rows; i++)
        {
            float v = (float)i / stacks;                          // 0 la polul sud, 1 la polul nord
            float latitude = -MathF.PI / 2.0f + v * MathF.PI;     // unghiul față de ecuator: -90° .. +90°

            for (int j = 0; j < columns; j++)
            {
                float u = (float)j / slices;                      // 0 .. 1 în jurul sferei
                float longitude = u * 2.0f * MathF.PI;            // 0° .. 360°

                // TODO 5.4 (a): poziția vârfului (coordonate sferice -> carteziene, secțiunea 2.2)
                float cosLatitude = MathF.Cos(latitude);
                float sinLatitude = MathF.Sin(latitude);
                float rp = radius * cosLatitude;                  // raza paralelei
                float x = rp * MathF.Cos(longitude);
                float y = radius * sinLatitude;
                float z = -rp * MathF.Sin(longitude);

                SetVertex(vertices, i * columns + j, x, y, z, u, v, 0.35f);
            }
        }

        // 2. Indicii: fiecare celulă a grilei (un "dreptunghi" curbat) = 2 triunghiuri.
        int k = 0;
        for (int i = 0; i < stacks; i++)
        {
            for (int j = 0; j < slices; j++)
            {
                uint bottomLeft = (uint)(i * columns + j);
                uint bottomRight = bottomLeft + 1;
                uint topLeft = bottomLeft + (uint)columns;
                uint topRight = topLeft + 1;

                // TODO 5.4 (b): cele două triunghiuri ale celulei, în sens trigonometric privite din afară
                //               (6 indici: indices[k++] = ...)
                indices[k++] = bottomLeft;
                indices[k++] = bottomRight;
                indices[k++] = topRight;
                
                indices[k++] = topRight;
                indices[k++] = topLeft;
                indices[k++] = bottomLeft;
            }
        }

        return new Mesh(vertices, indices);
    }

    /// <summary>
    /// Cilindrul cu raza 1 și înălțimea 1, centrat în origine.
    /// Suprafața laterală: o grilă cu 1 rând de celule (2 rânduri de vârfuri, sus și jos).
    /// Capacele sunt evantaie de triunghiuri (ca poligonul regulat din laboratorul 3).
    /// </summary>
    /// <param name="slices">Numărul de felii verticale (meridiane), de exemplu 32.</param>
    public static Mesh Cylinder(int slices = 32)
    {
        const float radius = 1.0f;
        const float height = 1.0f;
        const float halfHeight = height / 2.0f;

        int columns = slices + 1;   // ultima coloană închide cilindrul (cusătura)
        int verticesCount = 2 * columns +    // vârfurile suprafeței laterale (2 rânduri)
                            2 * (1 + slices); // vârfurile capacelor (centru + perimetrul)
        var vertices = new float[verticesCount * Mesh.FloatsPerVertex];
        var indices = new uint[slices * 6 +  // suprafața laterală: slices × 2 triunghiuri
                               slices * 3 +   // capa inferioară: slices triunghiuri
                               slices * 3];   // capa superioară: slices triunghiuri

        int v = 0;
        int k = 0;

        // 1. Suprafața laterală: grilă cu stacks = 1
        var lateralColor = new Color4(0.8f, 0.3f, 0.3f, 1.0f);
        int bottomStart = 0;
        int topStart = columns;

        for (int j = 0; j < columns; j++)
        {
            float u = (float)j / slices;
            float angle = u * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);

            // vârf jos (-Y)
            SetVertex(vertices, v++, x, -halfHeight, z, lateralColor);
            // vârf sus (+Y)
            SetVertex(vertices, v++, x, halfHeight, z, lateralColor);
        }

        // indicii suprafeței laterale: fiecare celulă = 2 triunghiuri
        for (int j = 0; j < slices; j++)
        {
            uint bottomLeft = (uint)(bottomStart + j * 2);
            uint bottomRight = (uint)(bottomStart + (j + 1) * 2);
            uint topLeft = (uint)(topStart + j * 2);
            uint topRight = (uint)(topStart + (j + 1) * 2);

            // triunghiul 1: bottomLeft, bottomRight, topRight
            indices[k++] = bottomLeft;
            indices[k++] = bottomRight;
            indices[k++] = topRight;

            // triunghiul 2: topRight, topLeft, bottomLeft
            indices[k++] = topRight;
            indices[k++] = topLeft;
            indices[k++] = bottomLeft;
        }

        // 2. Capa inferioară (-Y): evantai de triunghiuri
        int bottomCapCenter = v;
        SetVertex(vertices, v++, 0.0f, -halfHeight, 0.0f, new Color4(0.3f, 0.8f, 0.3f, 1.0f));
        int bottomCapStart = v;

        for (int j = 0; j < columns; j++)
        {
            float u = (float)j / slices;
            float angle = u * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);
            SetVertex(vertices, v++, x, -halfHeight, z, new Color4(0.3f, 0.8f, 0.3f, 1.0f));
        }

        // triunghiuri pentru capa inferioară
        for (int j = 0; j < slices; j++)
        {
            uint center = (uint)bottomCapCenter;
            uint curr = (uint)(bottomCapStart + j);
            uint next = (uint)(bottomCapStart + j + 1);

            // privite din -Y: trebuie să fie în sens trigonometric
            indices[k++] = next;
            indices[k++] = curr;
            indices[k++] = center;
        }

        // 3. Capa superioară (+Y): evantai de triunghiuri
        int topCapCenter = v;
        SetVertex(vertices, v++, 0.0f, halfHeight, 0.0f, new Color4(0.3f, 0.3f, 0.8f, 1.0f));
        int topCapStart = v;

        for (int j = 0; j < columns; j++)
        {
            float u = (float)j / slices;
            float angle = u * 2.0f * MathF.PI;
            float x = radius * MathF.Cos(angle);
            float z = -radius * MathF.Sin(angle);
            SetVertex(vertices, v++, x, halfHeight, z, new Color4(0.3f, 0.3f, 0.8f, 1.0f));
        }

        // triunghiuri pentru capa superioară
        for (int j = 0; j < slices; j++)
        {
            uint center = (uint)topCapCenter;
            uint curr = (uint)(topCapStart + j);
            uint next = (uint)(topCapStart + j + 1);

            // privite din +Y: trebuie să fie în sens trigonometric
            indices[k++] = curr;
            indices[k++] = next;
            indices[k++] = center;
        }

        return new Mesh(vertices, indices);
    }

    // =====================================================================================
    // Funcții ajutătoare
    // =====================================================================================

    /// <summary>
    /// Scrie fața <paramref name="face"/> a unei forme compuse din patrulatere: vârfurile
    /// 4·face .. 4·face + 3 și indicii 6·face .. 6·face + 5 (triunghiurile a-b-c și c-d-a).
    /// Colțurile a, b, c, d trebuie date în sens trigonometric, privind fața din afară.
    /// </summary>
    private static void AddFace(float[] vertices, uint[] indices, int face,
                                Vector3 a, Vector3 b, Vector3 c, Vector3 d, Color4 color)
    {
        int first = 4 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        SetVertex(vertices, first + 3, d, color);

        uint f = (uint)first;
        int k = 6 * face;
        indices[k] = f;         indices[k + 1] = f + 1; indices[k + 2] = f + 2;   // a, b, c
        indices[k + 3] = f + 2; indices[k + 4] = f + 3; indices[k + 5] = f;       // c, d, a
    }

    /// <summary>Scrie triunghiul <paramref name="face"/>: vârfurile 3·face .. 3·face + 2 și indicii lor.</summary>
    private static void AddTriangle(float[] vertices, uint[] indices, int face,
                                    Vector3 a, Vector3 b, Vector3 c, Color4 color)
    {
        int first = 3 * face;
        SetVertex(vertices, first, a, color);
        SetVertex(vertices, first + 1, b, color);
        SetVertex(vertices, first + 2, c, color);
        indices[first] = (uint)first;
        indices[first + 1] = (uint)first + 1;
        indices[first + 2] = (uint)first + 2;
    }

    /// <summary>Scrie un vârf (poziție + culoare) în tabloul <paramref name="vertices"/>, pe poziția <paramref name="index"/>.</summary>
    private static void SetVertex(float[] vertices, int index, float x, float y, float z, float r, float g, float b)
    {
        int i = index * Mesh.FloatsPerVertex;
        vertices[i] = x;
        vertices[i + 1] = y;
        vertices[i + 2] = z;
        vertices[i + 3] = r;
        vertices[i + 4] = g;
        vertices[i + 5] = b;
    }

    private static void SetVertex(float[] vertices, int index, float x, float y, float z, Color4 color) =>
        SetVertex(vertices, index, x, y, z, color.R, color.G, color.B);

    private static void SetVertex(float[] vertices, int index, Vector3 p, Color4 color) =>
        SetVertex(vertices, index, p.X, p.Y, p.Z, color.R, color.G, color.B);
}
