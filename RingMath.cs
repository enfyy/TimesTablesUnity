using System.Linq;
using UnityEngine;

/// <summary>
/// Times Tables Math fun.
/// https://www.youtube.com/watch?v=qhbuKbxJsk8
/// </summary>
[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
public class RingMath : MonoBehaviour
{
    public bool animate;                            // toggle animation
    [Range(1, 100)] public int n = 20;              // number of points around the circle
    [Range(2, 100)] public float m = 3;             // multiplicator
    [Range(1f, 100f)] public float speed = 2;       // speed of the animation
    [Range(3, 360)] public int circleSegments;      // "roundness" of the circles in 3D mode
    public bool threeDimensional = true;            // 3D mode on / off
    public float radius = 5f;                       // radius of the entire thing
    public bool colorMode;                          // toggles between coloring by angle (false) and distance (true) 
    
    private float current_m;                        // current multiplicator for the animation
    private int animation_direction = 1;            // direction of the animation (switches between forward and backward)
    private Material[] materials;                   // array of materials for the submeshes (1st submesh uses 1st material)
    private MeshFilter mf;                          // MeshFilter component.
    private Mesh mesh;                              // Mesh that gets generated.
    private float step;                             // increases multiplicator when animated
    private Vector2[] a_points;                     // the points around the circle.
    private CombineInstance[] combine;              // used to combine all submeshes.
    
    /// <summary>
    /// Gets called at the start.
    /// </summary>
    void Start()
    {
        mf = GetComponent<MeshFilter>();
        mf.mesh = mesh = new Mesh();
        mesh.name = "Times Circle Procedural Mesh";
        
        if (!animate)
        {
            mesh.Clear();
            GenerateMesh();
        }
    }

    /// <summary>
    /// Gets called before start and when values in the inspector change.
    /// </summary>
    private void OnValidate()
    {
        step = speed * 0.00025f;
        current_m = m;
        a_points = new Vector2[n];
        
        // calculate the points around the circle here, so they dont have to be calculated every frame.
        for (int i = 0; i < n; i++)
            a_points[i] = Polar(radius, 360f * i / n);
        
        if (!animate && mesh != null)
        {
            mesh.Clear();
            GenerateMesh();
        }
    }

    /// <summary>
    /// Generates and draws the mesh.
    /// </summary>
    private void GenerateMesh()
    {
        // calculate new mesh
        mesh.Clear();
        for (int i = 0; i < n; i++)
        {
            float j = (i * current_m) % n;
            Vector2 a = a_points[i];
            Vector2 b = Polar(radius, 360 * j / n);

            if (threeDimensional)
                CircleMeshFromLine(a, b, circleSegments);
            else
                AddLine(a,b);
        }
    }

    /// <summary>
    /// Gets called on every frame of the gameloop.
    /// </summary>
    private void Update()
    {
        if (animate)// no need to do this every frame if theres no animation.
        {
            // animation
            m = Mathf.Clamp(m, 0, n);
            current_m += animation_direction * step;
            if (current_m >= m || current_m <= 0)
            {
                current_m = Mathf.Clamp(current_m, 0, m);
                animation_direction *= -1;
            }
            GenerateMesh();
        }
    }

    /// <summary>
    /// Gives x and y position on a circle with the given radius and angle in degrees.
    /// </summary>
    private static Vector2 Polar(float r, float angleDeg)
    {
        float x = -r * Mathf.Cos(Mathf.Deg2Rad * angleDeg);
        float y = -r * Mathf.Sin(Mathf.Deg2Rad * angleDeg);
        return new Vector2(x, y);
    }

    /// <summary>
    /// Adds a sub-mesh of a Line between the two vertices to the mesh.
    /// </summary>
    private void AddLine(Vector3 a, Vector3 b)
    {
        // add vertices to mesh
        Vector3[] new_verts = new Vector3[mesh.vertexCount + 2];
        mesh.vertices.CopyTo(new_verts, 0);
        new_verts[new_verts.Length - 2] = a;
        new_verts[new_verts.Length - 1] = b;
        mesh.vertices = new_verts;
        mesh.SetIndices(Enumerable.Range(0, mesh.vertexCount).ToArray(), MeshTopology.Lines, 0);
        
        // Set Vertex colors
        float hue = calculateHue(a, b);
        Color line_color = Color.HSVToRGB(hue, 1f, 1f);
        Color[] new_colors = new Color[mesh.vertexCount];
        mesh.colors.CopyTo(new_colors,0);
        new_colors[new_colors.Length - 2] = line_color;
        new_colors[new_colors.Length - 1] = line_color;
        mesh.colors = new_colors;
    }

    /// <summary>
    ///  Calculate a hue value based on the color mode (angle or distance)
    /// </summary>
    private float calculateHue(Vector2 a, Vector2 b)
    {
        if (colorMode)
        {
            float distance = Vector3.Distance(a, b);
            return Mathf.Clamp(distance / (radius * 2 * 0.01f) * 0.01f, 0, 1);
        }

        float degreeAngle = Vector2.Angle(a, b);
        return Mathf.Clamp((degreeAngle / 1.8f) * 0.01f, 0, 1);
    }

    /// <summary>
    /// Creates a circle mesh in the 3D plane of the line between the two vectors and adds it as a sub-mesh
    /// </summary>
    private void CircleMeshFromLine(Vector3 a, Vector3 b, int segments)
    {
        segments = Mathf.Clamp(segments, 3, 360); // make sure there are enough segments for at least a triangle...
        
        // calculate circle mesh
        float r = Vector3.Distance(a, b) * .5f;
        Vector3 center = Vector3.Lerp(a, b, .5f);
        Vector3 forward = (b - a).normalized;
        Vector3[] verts = new Vector3[segments * 2 + 2];
        Vector2 zero = Polar(r, 0);
        Vector2 one = Polar(r, 360f / segments);
        verts[0] = center + forward * zero.y + Vector3.forward * zero.x;
        verts[1] = center + forward * one.y + Vector3.forward * one.x;
        int j = 1;
        for (int i = 2; i < segments*2; i+=2)
        {
            float deg = (360f / segments) * j;
            Vector2 vec = Polar(r, deg);
            verts[i] = verts[i - 1]; // need redundancy of vertices for MeshTopology.Lines
            verts[i+1] = center + (forward * vec.y) + (Vector3.forward * vec.x);
            j++;
        }
        verts[segments*2] = verts[segments * 2 - 1];
        verts[segments*2 + 1] = verts[0];

        // integrate new circle into mesh
        int prev_length = mesh.vertexCount;
        Vector3[] new_vertices = new Vector3[prev_length +  verts.Length];
        mesh.vertices.CopyTo(new_vertices, 0);
        verts.CopyTo(new_vertices, prev_length);
        mesh.vertices = new_vertices;
        mesh.SetIndices(Enumerable.Range(0, mesh.vertexCount).ToArray(), MeshTopology.Lines, 0);

        // Set Vertex colors
        float hue = calculateHue(a, b);
        Color circle_color = Color.HSVToRGB(hue, 1f, 1f);
        Color[] new_colors = new Color[new_vertices.Length];
        mesh.colors.CopyTo(new_colors, 0);
        for (int i = prev_length; i < mesh.vertexCount; i++)
            new_colors[i] = circle_color;
        mesh.colors = new_colors;
    }
}