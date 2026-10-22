using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CraneController))]
public class CraneUI : MonoBehaviour
{
    [SerializeField] private CraneController craneController;

    private Text telemetryText;

    public void SetController(CraneController controller)
    {
        craneController = controller;
    }

    private void Start()
    {
        if (craneController == null)
        {
            craneController = GetComponent<CraneController>();
        }

        BuildDynamicCanvas();
    }

    private void Update()
    {
        if (craneController == null || telemetryText == null) return;

        float mTarget = craneController.MastilTarget;
        float mReal   = craneController.MastilReal;

        float cTarget = craneController.CarroTarget;
        float cReal   = craneController.CarroReal;

        float yTarget = craneController.CableTarget;
        float yReal   = craneController.CableReal;

        float pTarget = craneController.PinzasTarget;
        float pReal   = craneController.PinzasReal;

        telemetryText.text =
            "<b>SIMULADOR GRÚA TORRE - TELEMETRÍA DE DRIVES</b>\n" +
            "------------------------------------------------------------------\n" +
            "Controles: [A/D] Mástil | [W/S] Carro | [R/F] Cable | [Z/X] Pinzas\n" +
            "------------------------------------------------------------------\n" +
            $"1. Mástil (Revolute Y)  | Target: {mTarget,7:F2}° | Real: {mReal,7:F2}° | Δ: {Mathf.Abs(mTarget - mReal),5:F2}°\n" +
            $"2. Carro  (Prismatic Z) | Target: {cTarget,7:F2}m | Real: {cReal,7:F2}m | Δ: {Mathf.Abs(cTarget - cReal),5:F2}m\n" +
            $"3. Cable  (Prismatic Y) | Target: {yTarget,7:F2}m | Real: {yReal,7:F2}m | Δ: {Mathf.Abs(yTarget - yReal),5:F2}m\n" +
            $"4. Pinzas (Revolute Z)  | Target: {pTarget,7:F2}° | Real: {pReal,7:F2}° | Δ: {Mathf.Abs(pTarget - pReal),5:F2}°";
    }

    private void BuildDynamicCanvas()
    {
        GameObject canvasObj = new GameObject("CraneTelemetryCanvas");
        canvasObj.transform.SetParent(transform, false);

        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 100;

        CanvasScaler scaler = canvasObj.AddComponent<CanvasScaler>();
        scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        scaler.referenceResolution = new Vector2(1920f, 1080f);
        scaler.matchWidthOrHeight = 0.5f;

        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject panelObj = new GameObject("TelemetryPanel");
        panelObj.transform.SetParent(canvasObj.transform, false);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.10f, 0.14f, 0.88f);

        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0f, 1f);
        panelRect.anchorMax = new Vector2(0f, 1f);
        panelRect.pivot = new Vector2(0f, 1f);
        panelRect.anchoredPosition = new Vector2(20f, -20f);
        panelRect.sizeDelta = new Vector2(760f, 220f);

        GameObject textObj = new GameObject("TelemetryText");
        textObj.transform.SetParent(panelObj.transform, false);

        telemetryText = textObj.AddComponent<Text>();
        Font builtinFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        if (builtinFont == null)
        {
            builtinFont = Resources.GetBuiltinResource<Font>("Arial.ttf");
        }
        telemetryText.font = builtinFont;
        telemetryText.fontSize = 18;
        telemetryText.lineSpacing = 1.25f;
        telemetryText.supportRichText = true;
        telemetryText.alignment = TextAnchor.MiddleLeft;
        telemetryText.color = new Color(0.92f, 0.96f, 1.0f, 1.0f);

        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(18f, 14f);
        textRect.offsetMax = new Vector2(-18f, -14f);
    }
}
