using Silk.NET.Assimp;
using Silk.NET.Maths;
using Buffer = Silk.NET.Vulkan.Buffer;

public unsafe class Model
{
    public const string MODEL_PATH = @"Assets\cat.obj";

    public Buffer vertexBuffer;
    public Buffer indexBuffer;

    public Vertex[]? vertices;
    public uint[]? indices;

    public void Init(Game game)
    {
        using var assimp = Assimp.GetApi();
        var scene = assimp.ImportFile(MODEL_PATH, (uint)PostProcessPreset.TargetRealTimeMaximumQuality);

        var vertexMap = new Dictionary<Vertex, uint>();
        var tempVertices = new List<Vertex>();
        var tempIndices = new List<uint>();

        VisitSceneNode(scene->MRootNode);

        assimp.ReleaseImport(scene);

        vertices = [.. tempVertices];
        indices = [.. tempIndices];

        void VisitSceneNode(Node* node)
        {
            for (int m = 0; m < node->MNumMeshes; m++)
            {
                var mesh = scene->MMeshes[node->MMeshes[m]];

                for (int f = 0; f < mesh->MNumFaces; f++)
                {
                    var face = mesh->MFaces[f];

                    for (int i = 0; i < face.MNumIndices; i++)
                    {
                        uint index = face.MIndices[i];

                        var position = mesh->MVertices[index];
                        var texture = mesh->MTextureCoords[0][(int)index];

                        Vertex vertex = new()
                        {
                            pos = new Vector3D<float>(position.X, position.Y, position.Z),
                            color = new Vector3D<float>(1, 1, 1),
                            //Flip Y for OBJ in Vulkan
                            textCoord = new Vector2D<float>(texture.X, 1.0f - texture.Y)
                        };

                        if (vertexMap.TryGetValue(vertex, out var meshIndex))
                        {
                            tempIndices.Add(meshIndex);
                        }
                        else
                        {
                            tempIndices.Add((uint)tempVertices.Count);
                            vertexMap[vertex] = (uint)tempVertices.Count;
                            tempVertices.Add(vertex);
                        }
                    }
                }
            }

            for (int c = 0; c < node->MNumChildren; c++)
            {
                VisitSceneNode(node->MChildren[c]);
            }
        }
    }
}