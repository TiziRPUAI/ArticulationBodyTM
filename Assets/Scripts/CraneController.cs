using UnityEngine;
#if ENABLE_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

public class CraneController : MonoBehaviour
{
    [Header("Referencias de ArticulationBody")]
    [SerializeField] private ArticulationBody mastilBody;
    [SerializeField] private ArticulationBody carroBody;
    [SerializeField] private ArticulationBody cableBody;
    [SerializeField] private ArticulationBody ganchoBody;
    [SerializeField] private ArticulationBody pinzaIzquierdaBody;
    [SerializeField] private ArticulationBody pinzaDerechaBody;

    [Header("Renderizado Dinámico del Cable")]
    [SerializeField] private LineRenderer cableLineRenderer;

    [Header("Velocidades de Control")]
    [SerializeField] private float mastilSpeedDegPerSec = 45f;
    [SerializeField] private float carroSpeedMetersPerSec = 2.0f;
    [SerializeField] private float cableSpeedMetersPerSec = 1.8f;
    [SerializeField] private float pinzasSpeedDegPerSec = 40f;

    [Header("Límites de Operación")]
    [SerializeField] private Vector2 mastilLimits = new Vector2(-180f, 180f);
    [SerializeField] private Vector2 carroLimits = new Vector2(-2.0f, 3.0f);
    [SerializeField] private Vector2 cableLimits = new Vector2(-3.5f, 2.0f);
    [SerializeField] private Vector2 pinzasLimits = new Vector2(-30f, 30f);

    private float mastilTarget;
    private float carroTarget;
    private float cableTarget;
    private float pinzasTarget;

    // Propiedades públicas / Getters para Target y Valor Real (jointPosition) de los 4 drives
    public float MastilTarget => mastilTarget;
    public float MastilReal => GetRevolutePositionDegrees(mastilBody);

    public float CarroTarget => carroTarget;
    public float CarroReal => GetPrismaticPositionMeters(carroBody);

    public float CableTarget => cableTarget;
    public float CableReal => GetPrismaticPositionMeters(cableBody);

    public float PinzasTarget => pinzasTarget;
    public float PinzasReal => GetPinzasRealDegrees();

    public void ConfigureReferences(
        ArticulationBody mastil,
        ArticulationBody carro,
        ArticulationBody cable,
        ArticulationBody gancho,
        ArticulationBody pinzaIzq,
        ArticulationBody pinzaDer,
        LineRenderer cableRenderer = null)
    {
        mastilBody = mastil;
        carroBody = carro;
        cableBody = cable;
        ganchoBody = gancho;
        pinzaIzquierdaBody = pinzaIzq;
        pinzaDerechaBody = pinzaDer;
        cableLineRenderer = cableRenderer;
    }

    private void Awake()
    {
        AutoAssignReferencesIfMissing();
        SetupDynamicCableRenderer();
        IgnoreInternalStructureCollisions();
        SyncInitialTargetsFromDrives();
        UpdateDynamicCablePositions();
    }

    private void FixedUpdate()
    {
        float dt = Time.fixedDeltaTime;

        // 1. Drive Mástil (Revolute - Eje Y): A / D
        float mastilInput = ReadAxis(KeyPreset.MastilNegative, KeyPreset.MastilPositive);
        if (Mathf.Abs(mastilInput) > 0.001f)
        {
            mastilTarget = Mathf.Clamp(mastilTarget + mastilInput * mastilSpeedDegPerSec * dt, mastilLimits.x, mastilLimits.y);
        }

        // 2. Drive Carro / Trolley (Prismatic - Eje Z): W / S
        float carroInput = ReadAxis(KeyPreset.CarroNegative, KeyPreset.CarroPositive);
        if (Mathf.Abs(carroInput) > 0.001f)
        {
            carroTarget = Mathf.Clamp(carroTarget + carroInput * carroSpeedMetersPerSec * dt, carroLimits.x, carroLimits.y);
        }

        // 3. Drive Cable (Prismatic - Eje Y): F (bajar) / R (subir) o Q / E
        float cableInput = ReadAxis(KeyPreset.CableNegative, KeyPreset.CablePositive);
        if (Mathf.Abs(cableInput) > 0.001f)
        {
            cableTarget = Mathf.Clamp(cableTarget + cableInput * cableSpeedMetersPerSec * dt, cableLimits.x, cableLimits.y);
        }

        // 4. Drive Pinzas (Revolute - Apertura/Cierre): Z (abrir) / X (cerrar)
        float pinzasInput = ReadAxis(KeyPreset.PinzasNegative, KeyPreset.PinzasPositive);
        if (Mathf.Abs(pinzasInput) > 0.001f)
        {
            pinzasTarget = Mathf.Clamp(pinzasTarget + pinzasInput * pinzasSpeedDegPerSec * dt, pinzasLimits.x, pinzasLimits.y);
        }

        ApplyDriveTargets();
        UpdateDynamicCablePositions();
    }

    private void LateUpdate()
    {
        UpdateDynamicCablePositions();
    }

    private void UpdateDynamicCablePositions()
    {
        if (cableLineRenderer == null || carroBody == null || ganchoBody == null) return;

        Vector3 startAnchorWorld = cableBody != null
            ? carroBody.transform.TransformPoint(cableBody.parentAnchorPosition)
            : carroBody.transform.position;

        Vector3 endHookWorld = ganchoBody.transform.TransformPoint(ganchoBody.anchorPosition);

        cableLineRenderer.SetPosition(0, startAnchorWorld);
        cableLineRenderer.SetPosition(1, endHookWorld);
    }

    private void SetupDynamicCableRenderer()
    {
        if (cableBody == null) return;

        MeshRenderer originalMesh = cableBody.GetComponentInChildren<MeshRenderer>();
        if (originalMesh != null)
        {
            originalMesh.enabled = false;
        }

        if (cableLineRenderer == null)
        {
            cableLineRenderer = cableBody.GetComponent<LineRenderer>();
        }

        if (cableLineRenderer == null)
        {
            cableLineRenderer = cableBody.gameObject.AddComponent<LineRenderer>();
            cableLineRenderer.useWorldSpace = true;
            cableLineRenderer.positionCount = 2;
            cableLineRenderer.startWidth = 0.08f;
            cableLineRenderer.endWidth = 0.08f;
        }
    }

    private void ApplyDriveTargets()
    {
        if (mastilBody != null)
        {
            ArticulationDrive drive = mastilBody.xDrive;
            drive.target = mastilTarget;
            mastilBody.xDrive = drive;
        }

        if (carroBody != null)
        {
            ArticulationDrive drive = carroBody.zDrive;
            drive.target = carroTarget;
            carroBody.zDrive = drive;
        }

        if (cableBody != null)
        {
            ArticulationDrive drive = cableBody.yDrive;
            drive.target = cableTarget;
            cableBody.yDrive = drive;
        }

        if (pinzaIzquierdaBody != null)
        {
            ArticulationDrive driveIzq = pinzaIzquierdaBody.xDrive;
            driveIzq.target = pinzasTarget;
            pinzaIzquierdaBody.xDrive = driveIzq;
        }

        if (pinzaDerechaBody != null)
        {
            ArticulationDrive driveDer = pinzaDerechaBody.xDrive;
            driveDer.target = pinzasTarget;
            pinzaDerechaBody.xDrive = driveDer;
        }
    }

    private void SyncInitialTargetsFromDrives()
    {
        if (mastilBody != null) mastilTarget = mastilBody.xDrive.target;
        if (carroBody != null) carroTarget = carroBody.zDrive.target;
        if (cableBody != null) cableTarget = cableBody.yDrive.target;
        if (pinzaIzquierdaBody != null) pinzasTarget = pinzaIzquierdaBody.xDrive.target;
    }

    private void AutoAssignReferencesIfMissing()
    {
        if (mastilBody == null) mastilBody = FindBodyByName("Mastil");
        if (carroBody == null) carroBody = FindBodyByName("Carro");
        if (cableBody == null) cableBody = FindBodyByName("Cable");
        if (ganchoBody == null) ganchoBody = FindBodyByName("BaseGancho");
        if (pinzaIzquierdaBody == null) pinzaIzquierdaBody = FindBodyByName("PinzaIzquierda");
        if (pinzaDerechaBody == null) pinzaDerechaBody = FindBodyByName("PinzaDerecha");
    }

    private ArticulationBody FindBodyByName(string childName)
    {
        ArticulationBody[] bodies = GetComponentsInChildren<ArticulationBody>(true);
        foreach (ArticulationBody body in bodies)
        {
            if (body.name == childName)
            {
                return body;
            }
        }
        return null;
    }

    private void IgnoreInternalStructureCollisions()
    {
        Collider[] craneColliders = GetComponentsInChildren<Collider>(true);
        for (int i = 0; i < craneColliders.Length; i++)
        {
            for (int j = i + 1; j < craneColliders.Length; j++)
            {
                Physics.IgnoreCollision(craneColliders[i], craneColliders[j], true);
            }
        }
    }

    private static float GetRevolutePositionDegrees(ArticulationBody body)
    {
        if (body == null || body.dofCount == 0) return 0f;
        return body.jointPosition[0] * Mathf.Rad2Deg;
    }

    private static float GetPrismaticPositionMeters(ArticulationBody body)
    {
        if (body == null || body.dofCount == 0) return 0f;
        return body.jointPosition[0];
    }

    private float GetPinzasRealDegrees()
    {
        if (pinzaIzquierdaBody != null && pinzaDerechaBody != null &&
            pinzaIzquierdaBody.dofCount > 0 && pinzaDerechaBody.dofCount > 0)
        {
            float izq = pinzaIzquierdaBody.jointPosition[0] * Mathf.Rad2Deg;
            float der = pinzaDerechaBody.jointPosition[0] * Mathf.Rad2Deg;
            return (izq + der) * 0.5f;
        }
        return GetRevolutePositionDegrees(pinzaIzquierdaBody);
    }

    private enum KeyPreset
    {
        MastilNegative,
        MastilPositive,
        CarroNegative,
        CarroPositive,
        CableNegative,
        CablePositive,
        PinzasNegative,
        PinzasPositive
    }

    private static float ReadAxis(KeyPreset negative, KeyPreset positive)
    {
        float value = 0f;
        if (IsPressed(negative)) value -= 1f;
        if (IsPressed(positive)) value += 1f;
        return value;
    }

    private static bool IsPressed(KeyPreset preset)
    {
#if ENABLE_INPUT_SYSTEM
        Keyboard kb = Keyboard.current;
        if (kb != null)
        {
            switch (preset)
            {
                case KeyPreset.MastilNegative: return kb.aKey.isPressed || kb.leftArrowKey.isPressed;
                case KeyPreset.MastilPositive: return kb.dKey.isPressed || kb.rightArrowKey.isPressed;
                case KeyPreset.CarroNegative:  return kb.sKey.isPressed || kb.downArrowKey.isPressed;
                case KeyPreset.CarroPositive:  return kb.wKey.isPressed || kb.upArrowKey.isPressed;
                case KeyPreset.CableNegative:  return kb.fKey.isPressed || kb.qKey.isPressed;
                case KeyPreset.CablePositive:  return kb.rKey.isPressed || kb.eKey.isPressed;
                case KeyPreset.PinzasNegative: return kb.zKey.isPressed || kb.oKey.isPressed;
                case KeyPreset.PinzasPositive: return kb.xKey.isPressed || kb.pKey.isPressed;
            }
        }
#endif
#if ENABLE_LEGACY_INPUT_MANAGER
        switch (preset)
        {
            case KeyPreset.MastilNegative: return Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow);
            case KeyPreset.MastilPositive: return Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow);
            case KeyPreset.CarroNegative:  return Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
            case KeyPreset.CarroPositive:  return Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
            case KeyPreset.CableNegative:  return Input.GetKey(KeyCode.F) || Input.GetKey(KeyCode.Q);
            case KeyPreset.CablePositive:  return Input.GetKey(KeyCode.R) || Input.GetKey(KeyCode.E);
            case KeyPreset.PinzasNegative: return Input.GetKey(KeyCode.Z) || Input.GetKey(KeyCode.O);
            case KeyPreset.PinzasPositive: return Input.GetKey(KeyCode.X) || Input.GetKey(KeyCode.P);
        }
#endif
        return false;
    }
}
