using System;
using UnityEngine;

public class ShadowControl : MonoBehaviour {
    public GameObject Sphere;
    public GameObject CubeTemplate;
    public GameObject VertexTemplate;
    public Transform GeneratedSphere;

    [Range(1.0f, 48.0f)]
    public float Resolution = 1;
    [Range(0.99f,0.0f)]
    public float FacetThickness = 0.99f;
    public int SphereX;
    public int SphereY;
    public Material Odd;
    public Material Even;
    public Texture2D SphereTexture;
    public Material DefaultShader;
    public Material TextureShader;

    public int[] CubeTemplateTriangles;
    public Vector3[] CubeTemplateVertices;
    public int[] TruncatedTetrahedronTriangles;
    public Vector3[] TruncatedTetrahedronVertices;
    public Vector3Int[] TruncatedTetrahedronVerticeMap;
    public Vector3Int[] DefaultCubeVerticeMap;

    private float Radius;
    private MeshFilter cubeMeshFilter;
    private MeshRenderer sphereMeshRender;
    private Material previous_material;
    private bool previous_sphere_activity;
    void Awake() {
        Resolution = 3;
        Radius = Sphere.GetComponent<SphereCollider>().radius;
        sphereMeshRender = Sphere.GetComponent<MeshRenderer>();
        sphereMeshRender.material = TextureShader;
        previous_material = DefaultShader;
        previous_sphere_activity = Sphere.activeSelf;
        cubeMeshFilter = CubeTemplate.GetComponent<MeshFilter>();
        CubeTemplateTriangles = cubeMeshFilter.sharedMesh.triangles;
        CubeTemplateVertices = cubeMeshFilter.sharedMesh.vertices;
        TruncatedTetrahedronTriangles = new int[] {
            0, 2, 3,
            0, 3, 1,
            5, 4, 6,
            5, 6, 7,
            8, 9, 10,
            8, 10, 11,
            14, 13, 12,
            17, 16, 15
        };
        TruncatedTetrahedronVertices = new Vector3[] {
            new Vector3(0.5f, -0.5f, 0.5f),
            new Vector3(-0.5f, -0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, 0.5f),
            new Vector3(-0.5f, 0.5f, 0.0f),
            new Vector3(0.5f, 0.5f, -0.5f),
            new Vector3(0.5f, -0.5f, -0.5f)
         };
        TruncatedTetrahedronVerticeMap = new Vector3Int[] {
            new Vector3Int(0,7,15),
            new Vector3Int(1,8,17),
            new Vector3Int(2,6,12),
            new Vector3Int(3,9,13),
            new Vector3Int(4,10,14),
            new Vector3Int(5,11,16)
        };
        DefaultCubeVerticeMap = new Vector3Int[] {
            new Vector3Int(0,13,23),
            new Vector3Int(1,14,16),
            new Vector3Int(2,8,22),
            new Vector3Int(3,9,17),
            new Vector3Int(4,10,21),
            new Vector3Int(5,11,18),
            new Vector3Int(6,12,20),
            new Vector3Int(7,15,19)
        };
    }
    // Use ReverseTriangleWinding to make the final screen work to build a gantry like sphere.
    void ReverseTriangleWinding(MeshFilter mesh_filter) {
        var triangles = mesh_filter.mesh.triangles;
        var number_of_polys = triangles.Length / 3;

        for (var t = 0; t < number_of_polys; t++) {
            var triangle_buffer = triangles[t * 3];
            triangles[t * 3] = triangles[(t * 3) + 2];
            triangles[(t * 3) + 2] = triangle_buffer;
        }
    }
    private GameObject CurrentCube;
    void Update() {
        //Sphere.transform.Rotate(Vector3.up * Time.deltaTime);
        if (CurrentCube != null) {
            CubeTemplateVertices = CurrentCube.GetComponent<MeshFilter>().sharedMesh.vertices;
        }
    }
    public void OnSliderChange(float f) {
        Resolution = f;
        RebuildRectangles();
    }
    public void RebuildRectangles() {
        foreach (Transform t in GeneratedSphere) {
            Destroy(t.gameObject);
        }
        //CurrentCube = Instantiate(CubeTemplate, GeneratedSphere);
        //CubeTemplateVertices = CurrentCube.GetComponent<MeshFilter>().sharedMesh.vertices;
        //MakeTruncatedTetrahedron(CurrentCube);
        //for (int i = 0; i < CubeTemplateVertices.Length / 3; i++) {
        //    var go = Instantiate(VertexTemplate, GeneratedSphere);
        //    go.transform.position = CubeTemplateVertices[i];
        //    go.name = i.ToString();
        //}
        //var v = new Vector3[mesh.vertices.Length];
        //Array.Copy(mesh.vertices, v, mesh.vertices.Length);
        for (int i = 0; i < Resolution; i++) {
            for (int j = 0; j < Resolution; j++) {
                var go = Instantiate(CubeTemplate, GeneratedSphere);
                CreateFacet(go, i, j);
            }
        }
        //mesh.vertices = v;
    }
    public void MakeTruncatedTetrahedron(GameObject go) {
        var mesh = go.GetComponent<MeshFilter>().mesh;
        var t = new int[TruncatedTetrahedronTriangles.Length];
        Array.Copy(TruncatedTetrahedronTriangles, t, t.Length);
        mesh.triangles = t;
        var vertices = new Vector3[TruncatedTetrahedronVertices.Length * 3];
        foreach (var v in TruncatedTetrahedronVerticeMap) {
            vertices[v.x] = TruncatedTetrahedronVertices[v.x];
            vertices[v.y] = TruncatedTetrahedronVertices[v.x];
            vertices[v.z] = TruncatedTetrahedronVertices[v.x];
        }
        mesh.vertices = vertices;
    }
    private float angle;
    public void CreateFacet(GameObject go, int i, int j) {
        angle = Mathf.PI / (2.0f * (1.0f / Resolution));
        if (j < Resolution - 1) {
            if (i == Resolution - 1) {
                //MakeTruncatedTetrahedron(go);
                //TransformTetrahedronToSphereFacet(go, i, j);
            }
            else {
                TransformCubeToSphereFacet(go, i, j);
            }
            Mirror(go);
        }
        else if (Resolution == 1) {
            //MakeTruncatedTetrahedron(go);
            //TransformTetrahedronToSphereFacet(go, i, j);
        }
    }
    public void TransformTetrahedronToSphereFacet(GameObject go, int i, int j) {
        var mesh_filter = go.GetComponent<MeshFilter>();
        var vertices = TruncatedTetrahedronVertices;
        var scale = vertices[0].x;
        var new_vertices = new Vector3[vertices.Length * 3];
        var current_xy_angle = angle * i;
        var current_zy_angle = angle * j;
        var current_zy_angle_plus1 = angle * (j + 1);
        new_vertices[0] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle_plus1)) * FacetThickness;
        new_vertices[1] = new Vector3(0.0f, 1.0f, 0.0f);
        new_vertices[2] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle_plus1));
        new_vertices[3] = new Vector3(0.0f, 1.0f * FacetThickness, 0.0f);
        new_vertices[4] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle));
        new_vertices[5] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle)) * FacetThickness;
        AttachToMesh(mesh_filter, new_vertices, TruncatedTetrahedronVerticeMap);
    }
    public void TransformCubeToSphereFacet(GameObject go, int i, int j) {
        var mesh_filter = go.GetComponent<MeshFilter>();
        var vertices = mesh_filter.sharedMesh.vertices;
        var scale = vertices[0].x;
        var new_vertices = new Vector3[vertices.Length];
        var current_xy_angle = angle * i;
        var current_xy_angle_plus1 = angle * (i + 1);
        var current_zy_angle = angle * j;
        var current_zy_angle_plus1 = angle * (j + 1);
        new_vertices[0] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle_plus1));
        new_vertices[1] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle_plus1)) * FacetThickness;
        new_vertices[2] = new Vector3(Mathf.Cos(current_xy_angle_plus1), Mathf.Sin(current_xy_angle_plus1), Mathf.Sin(current_zy_angle_plus1));
        new_vertices[3] = new Vector3(Mathf.Cos(current_xy_angle_plus1), Mathf.Sin(current_xy_angle_plus1), Mathf.Sin(current_zy_angle_plus1)) * FacetThickness;
        new_vertices[4] = new Vector3(Mathf.Cos(current_xy_angle_plus1), Mathf.Sin(current_xy_angle_plus1), Mathf.Sin(current_zy_angle));
        new_vertices[5] = new Vector3(Mathf.Cos(current_xy_angle_plus1), Mathf.Sin(current_xy_angle_plus1), Mathf.Sin(current_zy_angle)) * FacetThickness;
        new_vertices[6] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle));
        new_vertices[7] = new Vector3(Mathf.Cos(current_xy_angle), Mathf.Sin(current_xy_angle), Mathf.Sin(current_zy_angle)) * FacetThickness;
        AttachToMesh(mesh_filter, new_vertices, DefaultCubeVerticeMap);
    }
    public void AttachToMesh(MeshFilter mesh, Vector3[] vertices, Vector3Int[] map) {
        foreach (var m in map) {
            vertices[m.y] = vertices[m.x];
            vertices[m.z] = vertices[m.x];
        }
        mesh.sharedMesh.vertices = vertices;
    }
    public void Mirror(GameObject g) {
    }

    public void SwitchSphereMaterial() {
        var material = sphereMeshRender.material;
        sphereMeshRender.material = previous_material;
        previous_material = material;
    }
    public void ToggleSphere() {
        previous_sphere_activity = !previous_sphere_activity;
        Sphere.SetActive(previous_sphere_activity);
    }
}
