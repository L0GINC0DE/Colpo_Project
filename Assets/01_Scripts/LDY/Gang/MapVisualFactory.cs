using UnityEngine;

public static class MapVisualFactory
{
    private static Shader cachedShader;

    private static Shader UnlitShader
    {
        get
        {
            if (cachedShader == null)
                cachedShader = Shader.Find("Universal Render Pipeline/Unlit");
            if (cachedShader == null)
                cachedShader = Shader.Find("Unlit/Color"); 
            return cachedShader;
        }
    }

    public static GameObject CreateMarker(string name, Transform parent, Vector3 worldPosition, float scale, Color color, bool keepCollider = false)
    {
        GameObject go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
        go.name = name;

        if (!keepCollider)
            Object.Destroy(go.GetComponent<Collider>()); // 맵 노드는 추적 로직을 노드 그래프로만 처리하므로 물리 충돌체가 불필요

        go.transform.SetParent(parent, false);
        go.transform.position = worldPosition;
        go.transform.localScale = Vector3.one * scale;

        Renderer renderer = go.GetComponent<Renderer>();
        renderer.material = new Material(UnlitShader) { color = color };

        return go;
    }
    
    public static LineRenderer CreateEdgeLine(string name, Transform parent, Vector3 worldA, Vector3 worldB, float width, Color color)
    {
        var go = new GameObject(name);
        go.transform.SetParent(parent, false);

        var line = go.AddComponent<LineRenderer>();
        line.useWorldSpace = true;
        line.positionCount = 2;
        line.SetPosition(0, worldA);
        line.SetPosition(1, worldB);
        line.startWidth = width;
        line.endWidth = width;
        line.material = new Material(UnlitShader) { color = color };
        line.startColor = color;
        line.endColor = color;

        return line;
    }
}
