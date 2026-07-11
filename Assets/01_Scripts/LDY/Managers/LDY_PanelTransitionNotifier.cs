// 패널(기지/맵/상점 등) 전환 시점에 자동 저장을 걸기 위한 자리. 아직 그 전환을 담당하는
// 컨트롤러가 없어서 호출부는 없음.
// 패널 전환 컨트롤러 완성되면 전환 시점에 LDY_PanelTransitionNotifier.
// NotifyPanelChanged(패널이름) 호출 추가할 것
public static class LDY_PanelTransitionNotifier
{
    public static void NotifyPanelChanged(string panelName)
    {
        if (LDY_SaveSystem.Instance != null)
            LDY_SaveSystem.Instance.AutoSave($"패널 전환: {panelName}");
    }
}
