using UnityEngine;
using System.Collections;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GRBake : MonoBehaviour
{
    // Use this for initialization
    List<string> materials;
    List<Material> materialsElm;
    Dictionary<int, List<CombineInstance>> meshes;
    GameObject rootBake;
    GameObject rootTerrainTmp;
    // NU GameObject extRootCar;
    GameObject intRootCar;
    public string BakeOnlyThis;

    void Start ()
    {
        PrepareList();
        rootBake = new GameObject();
        rootBake.name = "RootBake";

        rootTerrainTmp = new GameObject();
        rootTerrainTmp.name = "RootTerrainTmp";

        //DevConsole.Console.AddCommand(new DevConsole.Console.ActionCommand(bakeThis, "gRally", "BAKE", "Bake!!!"));
        //DevConsole.Console.AddCommand(new Console.AddCommand(bakeThisCar, "gRally", "CAR_BAKE", "Bake car!!!"));
        //DevConsole.Console.AddCommand(new Console.AddCommand(bakeTerrainThis, "gRally", "TERRAIN_BAKE", "Bake Terrain!!!"));
    }
	
    public void PrepareList()
    {
        materials = new List<string>();
        materialsElm = new List<Material>();
        meshes = new Dictionary<int, List<CombineInstance>>();
    }

    public bool BakeGameObject(GameObject toBake)
    {
        bool ret = false;
        if (toBake.name.ToLower().StartsWith("collision_"))
        {
            return false;
        }

        // solo se è statico!
        if (!toBake.isStatic && toBake.activeSelf && toBake.tag == "BAKE")
        {
            // prima ciclo nei figli
            foreach (Transform child in toBake.transform)
            {
                if (child.gameObject.tag == "BAKE")
                {
                    BakeGameObject(child.gameObject);
                }
            }

            var rend = toBake.GetComponent<Renderer>();
            if (rend != null)
            {
                MeshFilter mF = (MeshFilter)toBake.GetComponent<MeshFilter>();
                if (mF)
                {
                    var mats = rend.sharedMaterials;
                    //mF.sharedMesh.Optimize();
                    //toBake.GetComponent<MeshFilter>().sharedMesh.RecalculateBounds();

                    var matLen = mats.Length;
                    var origMesh = mF.sharedMesh;
                    var meshLen = origMesh.subMeshCount;
                    /*
                    if (matLen < meshLen)
                    {
                        // truschino per livellare
                        var lastMat = mats[mats.Length - 1];
                        System.Array.Resize(ref mats, meshLen);
                        
                        for (int i = matLen; i < meshLen; i++)
                        {
                            mats[i] = lastMat;
                        }
                        matLen = mats.Length;
                    }
                    */
                    if (matLen <= meshLen)
                    {
                        for (int i = 0; i < matLen; i++)
                        {
                            if (!materials.Contains(mats[i].name))
                            {
                                // nuovo materiale
                                materials.Add(mats[i].name);
                                materialsElm.Add(mats[i]);
                                meshes.Add(materials.Count - 1, new List<CombineInstance>());
                                var goAdd = new GameObject();
                                goAdd.name = "BK_" + mats[i].name.Replace("(Instance)", "") + "_0";
                                goAdd.transform.parent = rootBake.transform;
                            }

                            var instNew = new CombineInstance();
                            //instNew.mesh = origMesh;
                            instNew.mesh = cleanVertex(origMesh, i);
                            instNew.transform = toBake.transform.localToWorldMatrix;

                            // cerco il materiale
                            var iMF = materials.FindIndex(a => a == mats[i].name);
                            meshes[iMF].Add(instNew);
                            ret = true;
                        }
                    }
                    else
                    {
                        Debug.LogError(string.Format("Mesh {0} have {1} mats and {2} submeshes!!", toBake.name, matLen, meshLen));
                    }
                }
            }
            else
            {
                ret = false;
            }
        }

        // disattivo
        if (!toBake.isStatic && ret)
        {
            if (toBake.name != "layout0")
            {
                if (!toBake.GetComponent<Terrain>())
                {
                    toBake.SetActive(false);
                }
            }
        }
        return ret;
    }

    bool bakeTerrainGameObject(GameObject toBake)
    {
        bool ret = false;

        // solo se è statico!
        if (!toBake.isStatic && toBake.activeSelf)
        {
            // prima ciclo nei figli
            foreach (Transform child in toBake.transform)
            {
                bakeTerrainGameObject(child.gameObject);
            }

            var terrain = toBake.GetComponent<Terrain>();
            // NU int i = 0;
            if (terrain != null)
            {
                int treeeI = 0;
                foreach (var item in terrain.terrainData.treeInstances)
                {
                    int protID = item.prototypeIndex;
                    var pos = terrain.GetPosition() + new Vector3(item.position.x * terrain.terrainData.size.x,
                        +item.position.y * terrain.terrainData.size.y,
                        +item.position.z * terrain.terrainData.size.z);
                    var scale = new Vector3(item.widthScale, item.heightScale, item.widthScale);
                    var rot = item.rotation;
                    var org = terrain.terrainData.treePrototypes[protID].prefab;
                    var newT = Object.Instantiate(org);
                    newT.transform.parent = rootTerrainTmp.transform;
                    newT.transform.position = pos;
                    newT.transform.localScale = scale;
                    newT.transform.Rotate(Vector3.up, rot);
                    newT.name = org.name + "_" + treeeI.ToString();
                    newT.tag = "BAKE";
                    treeeI++;
                }
                ret = true;
            }
        }

        // disattivo
        if (ret)
        {
            toBake.SetActive(false);
        }
        return ret;
    }

    public bool BakeCarGameObject(GameObject toBake, GameObject newRootBake)
    {
        bool ret = false;

        // solo se è statico!
        if (toBake.activeSelf)
        {
            // prima ciclo nei figli
            foreach (Transform child in toBake.transform)
            {
                if (child.gameObject.tag == "BAKE")
                {
                    BakeCarGameObject(child.gameObject, newRootBake);
                }
            }

            if (toBake.tag != "BAKE")
            {
                return ret;
            }

            var rend = toBake.GetComponent<Renderer>();
            if (rend != null)
            {
                MeshFilter mF = (MeshFilter)toBake.GetComponent<MeshFilter>();
                if (mF)
                {
                    var mats = rend.sharedMaterials;
                    var matLen = mats.Length;
                    var origMesh = mF.sharedMesh;
                    var meshLen = origMesh.subMeshCount;

                    if (matLen <= meshLen)
                    {
                        for (int i = 0; i < matLen; i++)
                        {
                            if (!materials.Contains(mats[i].name))
                            {
                                // nuovo materiale
                                materials.Add(mats[i].name);
                                materialsElm.Add(mats[i]);
                                meshes.Add(materials.Count - 1, new List<CombineInstance>());
                                var goAdd = new GameObject();
                                goAdd.name = "BK_" + mats[i].name.Replace("(Instance)", "") + "_0";
                                goAdd.transform.parent = newRootBake.transform;
                            }

                            var instNew = new CombineInstance();
                            //instNew.mesh = origMesh;
                            instNew.mesh = cleanVertex(origMesh, i);
                            instNew.transform = toBake.transform.localToWorldMatrix;

                            // cerco il materiale
                            var iMF = materials.FindIndex(a => a == mats[i].name);
                            meshes[iMF].Add(instNew);
                            ret = true;
                        }
                    }
                    else
                    {
                        Debug.LogError(string.Format("Mesh {0} have {1} mats and {2} submeshes!!", toBake.name, matLen, meshLen));
                    }
                }
            }
            else
            {
                ret = false;
            }
        }

        // disattivo
        if (!toBake.isStatic && ret)
        {
            toBake.SetActive(false);
        }
        return ret;
    }

    public void CreateCar(GameObject newRootBake)
    {
        foreach (KeyValuePair<int, List<CombineInstance>> entry in meshes)
        {
            var instances = new List<List<CombineInstance>>();
            checkVertex(entry.Value, ref instances);
            for (int i = 0; i < instances.Count; i++)
            {
                var go = GameObject.Find("BK_" + materials[entry.Key] + "_" + i.ToString());

                if (go == null)
                {
                    go = new GameObject();
                    go.name = "BK_" + materials[entry.Key].Replace("(Instance)", "") + "_" + i.ToString();
                    go.transform.parent = newRootBake.transform;
                }

                //go.name += "__" + entry.Value.Count.ToString();
                var meshR = go.GetComponent<MeshRenderer>();
                if (!meshR)
                {
                    meshR = go.AddComponent<MeshRenderer>();
                    meshR.material = materialsElm[entry.Key];
                }

                var meshF = go.GetComponent<MeshFilter>();
                if (meshF)
                {
                    DestroyImmediate(meshF);
                }
                meshF = go.AddComponent<MeshFilter>();
                meshF.mesh = new Mesh();
                meshF.mesh.CombineMeshes(instances[i].ToArray());
                meshF.mesh.RecalculateBounds();
                // NU go.GetComponent<MeshInfo>().DebugElements = entry.Value.Count;
            }
        }
    }

    void bakeThis()
    {
        foreach (var item in SceneManager.GetSceneByName("stage").GetRootGameObjects())
        {
            if (item.activeSelf && item.tag == "BAKE")
            {
                BakeGameObject(item);
            }
        }

        if (rootTerrainTmp.activeSelf)
        {
            BakeGameObject(rootTerrainTmp);
        }

        foreach (var item in SceneManager.GetSceneByName("layout0").GetRootGameObjects())
        {
            if (item.activeSelf)
            {
                BakeGameObject(item);
            }
        }

        Create();

        // pulisco
        CleanList();
    }

    public void Create()
    {
        // ora creo!!
        foreach (KeyValuePair<int, List<CombineInstance>> entry in meshes)
        {
            //Debug.Log(string.Format("Material {0} baked with {1} meshes.", materials[entry.Key], entry.Value.Count));
            // do something with entry.Value or entry.Key
            var instances = new List<List<CombineInstance>>();
            checkVertex(entry.Value, ref instances);
            for (int i = 0; i < instances.Count; i++)
            {
                var go = GameObject.Find("BK_" + materials[entry.Key] + "_" + i.ToString());

                if (go == null)
                {
                    go = new GameObject();
                    go.name = "BK_" + materials[entry.Key].Replace("(Instance)", "") + "_" + i.ToString();
                    go.transform.parent = rootBake.transform;
                }

                //go.name += "__" + entry.Value.Count.ToString();
                var meshR = go.GetComponent<MeshRenderer>();
                if (!meshR)
                {
                    meshR = go.AddComponent<MeshRenderer>();
                    meshR.material = materialsElm[entry.Key];
                }

                var meshF = go.GetComponent<MeshFilter>();
                if (meshF)
                {
                    DestroyImmediate(meshF);
                }
                meshF = go.AddComponent<MeshFilter>();
                meshF.mesh = new Mesh();
                meshF.mesh.CombineMeshes(instances[i].ToArray());
                meshF.mesh.RecalculateBounds();
                // NU go.GetComponent<MeshInfo>().DebugElements = entry.Value.Count;
                go.isStatic = true;
            }
        }
    }

    public void CleanList()
    {
        materials.Clear();
        materials = null;

        materialsElm.Clear();
        materialsElm = null;

        meshes.Clear();
        meshes = null;
    }

    private void bakeTerrainThis()
    {
        foreach (var item in SceneManager.GetSceneByName("stage").GetRootGameObjects())
        {
            if (item.activeSelf)
            {
                bakeTerrainGameObject(item);
            }
        }

        foreach (var item in SceneManager.GetSceneByName("layout0").GetRootGameObjects())
        {
            if (item.activeSelf)
            {
                bakeTerrainGameObject(item);
            }
        }
    }

    private void checkVertex(List<CombineInstance> instances, ref List<List<CombineInstance>> output)
    {
        List<CombineInstance> currentInstance = new List<CombineInstance>();
        int vertex = 0;

        for (int i = 0; i < instances.Count; i++)
        {
            vertex += instances[i].mesh.vertexCount;
            if (vertex < 65500)
            {
                // aggiungo senza problemi...
                currentInstance.Add(instances[i]);
            }
            else
            {
                // devo splittare!!!
                output.Add(currentInstance);
                currentInstance = new List<CombineInstance>();
                currentInstance.Add(instances[i]);
                vertex = instances[i].mesh.vertexCount;
            }
        }
        output.Add(currentInstance);
    }

    private Mesh cleanVertex(Mesh mesh, int subMeshID)
    {
        var newMesh = new Mesh();
        newMesh.name = mesh.name;
        Vector3[] oldVtx = mesh.vertices;
        Vector2[] oldUv1 = mesh.uv;
        Vector2[] oldUv2 = mesh.uv2;
        Vector2[] oldUv3 = mesh.uv3;
        Vector2[] oldUv4 = mesh.uv4;
        Vector3[] oldNrm = mesh.normals;
        Color[] oldClr = mesh.colors;

        List<Vector3> newVtx = new List<Vector3>();
        List<Vector2> newUv1 = new List<Vector2>();
        List<Vector2> newUv2 = new List<Vector2>();
        List<Vector2> newUv3 = new List<Vector2>();
        List<Vector2> newUv4 = new List<Vector2>();
        List<Vector3> newNrm = new List<Vector3>();
        List<Color> newClr = new List<Color>();

        List<int> oldTri = new List<int>();
        foreach (var item in mesh.GetTriangles(subMeshID))
        {
            oldTri.Add(item);
        }

        int newTriID = -1;
        for (int i = 0; i < oldTri.Count; i++)
        {
            int oldTriID = oldTri[i];
            if (oldTriID >= 0)
            {
                // nuovo, aggiungo!
                newVtx.Add(oldVtx[oldTriID]);
                if (oldUv1.Length > 0)
                {
                    newUv1.Add(oldUv1[oldTriID]);
                }
                if (oldUv2.Length > 0)
                {
                    newUv2.Add(oldUv2[oldTriID]);
                }
                if (oldUv3.Length > 0)
                {
                    newUv3.Add(oldUv3[oldTriID]);
                }
                if (oldUv4.Length > 0)
                {
                    newUv4.Add(oldUv4[oldTriID]);
                }
                if (oldClr.Length > 0)
                {
                    newClr.Add(oldClr[oldTriID]);
                }
                newNrm.Add(oldNrm[oldTriID]);
                for (int newI = 0; newI < oldTri.Count; newI++)
                {
                    if (oldTri[newI] == oldTriID)
                    {
                        oldTri[newI] = newTriID;
                    }
                }
                newTriID -= 1;
            }
        }

        // rimetto a posto
        for (int i = 0; i < oldTri.Count; i++)
        {
            oldTri[i] = (oldTri[i] * -1) - 1;
        }

        newMesh.Clear();

        newMesh.vertices = newVtx.ToArray();
        newMesh.uv = newUv1.ToArray();
        newMesh.uv2 = newUv2.ToArray();
        newMesh.uv3 = newUv3.ToArray();
        newMesh.uv4 = newUv4.ToArray();
        newMesh.normals = newNrm.ToArray();
        newMesh.triangles = oldTri.ToArray();
        newMesh.colors = newClr.ToArray();
        newMesh.RecalculateBounds();
        //newMesh.Optimize();

        return newMesh;
    }

    public Mesh AutoWeld(Mesh mesh, float threshold, float bucketStep)
    {
        Vector3[] oldVertices = mesh.vertices;
        Vector2[] oldUvs = mesh.uv;
        Color[] oldColors = mesh.colors;
        Vector3[] newVertices = new Vector3[oldVertices.Length];
        Color[] newColors = new Color[oldVertices.Length];
        Vector2[] newUvs = new Vector2[oldUvs.Length];

        int[] old2new = new int[oldVertices.Length];
        int newSize = 0;

        // Find AABB
        Vector3 min = new Vector3(float.MaxValue, float.MaxValue, float.MaxValue);
        Vector3 max = new Vector3(float.MinValue, float.MinValue, float.MinValue);
        for (int i = 0; i < oldVertices.Length; i++)
        {
            if (oldVertices[i].x < min.x) min.x = oldVertices[i].x;
            if (oldVertices[i].y < min.y) min.y = oldVertices[i].y;
            if (oldVertices[i].z < min.z) min.z = oldVertices[i].z;
            if (oldVertices[i].x > max.x) max.x = oldVertices[i].x;
            if (oldVertices[i].y > max.y) max.y = oldVertices[i].y;
            if (oldVertices[i].z > max.z) max.z = oldVertices[i].z;
        }

        // Make cubic buckets, each with dimensions "bucketStep"
        int bucketSizeX = Mathf.FloorToInt((max.x - min.x) / bucketStep) + 1;
        int bucketSizeY = Mathf.FloorToInt((max.y - min.y) / bucketStep) + 1;
        int bucketSizeZ = Mathf.FloorToInt((max.z - min.z) / bucketStep) + 1;
        List<int>[,,] buckets = new List<int>[bucketSizeX, bucketSizeY, bucketSizeZ];

        // Make new vertices
        for (int i = 0; i < oldVertices.Length; i++)
        {
            // Determine which bucket it belongs to
            int x = Mathf.FloorToInt((oldVertices[i].x - min.x) / bucketStep);
            int y = Mathf.FloorToInt((oldVertices[i].y - min.y) / bucketStep);
            int z = Mathf.FloorToInt((oldVertices[i].z - min.z) / bucketStep);

            // Check to see if it's already been added
            if (buckets[x, y, z] == null)
                buckets[x, y, z] = new List<int>(); // Make buckets lazily

            for (int j = 0; j < buckets[x, y, z].Count; j++)
            {
                Vector3 to = newVertices[buckets[x, y, z][j]] - oldVertices[i];
                if (Vector3.SqrMagnitude(to) < threshold)
                {
                    old2new[i] = buckets[x, y, z][j];
                    goto skip; // Skip to next old vertex if this one is already there
                }
            }

            // Add new vertex
            newVertices[newSize] = oldVertices[i];
            newUvs[newSize] = oldUvs[i];
            newColors[newSize] = oldColors[i];
            buckets[x, y, z].Add(newSize);
            old2new[i] = newSize;
            newSize++;

            skip:;
        }

        // Make new triangles
        int[] oldTris = mesh.triangles;
        int[] newTris = new int[oldTris.Length];

        for (int i = 0; i < oldTris.Length; i++)
        {
            newTris[i] = old2new[oldTris[i]];
        }

        Vector3[] finalVertices = new Vector3[newSize];
        Vector2[] finalUvs = new Vector2[newSize];
        Color[] finalColors = new Color[newSize];
        for (int i = 0; i < newSize; i++)
        {
            finalVertices[i] = newVertices[i];
            finalColors[i] = newColors[i];
            // terrain finalUvs[i] = new Vector2(finalVertices[i].x / GroundUvMultiplier, finalVertices[i].z / GroundUvMultiplier);
            finalUvs[i] = newUvs[i];
        }

        mesh.Clear();
        mesh.vertices = finalVertices;
        mesh.uv = finalUvs;
        mesh.triangles = newTris;
        mesh.colors = finalColors;
        //mesh.RecalculateNormals();
        ;

        return mesh;
    }

}
