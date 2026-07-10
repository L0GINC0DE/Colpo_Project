using UnityEngine;

public static class LDY_MapVisualFactory
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

    // nodeAId/nodeBId를 넘기면 클릭 가능한 콜라이더(+LDY_EdgeMarker)도 같이 만들어준다.
    // 벽 아이템처럼 "이 길을 클릭해서 고른다"가 필요한 경우에만 사용.
    public static LineRenderer CreateClickableEdgeLine(string name, Transform parent, Vector3 worldA, Vector3 worldB, float width, Color color, string nodeAId, string nodeBId)
    {
        LineRenderer line = CreateEdgeLine(name, parent, worldA, worldB, width, color);

        var colliderGo = new GameObject($"{name}_Collider");
        colliderGo.transform.SetParent(parent, false);

        Vector3 diff = worldB - worldA;
        colliderGo.transform.position = (worldA + worldB) * 0.5f;
        colliderGo.transform.rotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg);

        BoxCollider box = colliderGo.AddComponent<BoxCollider>();
        box.size = new Vector3(diff.magnitude, Mathf.Max(width * 4f, 0.3f), 0.3f);

        LDY_EdgeMarker marker = colliderGo.AddComponent<LDY_EdgeMarker>();
        marker.nodeAId = nodeAId;
        marker.nodeBId = nodeBId;

        return line;
    }
}
