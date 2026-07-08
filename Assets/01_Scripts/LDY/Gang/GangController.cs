using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

public class GangController : MonoBehaviour
{
    public GangData gangData;
    public bool pursuing;
    public string playerBaseId = "base";

    // 리스타트 기준 노드. currentNodeId는 플레이 중 계속 바뀌고 세션 넘어 안 돌아올 때도
    // 있어서 따로 저장해둔다. 비워두면 Start()에서 자동으로 채워짐.
    [SerializeField] private string spawnNodeId;

    [SerializeField] private float moveDuration = 0.4f;

    // 직진파 전용 - A*를 한 번만 계산해서 캐싱해두고 이후엔 재탐색 없이 그대로 따라감.
    private List<string> cachedPath;
    private int cachedPathIndex;

    private float initialDamageResistance;

    // 얼리기: 남은 정지 턴. 0보다 크면 ProcessTurn에서 이동을 스킵.
    // 얼면 마커 위에 FreezeItemHandler가 지정한 프리팹/머티리얼을 그대로 겹쳐 보여줌.
    private int frozenTurnsRemaining;
    private GameObject iceOverlay;

    // 플레이어가 강제로 지정한 경로. 있는 동안은 원래 AI 대신 이걸 한 칸씩 따라가고,
    // 다 쓰면 원래 AI로 복귀한다.
    private List<string> overridePath;
    private int overridePathIndex;

    // 노이즈: 남은 턴 수. 0보다 크면 시너지가 전부 무효(FindActiveSynergyEffect가 무조건 null).
    private int noiseTurnsRemaining;
    private GameObject noiseOverlay;

    // 합체 이동: 보급파/디버프파는 뭉친 갱단이 있으면 자기 AI 대신 그쪽 이동에 끌려간다.
    // movedOnTurn은 이번 턴에 이미 움직였는지 표시(중복 이동 방지용).
    private int lastTurnCount;
    private int movedOnTurn = -1;

    private void Start()
    {
        if (string.IsNullOrEmpty(spawnNodeId))
            spawnNodeId = gangData.currentNodeId;

        // ScriptableObject라 이전 Play 세션 값이 남아있을 수 있어서 시작할 때 강제로 초기화.
        gangData.currentNodeId = spawnNodeId;
        gangData.currentFunds = gangData.maxFunds;
        gangData.hackLockedUntilTurn = 0;
        initialDamageResistance = gangData.damageResistance;

        if (GraphMapSetup.Instance != null && GraphMapSetup.Instance.Nodes.TryGetValue(spawnNodeId, out MapNode node))
            transform.position = node.position;

        MapVisualFactory.CreateMarker($"GangVisual_{gangData.gangName}", transform, transform.position, 0.7f, GetGangColor(gangData.type), keepCollider: true);
    }

    // 얼리기 스킬: turns턴 동안 ProcessTurn에서 이동을 건너뛰게 하고, 얼음 오버레이를 켠다.
    public void Freeze(int turns)
    {
        frozenTurnsRemaining = Mathf.Max(frozenTurnsRemaining, turns);
        SetIceOverlayActive(true);
        Debug.Log($"[{gangData.gangName}] 얼음 - {frozenTurnsRemaining}턴 동안 정지");
    }

    private const float IceSizeAnimDuration = 1.2f;

    private const float IceMaxSize = 2f;

    private void SetIceOverlayActive(bool active)
    {
        if (active)
        {
            if (iceOverlay == null)
                iceOverlay = CreateIceOverlay();

            iceOverlay.SetActive(true);
            Material material = GetIceMaterial();
            if (material == null)
                return;

            material.DOKill();
            if (material.HasProperty("_size"))
            {
                material.SetFloat("_size", 0f);
                material.DOFloat(IceMaxSize, "_size", IceSizeAnimDuration);
            }
        }
        else if (iceOverlay != null)
        {
            Material material = GetIceMaterial();
            if (material == null)
            {
                iceOverlay.SetActive(false);
                return;
            }

            material.DOKill();
            if (material.HasProperty("_size"))
                material.DOFloat(0f, "_size", IceSizeAnimDuration).OnComplete(() => iceOverlay.SetActive(false));
            else
                iceOverlay.SetActive(false);
        }
    }

    // 프리팹이면 렌더러가 자식에 있을 수도 있어서 GetComponentInChildren로 찾는다.
    private Material GetIceMaterial()
    {
        Renderer renderer = iceOverlay.GetComponentInChildren<Renderer>();
        return renderer != null ? renderer.material : null;
    }

    // 프리팹 > 머티리얼 > 기본 흰 스프라이트 순으로 fallback.
    private GameObject CreateIceOverlay()
    {
        GameObject prefab = FreezeItemHandler.Instance != null ? FreezeItemHandler.Instance.IcePrefab : null;
        if (prefab != null)
        {
            GameObject instance = Instantiate(prefab, transform);
            instance.name = $"Ice_{gangData.gangName}";
            // 프리팹 만들 때 있던 씬 위치가 로컬 좌표에 남아있어서 어긋날 수 있다 - 중심에
            // 맞추고, z만 마커(반지름 0.35 구체) 앞으로 당긴다.
            instance.transform.localPosition = new Vector3(0f, 0f, -0.5f);
            return instance;
        }

        Material material = FreezeItemHandler.Instance != null ? FreezeItemHandler.Instance.IceMaterial : null;
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = $"Ice_{gangData.gangName}";
        Destroy(overlay.GetComponent<Collider>());

        overlay.transform.SetParent(transform, false);
        overlay.transform.localPosition = new Vector3(0f, 0f, -0.5f);
        overlay.transform.localScale = Vector3.one * 0.9f;

        Renderer renderer = overlay.GetComponent<Renderer>();
        renderer.material = material != null ? new Material(material) : new Material(Shader.Find("Sprites/Default"));

        return overlay;
    }

    // 노이즈: 시너지 무효화됐다는 걸 보여주는 회색 반투명 구체.
    public void ApplyNoise(int turns)
    {
        noiseTurnsRemaining = Mathf.Max(noiseTurnsRemaining, turns);
        SetNoiseOverlayActive(true);
        Debug.Log($"[{gangData.gangName}] 노이즈 - {noiseTurnsRemaining}턴 동안 시너지/상성 무효화");
    }

    // 매 턴 호출: 노이즈 지속 턴을 줄이고, 0이 되면 오버레이를 끈다.
    public void TickNoise()
    {
        if (noiseTurnsRemaining <= 0)
            return;

        noiseTurnsRemaining--;
        if (noiseTurnsRemaining == 0)
            SetNoiseOverlayActive(false);
    }

    private void SetNoiseOverlayActive(bool active)
    {
        if (active)
        {
            if (noiseOverlay == null)
                noiseOverlay = CreateCircleOverlay("Noise", 0.85f, new Color(0.5f, 0.5f, 0.5f, 0.5f));
            noiseOverlay.SetActive(true);
        }
        else if (noiseOverlay != null)
        {
            noiseOverlay.SetActive(false);
        }
    }

    // 알파 마스크 텍스처 씌운 반투명 Quad 공통 생성 로직 (노이즈=원, 얼음=팔각형).
    // 머티리얼은 갱단마다 복제해야 함 - 공유하면 여러 갱단이 서로 덮어씀.
    private GameObject CreateMaskedOverlay(string label, float scale, Color color, Texture2D mask)
    {
        GameObject overlay = GameObject.CreatePrimitive(PrimitiveType.Quad);
        overlay.name = $"{label}_{gangData.gangName}";
        Destroy(overlay.GetComponent<Collider>());

        overlay.transform.SetParent(transform, false);
        // 마커가 Sphere(반지름 0.35)라 표면이 z=-0.35까지 튀어나온다. 그것보다 확실히 더
        // 앞으로 당겨야 안 가려짐.
        overlay.transform.localPosition = new Vector3(0f, 0f, -0.5f);
        overlay.transform.localScale = Vector3.one * scale;

        Renderer renderer = overlay.GetComponent<Renderer>();
        renderer.material = new Material(Shader.Find("Sprites/Default"))
        {
            mainTexture = mask,
            color = color
        };

        return overlay;
    }

    private GameObject CreateCircleOverlay(string label, float scale, Color color)
    {
        return CreateMaskedOverlay(label, scale, color, GetCircleAlphaTexture());
    }

    private static Texture2D cachedCircleAlphaTexture;

    // 흰 원 + 밖은 투명. 색은 material.color로 곱해서 입힌다. 다 같이 쓰는 모양이라 캐싱.
    private static Texture2D GetCircleAlphaTexture()
    {
        if (cachedCircleAlphaTexture != null)
            return cachedCircleAlphaTexture;

        const int size = 64;
        var texture = new Texture2D(size, size, TextureFormat.RGBA32, false);
        float radius = size * 0.5f;
        Vector2 center = new Vector2(radius, radius);

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dist = Vector2.Distance(new Vector2(x + 0.5f, y + 0.5f), center);
                texture.SetPixel(x, y, new Color(1f, 1f, 1f, dist <= radius ? 1f : 0f));
            }
        }

        texture.Apply();
        cachedCircleAlphaTexture = texture;
        return texture;
    }

    // path[0]은 현재 위치와 같아야 하고 그 뒤는 인접 노드로 중복 없이 이어져야 한다
    // (PathRedirectHandler가 클릭할 때마다 검증해서 넘겨줌). 설정되면 다음 턴부터
    // 이 경로를 강제로 따라간다.
    public void SetOverridePath(List<string> path)
    {
        if (path == null || path.Count < 2 || path[0] != gangData.currentNodeId)
        {
            Debug.LogWarning($"[{gangData.gangName}] 강제 경로 설정 실패 - 잘못된 경로");
            return;
        }

        overridePath = path;
        overridePathIndex = 0;
        // 캐시를 비워서 다음에 새로 재탐색하게 한다. cachedPathIndex도 같이 안 돌리면
        // 엉뚱한 인덱스로 새 경로를 읽어서 이상한 노드로 튄다.
        cachedPath = null;
        cachedPathIndex = 0;
        Debug.Log($"[{gangData.gangName}] 강제 경로 설정: {string.Join(" -> ", path)}");
    }

    private void ProcessOverridePath(Func<string, string, bool> isEdgeBlocked)
    {
        string nextNode = overridePath[overridePathIndex + 1];
        if (isEdgeBlocked != null && isEdgeBlocked(gangData.currentNodeId, nextNode))
        {
            Debug.Log($"[{gangData.gangName}] 강제 경로 - 길이 막혀 대기");
            return;
        }

        overridePathIndex++;
        MoveTo(nextNode);

        if (overridePathIndex + 1 >= overridePath.Count)
            overridePath = null; // 마지막 칸 도착 - 다음 턴부터 원래 AI 재개.
    }

    // 리스타트: 자금/위치/추적 상태/캐시를 전부 시작 시점으로 되돌리고, 와해돼서
    // 꺼졌던 오브젝트도 다시 켠다.
    public void ResetToStart()
    {
        gameObject.SetActive(true);

        // 이동 트윈이랑 트레일 트윈(SetTarget(this)로 태깅됨) 둘 다 죽여야 한다. 안 그러면
        // 리셋 도중이던 트윈의 OnComplete가 뒤늦게 실행되면서 방금 되돌린 색을 다시 덮어쓴다.
        transform.DOKill();
        DOTween.Kill(this);

        gangData.currentNodeId = spawnNodeId;
        gangData.currentFunds = gangData.maxFunds;
        gangData.damageResistance = initialDamageResistance;
        pursuing = false;
        cachedPath = null;
        cachedPathIndex = 0;

        frozenTurnsRemaining = 0;
        SetIceOverlayActive(false);

        overridePath = null;
        overridePathIndex = 0;

        noiseTurnsRemaining = 0;
        SetNoiseOverlayActive(false);

        gangData.hackLockedUntilTurn = 0;
        movedOnTurn = -1;

        if (GraphMapSetup.Instance != null && GraphMapSetup.Instance.Nodes.TryGetValue(spawnNodeId, out MapNode node))
            transform.position = node.position;
    }

    private void OnEnable()
    {
        GameEvents.OnGangDefeated += HandleGangDefeated;
    }

    private void OnDisable()
    {
        GameEvents.OnGangDefeated -= HandleGangDefeated;
    }

    private void HandleGangDefeated(string gangId)
    {
        if (gangData != null && gangData.gangName == gangId)
            gameObject.SetActive(false);
    }

    private static Color GetGangColor(GangType type)
    {
        switch (type)
        {
            case GangType.Direct: return Color.red;
            case GangType.Intelligent: return new Color(0.6f, 0f, 1f);
            case GangType.Greedy: return new Color(1f, 0.5f, 0f);
            case GangType.Scale: return Color.magenta;
            case GangType.Chaos: return Color.yellow;
            case GangType.Support: return Color.green;
            case GangType.Debuff: return new Color(0.2f, 0.6f, 0.2f);
            case GangType.Tanker: return new Color(0.3f, 0.3f, 0.35f);
            default: return Color.gray;
        }
    }

    public void UpdateResistance(int turnCount)
    {
        switch (gangData.type)
        {
            case GangType.Scale:
                // 10턴마다 한 단계씩 오름(연속 상승 아님). 증가폭은 SO에서 갱단별로 조정 가능.
                gangData.damageResistance = Mathf.Min(0.8f, (turnCount / 10) * gangData.resistanceGainPerTenTurns);
                break;

            case GangType.Tanker:
                gangData.damageResistance = 0.3f;
                break;
        }
    }

    // 매 턴 호출(추적 여부 무관). ProcessTurn 안에만 있으면 pursuing 아닌 갱단은 카운트가
    // 영원히 안 줄어들어서 여기 따로 뺐다.
    public void TickFreeze()
    {
        if (frozenTurnsRemaining <= 0)
            return;

        frozenTurnsRemaining--;
        Debug.Log($"[{gangData.gangName}] 얼어서 이번 턴 정지 (남은 턴: {frozenTurnsRemaining})");

        if (frozenTurnsRemaining == 0)
            SetIceOverlayActive(false);
    }

    public void ProcessTurn(int turnCount, AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        // 미니게임(사전 해킹) 실패로 락 걸린 상태면 이번 턴은 아무 것도 안 한다.
        if (turnCount < gangData.hackLockedUntilTurn)
        {
            Debug.Log($"{gangData.gangName} 해킹 락 상태 (턴 {gangData.hackLockedUntilTurn}까지)");
            return;
        }

        lastTurnCount = turnCount;

        // 이번 턴에 이미(대표 갱단을 따라) 이동했으면 자기 AI는 건너뛴다.
        if (movedOnTurn == turnCount)
            return;

        if (frozenTurnsRemaining > 0)
            return;

        if (overridePath != null)
        {
            ProcessOverridePath(isEdgeBlocked);
            return;
        }

        // 보급파/디버프파는 뭉친 갱단 있으면 자기 힘으로 안 움직이고 그쪽 MoveTo에 끌려간다.
        if ((gangData.type == GangType.Support || gangData.type == GangType.Debuff) && GetClumpedPartners().Count > 0)
            return;

        switch (gangData.type)
        {
            case GangType.Direct:
                ProcessDirect(pathfinder, isEdgeBlocked);
                break;

            case GangType.Intelligent:
                ProcessIntelligent(pathfinder, isEdgeBlocked);
                break;

            case GangType.Greedy:
                ProcessGreedy(pathfinder, isEdgeBlocked);
                break;

            case GangType.Scale:
                {
                    int speed = 1 + turnCount / 3;
                    ProcessSpeedMove(pathfinder, isEdgeBlocked, speed);
                    break;
                }

            case GangType.Chaos:
                ProcessChaos(pathfinder, isEdgeBlocked);
                break;

            case GangType.Support:
                // 기지 추적은 안 하고, 시너지 확정된 타입(지금은 직진파)만 찾아간다.
                // 아무나 쫓아가던 옛날 방식이랑 다르다 - 시너지 목적 전용.
                ProcessSeekSynergyPartner(pathfinder, isEdgeBlocked);
                break;

            case GangType.Debuff:
                ProcessSeekNearestGang(pathfinder, isEdgeBlocked);
                break;

            case GangType.Tanker:
                ProcessSpeedMove(pathfinder, isEdgeBlocked, 1);
                break;
        }
    }

    // (내 성향, 상대 성향) → (속도 배율, 벽 무시 여부). 없는 조합은 그냥 효과 없음.
    // 같은 성향끼리도 키가 될 수 있다(예: (Direct, Direct)).
    private static readonly Dictionary<(GangType, GangType), (float speedMultiplier, bool ignoreBlockedEdges)> SynergyEffects =
        new Dictionary<(GangType, GangType), (float speedMultiplier, bool ignoreBlockedEdges)>
        {
            // 확정: 직진파+보급파 - 이동속도 2배, 막힌 간선(벽) 무시하고 통과.
            { (GangType.Direct, GangType.Support), (2f, true) },

            // TODO: 시너지 표 확정되면 여기 추가
        };

    // 딕셔너리에 정의된 시너지가 있으면 그 효과를, 없으면 null을 반환한다.
    private (float speedMultiplier, bool ignoreBlockedEdges)? FindActiveSynergyEffect()
    {
        if (noiseTurnsRemaining > 0)
            return null; // 노이즈 중에는 시너지가 전부 무효.

        foreach (GangController partner in GetClumpedPartners())
        {
            if (SynergyEffects.TryGetValue((gangData.type, partner.gangData.type), out var effect))
                return effect;
        }
        return null;
    }

    // 이 갱단이 현재 확정된 시너지 효과에 참여 중인지 (위험도 판정 등에서 사용).
    public bool HasActiveSynergyEffect() => FindActiveSynergyEffect() != null;

    // 실제 위험도 조회는 항상 이 메서드를 거친다 - gangData.baseRiskLevel을 직접 참조하지 말 것.
    public RiskLevel GetCurrentRiskLevel() => gangData.GetEffectiveRiskLevel(HasActiveSynergyEffect());

    // 시너지(직진파+보급파 전용): 같은 노드에 보급파가 있으면 속도 2배 + 벽(간선 차단) 무시.
    private void ProcessDirect(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        if (cachedPath == null)
            cachedPath = pathfinder.FindPath(gangData.currentNodeId, playerBaseId);

        if (cachedPath == null)
            return;

        var effect = FindActiveSynergyEffect();
        int speed = effect.HasValue ? Mathf.Max(1, Mathf.RoundToInt(effect.Value.speedMultiplier)) : 1;
        Func<string, string, bool> effectiveBlocked = (effect.HasValue && effect.Value.ignoreBlockedEdges) ? null : isEdgeBlocked;

        int steps = 0;
        while (steps < speed && cachedPathIndex + 1 < cachedPath.Count)
        {
            string nextNode = cachedPath[cachedPathIndex + 1];
            if (effectiveBlocked != null && effectiveBlocked(cachedPath[cachedPathIndex], nextNode))
            {
                Debug.Log($"[{gangData.gangName}] 직진파 - 길이 막혀 대기");
                break;
            }

            cachedPathIndex++;
            steps++;
        }

        if (steps > 0)
            MoveTo(cachedPath[cachedPathIndex]);
    }

    // 지능파: 매 턴 재탐색해서 막힌 길 우회. 시너지는 아직 없음.
    private void ProcessIntelligent(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        List<string> path = pathfinder.FindPath(gangData.currentNodeId, playerBaseId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        MoveTo(path[1]);
    }

    // 탐욕파: 훔친 돈 많을수록 빨라짐. 시너지는 아직 없음.
    private void ProcessGreedy(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        int stolenSoFar = gangData.maxFunds - gangData.currentFunds;
        int speed = 1 + stolenSoFar / 250;
        ProcessSpeedMove(pathfinder, isEdgeBlocked, speed);
    }

    // 디버프파: 기지 말고 가장 가까운 아무 갱단이나 쫓아간다. 이번 스펙 범위 밖이라 그대로 둠.
    private void ProcessSeekNearestGang(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        string targetNodeId = FindNearestOtherGangNodeId(pathfinder);
        if (targetNodeId == null || targetNodeId == gangData.currentNodeId)
            return;

        List<string> path = pathfinder.FindPath(gangData.currentNodeId, targetNodeId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        MoveTo(path[1]);
    }

    private string FindNearestOtherGangNodeId(AStarPathfinder pathfinder)
    {
        if (GangManager.Instance == null)
            return null;

        string nearest = null;
        int bestHops = int.MaxValue;
        foreach (GangController other in GangManager.Instance.GetAllGangControllers())
        {
            if (other == this || !other.gameObject.activeSelf)
                continue;

            List<string> path = pathfinder.FindPath(gangData.currentNodeId, other.gangData.currentNodeId);
            if (path != null && path.Count < bestHops)
            {
                bestHops = path.Count;
                nearest = other.gangData.currentNodeId;
            }
        }
        return nearest;
    }

    // 보급파: 아무 갱단이나 찾아가는 게 아니라 SynergyEffects에 등록된 상대 타입만
    // 찾아간다(지금은 사실상 직진파). 해당하는 타입이 없으면 그냥 안 움직인다.
    private void ProcessSeekSynergyPartner(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        string targetNodeId = FindNearestSynergyPartnerNodeId(pathfinder);
        if (targetNodeId == null || targetNodeId == gangData.currentNodeId)
            return;

        List<string> path = pathfinder.FindPath(gangData.currentNodeId, targetNodeId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        MoveTo(path[1]);
    }

    private string FindNearestSynergyPartnerNodeId(AStarPathfinder pathfinder)
    {
        if (GangManager.Instance == null)
            return null;

        // SynergyEffects의 키 중 내 타입이 포함된 조합에서 상대 타입만 뽑는다.
        var partnerTypes = new HashSet<GangType>();
        foreach (var key in SynergyEffects.Keys)
        {
            if (key.Item1 == gangData.type)
                partnerTypes.Add(key.Item2);
            else if (key.Item2 == gangData.type)
                partnerTypes.Add(key.Item1);
        }
        if (partnerTypes.Count == 0)
            return null;

        string nearest = null;
        int bestHops = int.MaxValue;
        foreach (GangController other in GangManager.Instance.GetAllGangControllers())
        {
            if (other == this || !other.gameObject.activeSelf || !partnerTypes.Contains(other.gangData.type))
                continue;

            List<string> path = pathfinder.FindPath(gangData.currentNodeId, other.gangData.currentNodeId);
            if (path != null && path.Count < bestHops)
            {
                bestHops = path.Count;
                nearest = other.gangData.currentNodeId;
            }
        }
        return nearest;
    }

    // 같은 노드에 있는 다른(활성 상태) 갱단 목록.
    private List<GangController> GetClumpedPartners()
    {
        var result = new List<GangController>();
        if (GangManager.Instance == null)
            return result;

        foreach (GangController other in GangManager.Instance.GetAllGangControllers())
        {
            if (other != this && other.gameObject.activeSelf && other.gangData.currentNodeId == gangData.currentNodeId)
                result.Add(other);
        }
        return result;
    }

    // 탐욕파/왕귀파 공용: 경로를 매 턴 재탐색해서 속도만큼 전진, 막힌 간선 만나면 거기서 멈춤.
    private void ProcessSpeedMove(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked, int speed)
    {
        List<string> path = pathfinder.FindPath(gangData.currentNodeId, playerBaseId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        int index = 0;
        int steps = 0;
        while (steps < speed && index + 1 < path.Count)
        {
            if (isEdgeBlocked != null && isEdgeBlocked(path[index], path[index + 1]))
                break;

            index++;
            steps++;
        }

        if (index > 0)
            MoveTo(path[index]);
    }

    // 금고 한도 초과("망") 처리용. 직진파 캐시 경로도 무시하고 새로 계산해서 steps칸
    // 강제 전진, 막힌 간선 만나면 거기서 멈춘다.
    public void ForceAdvance(int steps, AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        // 캐시 비워서 재탐색 유도. cachedPathIndex도 같이 리셋 안 하면 다음 ProcessDirect가
        // 엉뚱한 인덱스로 새 경로를 읽어서 이상한 데로 튄다.
        cachedPath = null;
        cachedPathIndex = 0;

        List<string> path = pathfinder.FindPath(gangData.currentNodeId, playerBaseId, isEdgeBlocked);
        if (path == null || path.Count < 2)
            return;

        int index = 0;
        int moved = 0;
        while (moved < steps && index + 1 < path.Count)
        {
            if (isEdgeBlocked != null && isEdgeBlocked(path[index], path[index + 1]))
                break;

            index++;
            moved++;
        }

        if (index > 0)
            MoveTo(path[index]);

        Debug.Log($"[{gangData.gangName}] 금고 한도 초과 - {moved}칸 강제 전진");
    }

    // 불예측파: 40% 확률로 인접 노드 중 무작위 이동, 그 외에는 평범하게 A* 경로로 한 칸 전진.
    private void ProcessChaos(AStarPathfinder pathfinder, Func<string, string, bool> isEdgeBlocked)
    {
        if (UnityEngine.Random.value < 0.4f)
        {
            string randomNode = RandomNeighbor(gangData.currentNodeId, isEdgeBlocked);
            if (randomNode == null)
                return;

            Debug.Log($"[{gangData.gangName}] 불예측파 - 무작위 이동");
            MoveTo(randomNode);
        }
        else
        {
            ProcessSpeedMove(pathfinder, isEdgeBlocked, 1);
        }
    }

    private string RandomNeighbor(string fromNodeId, Func<string, string, bool> isEdgeBlocked)
    {
        if (GraphMapSetup.Instance == null || !GraphMapSetup.Instance.Nodes.TryGetValue(fromNodeId, out MapNode node))
            return null;

        var reachable = new List<string>();
        foreach (string neighbor in node.connectedNodeIds)
        {
            if (isEdgeBlocked == null || !isEdgeBlocked(fromNodeId, neighbor))
                reachable.Add(neighbor);
        }

        return reachable.Count > 0 ? reachable[UnityEngine.Random.Range(0, reachable.Count)] : null;
    }

    private void MoveTo(string nodeId)
    {
        Debug.Log($"[{gangData.gangName}] {gangData.currentNodeId} -> {nodeId} 이동");

        // 뭉쳐있던 보급파/디버프파 동행자 중 얼지 않고 이번 턴에 아직 안 움직인 애들은
        // 같이 데려간다.
        var companions = new List<GangController>();
        foreach (GangController partner in GetClumpedPartners())
        {
            bool isFollowerType = partner.gangData.type == GangType.Support || partner.gangData.type == GangType.Debuff;
            bool free = partner.frozenTurnsRemaining <= 0 && partner.movedOnTurn != lastTurnCount;
            if (isFollowerType && free)
                companions.Add(partner);
        }

        MapNode fromNode = null;
        MapNode toNode = null;
        if (GraphMapSetup.Instance != null)
        {
            GraphMapSetup.Instance.Nodes.TryGetValue(gangData.currentNodeId, out fromNode);
            GraphMapSetup.Instance.Nodes.TryGetValue(nodeId, out toNode);
        }

        gangData.currentNodeId = nodeId;
        movedOnTurn = lastTurnCount;

        Color trailColor = GetGangColor(gangData.type);
        if (fromNode != null && toNode != null)
        {
            transform.DOKill();
            transform.DOMove(toNode.position, moveDuration).SetEase(Ease.InOutSine);
            PlayMoveTrail(fromNode, toNode, trailColor);
        }

        if (nodeId == playerBaseId)
        {
            pursuing = false;
            GameEvents.GangReachedBase(gangData.gangName);
        }

        // 동행자도 같은 색(대표 갱단 색)으로 트레일을 칠해서 같이 움직인 티가 나게 한다.
        foreach (GangController companion in companions)
            companion.MoveAlong(nodeId, lastTurnCount, trailColor);
    }

    // 대표 갱단(MoveTo 호출한 쪽)을 따라 같은 목적지로 이동. 트레일 색도 대표 색 그대로 쓴다.
    private void MoveAlong(string nodeId, int turnCount, Color trailColor)
    {
        if (movedOnTurn == turnCount)
            return;

        Debug.Log($"[{gangData.gangName}] {gangData.currentNodeId} -> {nodeId} 동행 이동");

        MapNode fromNode = null;
        MapNode toNode = null;
        if (GraphMapSetup.Instance != null)
        {
            GraphMapSetup.Instance.Nodes.TryGetValue(gangData.currentNodeId, out fromNode);
            GraphMapSetup.Instance.Nodes.TryGetValue(nodeId, out toNode);
        }

        gangData.currentNodeId = nodeId;
        movedOnTurn = turnCount;
        lastTurnCount = turnCount;

        if (fromNode != null && toNode != null)
        {
            transform.DOKill();
            transform.DOMove(toNode.position, moveDuration).SetEase(Ease.InOutSine);
            PlayMoveTrail(fromNode, toNode, trailColor);
        }

        if (nodeId == playerBaseId)
        {
            pursuing = false;
            GameEvents.GangReachedBase(gangData.gangName);
        }
    }

    private void PlayMoveTrail(MapNode fromNode, MapNode toNode, Color color)
    {
        Transform parent = GraphMapSetup.Instance != null ? GraphMapSetup.Instance.TrailContainer : transform;

        Vector3 from3 = new Vector3(fromNode.position.x, fromNode.position.y, -0.05f);
        Vector3 to3 = new Vector3(toNode.position.x, toNode.position.y, -0.05f);

        LineRenderer trail = MapVisualFactory.CreateEdgeLine(
            $"Trail_{gangData.gangName}_{fromNode.id}-{toNode.id}", parent, from3, from3, 0.08f, color);

        FlashNode(fromNode.id, color);

        float progress = 0f;
        DOTween.To(() => progress, x => progress = x, 1f, moveDuration)
            .SetTarget(this)
            .OnUpdate(() => trail.SetPosition(1, Vector3.Lerp(from3, to3, progress)))
            .OnComplete(() => FlashNode(toNode.id, color));
    }

    private static void FlashNode(string nodeId, Color color)
    {
        if (GraphMapSetup.Instance == null || !GraphMapSetup.Instance.NodeRenderers.TryGetValue(nodeId, out Renderer renderer))
            return;

        renderer.material.DOKill();
        renderer.material.DOColor(color, 0.2f);
    }
}
