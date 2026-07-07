using UnityEngine;

// [내부 구현] 노드/간선/갱단 마커를 런타임에 생성하기 위한 공용 헬퍼.
// URP 프로젝트의 라이팅 설정과 무관하게 항상 또렷하게 보이도록 Unlit 셰이더를 사용한다.
// GraphMapSetup(정적 맵)과 GangController(움직이는 갱단 마커)가 공용으로 사용한다.
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
                cachedShader = Shader.Find("Unlit/Color"); // Built-in RP 프로젝트 대비 폴백
            return cachedShader;
        }
    }

    // 노드/갱단 위치를 나타내는 작은 구체 마커를 만든다.
    // keepCollider가 true면 콜라이더를 남겨둔다 (갱단 마커를 마우스로 클릭해서 훔치는 용도).
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

    // 두 지점을 잇는 간선을 LineRenderer로 그린다.
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
        line.material = new Material(UnlitShader);
        line.startColor = color;
        line.endColor = color;

        return line;
    }
}
