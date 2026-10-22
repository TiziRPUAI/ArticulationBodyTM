using UnityEditor;
using UnityEngine;

public static class CraneBuilder
{
    [MenuItem("TP/Generar Simulador Grúa")]
    public static void BuildCraneSimulator()
    {
        CleanupExistingSimulator();

        // PhysicMaterial instanciado por código para las pinzas (alta fricción, sin rebote)
        PhysicsMaterial gripperMaterial = CreateGripperPhysicsMaterial();

        // Dimensiones geométricas (en metros) de los 8 cuerpos
        Vector3 baseSize    = new Vector3(3.00f, 0.60f, 3.00f);
        Vector3 mastilSize  = new Vector3(0.80f, 8.00f, 0.80f);
        Vector3 brazoSize   = new Vector3(0.70f, 0.60f, 9.00f);
        Vector3 carroSize   = new Vector3(0.90f, 0.40f, 1.00f);
        Vector3 cableSize   = new Vector3(0.12f, 3.60f, 0.12f);
        Vector3 ganchoSize  = new Vector3(0.90f, 0.35f, 0.60f);
        Vector3 pinzaSize   = new Vector3(0.14f, 0.90f, 0.45f);

        float brazoForwardOffsetZ = 2.50f;
        float carroInitialWorldZ  = 3.50f;
        float carroLocalOffsetZ   = carroInitialWorldZ - brazoForwardOffsetZ;
        float pinzaHorizontalOffsetX = (ganchoSize.x * 0.5f) - (pinzaSize.x * 0.5f);

        // Cálculo matemático de posiciones iniciales en espacio de mundo
        Vector3 basePos = new Vector3(0f, baseSize.y * 0.5f, 0f);
        Vector3 mastilPos = basePos + new Vector3(0f, (baseSize.y + mastilSize.y) * 0.5f, 0f);
        Vector3 brazoPos = mastilPos + new Vector3(0f, (mastilSize.y + brazoSize.y) * 0.5f, brazoForwardOffsetZ);
        Vector3 carroPos = brazoPos + new Vector3(0f, -(brazoSize.y + carroSize.y) * 0.5f, carroLocalOffsetZ);
        Vector3 cablePos = carroPos + new Vector3(0f, -(carroSize.y + cableSize.y) * 0.5f, 0f);
        Vector3 ganchoPos = cablePos + new Vector3(0f, -(cableSize.y + ganchoSize.y) * 0.5f, 0f);
        Vector3 pinzaIzqPos = ganchoPos + new Vector3(-pinzaHorizontalOffsetX, -(ganchoSize.y + pinzaSize.y) * 0.5f, 0f);
        Vector3 pinzaDerPos = ganchoPos + new Vector3(+pinzaHorizontalOffsetX, -(ganchoSize.y + pinzaSize.y) * 0.5f, 0f);

        // 1. CUERPO 1: Base (Raíz inamovible)
        ArticulationBody baseBody = CreateArticulationPart(
            "Base", null, basePos, baseSize,
            new Color(0.22f, 0.24f, 0.27f), 5000f, null);
        baseBody.immovable = true;

        // 2. CUERPO 2: Mástil (Revolute controlado)
        ArticulationBody mastilBody = CreateArticulationPart(
            "Mastil", baseBody.transform, mastilPos, mastilSize,
            new Color(0.92f, 0.68f, 0.10f), 1500f, null);
        mastilBody.jointType = ArticulationJointType.RevoluteJoint;
        ConfigureAnchors(
            mastilBody,
            parentAnchorPos: new Vector3(0f, baseSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, -mastilSize.y * 0.5f, 0f),
            anchorRot: Quaternion.Euler(0f, 0f, 90f));
        mastilBody.twistLock = ArticulationDofLock.LimitedMotion;
        mastilBody.xDrive = CreateDrive(-180f, 180f, stiffness: 120000f, damping: 18000f, forceLimit: 1000000f);

        // 3. CUERPO 3: Brazo (Fixed Joint respecto al Mástil)
        ArticulationBody brazoBody = CreateArticulationPart(
            "Brazo", mastilBody.transform, brazoPos, brazoSize,
            new Color(0.92f, 0.68f, 0.10f), 800f, null);
        brazoBody.jointType = ArticulationJointType.FixedJoint;
        ConfigureAnchors(
            brazoBody,
            parentAnchorPos: new Vector3(0f, mastilSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, -brazoSize.y * 0.5f, -brazoForwardOffsetZ),
            anchorRot: Quaternion.identity);

        // 4. CUERPO 4: Carro / Trolley (Prismatic Joint en eje Z)
        ArticulationBody carroBody = CreateArticulationPart(
            "Carro", brazoBody.transform, carroPos, carroSize,
            new Color(0.18f, 0.45f, 0.78f), 250f, null);
        carroBody.jointType = ArticulationJointType.PrismaticJoint;
        ConfigureAnchors(
            carroBody,
            parentAnchorPos: new Vector3(0f, -brazoSize.y * 0.5f, carroLocalOffsetZ),
            childAnchorPos: new Vector3(0f, carroSize.y * 0.5f, 0f),
            anchorRot: Quaternion.identity);
        carroBody.linearLockX = ArticulationDofLock.LockedMotion;
        carroBody.linearLockY = ArticulationDofLock.LockedMotion;
        carroBody.linearLockZ = ArticulationDofLock.LimitedMotion;
        carroBody.zDrive = CreateDrive(-2.0f, 3.0f, stiffness: 90000f, damping: 14000f, forceLimit: 500000f);

        // 5. CUERPO 5: Cable (Prismatic Joint en eje Y con LineRenderer dinámico y MeshRenderer oculto)
        ArticulationBody cableBody = CreateArticulationPart(
            "Cable", carroBody.transform, cablePos, cableSize,
            new Color(0.35f, 0.38f, 0.42f), 80f, null);
        cableBody.jointType = ArticulationJointType.PrismaticJoint;
        ConfigureAnchors(
            cableBody,
            parentAnchorPos: new Vector3(0f, -carroSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, cableSize.y * 0.5f, 0f),
            anchorRot: Quaternion.identity);
        cableBody.linearLockX = ArticulationDofLock.LockedMotion;
        cableBody.linearLockY = ArticulationDofLock.LimitedMotion;
        cableBody.linearLockZ = ArticulationDofLock.LockedMotion;
        cableBody.yDrive = CreateDrive(-3.5f, 2.0f, stiffness: 95000f, damping: 15000f, forceLimit: 500000f);

        // Ocultar el MeshRenderer original del cable para que no atraviese el carro al subir
        MeshRenderer cableMeshRenderer = cableBody.GetComponentInChildren<MeshRenderer>();
        if (cableMeshRenderer != null)
        {
            cableMeshRenderer.enabled = false;
        }

        // 6. CUERPO 6: Base del gancho (Spherical Joint pasivo sin stiffness: se mueve solo por gravedad)
        ArticulationBody ganchoBody = CreateArticulationPart(
            "BaseGancho", cableBody.transform, ganchoPos, ganchoSize,
            new Color(0.85f, 0.32f, 0.14f), 60f, gripperMaterial);
        ganchoBody.jointType = ArticulationJointType.SphericalJoint;
        ConfigureAnchors(
            ganchoBody,
            parentAnchorPos: new Vector3(0f, -cableSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, ganchoSize.y * 0.5f, 0f),
            anchorRot: Quaternion.Euler(0f, 0f, 90f));
        ganchoBody.twistLock = ArticulationDofLock.FreeMotion;
        ganchoBody.swingYLock = ArticulationDofLock.LimitedMotion;
        ganchoBody.swingZLock = ArticulationDofLock.LimitedMotion;
        ArticulationDrive passiveSphericalDrive = CreatePassiveDrive(-25f, 25f, damping: 25f);
        ganchoBody.xDrive = passiveSphericalDrive;
        ganchoBody.yDrive = passiveSphericalDrive;
        ganchoBody.zDrive = passiveSphericalDrive;
        ganchoBody.linearDamping = 0.5f;
        ganchoBody.angularDamping = 2.5f;

        // Configurar LineRenderer dinámico entre el ancla del Carro y la Base del Gancho
        LineRenderer cableLineRenderer = ConfigureDynamicCableLineRenderer(
            cableBody.gameObject,
            carroBody.transform.TransformPoint(cableBody.parentAnchorPosition),
            ganchoBody.transform.TransformPoint(ganchoBody.anchorPosition));

        // 7. CUERPO 7: Pinza Izquierda (Revolute Joint suave con alto damping y PhysicMaterial de fricción máxima)
        ArticulationBody pinzaIzqBody = CreateArticulationPart(
            "PinzaIzquierda", ganchoBody.transform, pinzaIzqPos, pinzaSize,
            new Color(0.15f, 0.16f, 0.18f), 15f, gripperMaterial);
        pinzaIzqBody.jointType = ArticulationJointType.RevoluteJoint;
        ConfigureAnchors(
            pinzaIzqBody,
            parentAnchorPos: new Vector3(-pinzaHorizontalOffsetX, -ganchoSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, pinzaSize.y * 0.5f, 0f),
            anchorRot: Quaternion.Euler(0f, -90f, 0f));
        pinzaIzqBody.twistLock = ArticulationDofLock.LimitedMotion;
        // Stiffness y forceLimit reducidos drásticamente + damping aumentado (resorte suave amortiguado)
        pinzaIzqBody.xDrive = CreateDrive(-30f, 30f, stiffness: 200f, damping: 120f, forceLimit: 90f);
        AddGripperFingerTip(pinzaIzqBody.transform, pinzaSize, isLeftJaw: true, gripperMaterial);

        // 8. CUERPO 8: Pinza Derecha (Revolute Joint suave con alto damping y PhysicMaterial de fricción máxima)
        ArticulationBody pinzaDerBody = CreateArticulationPart(
            "PinzaDerecha", ganchoBody.transform, pinzaDerPos, pinzaSize,
            new Color(0.15f, 0.16f, 0.18f), 15f, gripperMaterial);
        pinzaDerBody.jointType = ArticulationJointType.RevoluteJoint;
        ConfigureAnchors(
            pinzaDerBody,
            parentAnchorPos: new Vector3(+pinzaHorizontalOffsetX, -ganchoSize.y * 0.5f, 0f),
            childAnchorPos: new Vector3(0f, pinzaSize.y * 0.5f, 0f),
            anchorRot: Quaternion.Euler(0f, 90f, 0f));
        pinzaDerBody.twistLock = ArticulationDofLock.LimitedMotion;
        // Stiffness y forceLimit reducidos drásticamente + damping aumentado (resorte suave amortiguado)
        pinzaDerBody.xDrive = CreateDrive(-30f, 30f, stiffness: 200f, damping: 120f, forceLimit: 90f);
        AddGripperFingerTip(pinzaDerBody.transform, pinzaSize, isLeftJaw: false, gripperMaterial);

        // Adjuntar y vincular CraneController y CraneUI al objeto Raíz (Base)
        CraneController controller = baseBody.gameObject.AddComponent<CraneController>();
        controller.ConfigureReferences(
            mastilBody, carroBody, cableBody, ganchoBody, pinzaIzqBody, pinzaDerBody, cableLineRenderer);

        CraneUI ui = baseBody.gameObject.AddComponent<CraneUI>();
        ui.SetController(controller);

        // Crear entorno de interacción: Plano base + 3 cubos con Rigidbody estándar
        BuildEnvironmentAndLoads(gripperMaterial);

        FrameMainCamera();

        Undo.RegisterCreatedObjectUndo(baseBody.gameObject, "Generar Simulador Grúa");
        Selection.activeGameObject = baseBody.gameObject;
    }

    private static LineRenderer ConfigureDynamicCableLineRenderer(
        GameObject cableObject,
        Vector3 startWorldPos,
        Vector3 endWorldPos)
    {
        LineRenderer lr = cableObject.AddComponent<LineRenderer>();
        lr.useWorldSpace = true;
        lr.positionCount = 2;
        lr.startWidth = 0.08f;
        lr.endWidth = 0.08f;
        lr.numCapVertices = 4;
        lr.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.On;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }
        if (shader == null)
        {
            shader = Shader.Find("Sprites/Default");
        }

        if (shader != null)
        {
            Material cableMat = new Material(shader);
            Color cableColor = new Color(0.25f, 0.27f, 0.30f);
            if (cableMat.HasProperty("_BaseColor")) cableMat.SetColor("_BaseColor", cableColor);
            if (cableMat.HasProperty("_Color")) cableMat.SetColor("_Color", cableColor);
            lr.sharedMaterial = cableMat;
        }

        lr.SetPosition(0, startWorldPos);
        lr.SetPosition(1, endWorldPos);
        return lr;
    }

    private static ArticulationBody CreateArticulationPart(
        string partName,
        Transform parent,
        Vector3 worldPos,
        Vector3 dimensions,
        Color color,
        float mass,
        PhysicsMaterial physicsMat)
    {
        GameObject partObj = new GameObject(partName);
        if (parent != null)
        {
            partObj.transform.SetParent(parent, false);
        }
        partObj.transform.position = worldPos;
        partObj.transform.rotation = Quaternion.identity;
        partObj.transform.localScale = Vector3.one;

        BoxCollider box = partObj.AddComponent<BoxCollider>();
        box.size = dimensions;
        if (physicsMat != null)
        {
            box.sharedMaterial = physicsMat;
        }

        GameObject visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visual.name = "Mesh_" + partName;
        Object.DestroyImmediate(visual.GetComponent<Collider>());
        visual.transform.SetParent(partObj.transform, false);
        visual.transform.localPosition = Vector3.zero;
        visual.transform.localRotation = Quaternion.identity;
        visual.transform.localScale = dimensions;
        AssignRendererColor(visual.GetComponent<Renderer>(), color);

        ArticulationBody body = partObj.AddComponent<ArticulationBody>();
        body.mass = mass;
        body.useGravity = true;
        body.solverIterations = 24;
        body.solverVelocityIterations = 12;
        body.maxDepenetrationVelocity = 1.5f;
        body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;

        return body;
    }

    private static void AddGripperFingerTip(Transform jawTransform, Vector3 jawSize, bool isLeftJaw, PhysicsMaterial mat)
    {
        Vector3 tipSize = new Vector3(0.14f, 0.12f, jawSize.z);
        float inwardDir = isLeftJaw ? 1f : -1f;
        Vector3 tipLocalPos = new Vector3(
            inwardDir * (jawSize.x * 0.5f + tipSize.x * 0.4f),
            -jawSize.y * 0.5f + tipSize.y * 0.5f,
            0f);

        GameObject tipObj = GameObject.CreatePrimitive(PrimitiveType.Cube);
        tipObj.name = "UñaAgarre";
        tipObj.transform.SetParent(jawTransform, false);
        tipObj.transform.localPosition = tipLocalPos;
        tipObj.transform.localRotation = Quaternion.identity;
        tipObj.transform.localScale = tipSize;

        Collider tipCollider = tipObj.GetComponent<Collider>();
        if (tipCollider != null && mat != null)
        {
            tipCollider.sharedMaterial = mat;
        }

        AssignRendererColor(tipObj.GetComponent<Renderer>(), new Color(0.85f, 0.32f, 0.14f));
    }

    private static void ConfigureAnchors(
        ArticulationBody body,
        Vector3 parentAnchorPos,
        Vector3 childAnchorPos,
        Quaternion anchorRot)
    {
        body.matchAnchors = false;
        body.parentAnchorPosition = parentAnchorPos;
        body.anchorPosition = childAnchorPos;
        body.parentAnchorRotation = anchorRot;
        body.anchorRotation = anchorRot;
    }

    private static ArticulationDrive CreateDrive(
        float lowerLimit,
        float upperLimit,
        float stiffness,
        float damping,
        float forceLimit)
    {
        return new ArticulationDrive
        {
            lowerLimit = lowerLimit,
            upperLimit = upperLimit,
            stiffness = stiffness,
            damping = damping,
            forceLimit = forceLimit,
            target = 0f,
            targetVelocity = 0f,
            driveType = ArticulationDriveType.Force
        };
    }

    private static ArticulationDrive CreatePassiveDrive(float lowerLimit, float upperLimit, float damping)
    {
        return new ArticulationDrive
        {
            lowerLimit = lowerLimit,
            upperLimit = upperLimit,
            stiffness = 0f,
            damping = damping,
            forceLimit = float.MaxValue,
            target = 0f,
            targetVelocity = 0f,
            driveType = ArticulationDriveType.Force
        };
    }

    private static void BuildEnvironmentAndLoads(PhysicsMaterial gripMat)
    {
        GameObject envRoot = new GameObject("EntornoSimuladorGrua");

        GameObject plane = GameObject.CreatePrimitive(PrimitiveType.Plane);
        plane.name = "PlanoBase";
        plane.transform.SetParent(envRoot.transform, false);
        plane.transform.position = Vector3.zero;
        plane.transform.localScale = new Vector3(3.0f, 1.0f, 3.0f);
        AssignRendererColor(plane.GetComponent<Renderer>(), new Color(0.28f, 0.30f, 0.33f));

        Vector3 cubeScale = new Vector3(0.55f, 0.55f, 0.55f);
        float halfH = cubeScale.y * 0.5f;

        Vector3[] cubePositions =
        {
            new Vector3(0.0f, halfH, 3.5f),
            new Vector3(2.5f, halfH, 4.5f),
            new Vector3(-2.5f, halfH, 2.5f)
        };

        Color[] cubeColors =
        {
            new Color(0.85f, 0.22f, 0.22f),
            new Color(0.20f, 0.72f, 0.32f),
            new Color(0.22f, 0.50f, 0.88f)
        };

        float[] cubeMasses = { 8f, 10f, 12f };

        for (int i = 0; i < 3; i++)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = $"CuboClasificable_{i + 1}";
            cube.transform.SetParent(envRoot.transform, false);
            cube.transform.position = cubePositions[i];
            cube.transform.localScale = cubeScale;

            Collider col = cube.GetComponent<Collider>();
            if (col != null && gripMat != null)
            {
                col.sharedMaterial = gripMat;
            }

            Rigidbody rb = cube.AddComponent<Rigidbody>();
            rb.mass = cubeMasses[i];
            rb.linearDamping = 0.2f;
            rb.angularDamping = 0.8f;
            rb.maxDepenetrationVelocity = 1.5f;
            rb.interpolation = RigidbodyInterpolation.Interpolate;
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

            AssignRendererColor(cube.GetComponent<Renderer>(), cubeColors[i]);
        }

        Undo.RegisterCreatedObjectUndo(envRoot, "Generar Entorno Grúa");
    }

    private static PhysicsMaterial CreateGripperPhysicsMaterial()
    {
        return new PhysicsMaterial("GripperHighFriction")
        {
            dynamicFriction = 1f,
            staticFriction = 1f,
            bounciness = 0f,
            frictionCombine = PhysicsMaterialCombine.Maximum,
            bounceCombine = PhysicsMaterialCombine.Minimum
        };
    }

    private static void AssignRendererColor(Renderer renderer, Color color)
    {
        if (renderer == null) return;

        Shader shader = Shader.Find("Universal Render Pipeline/Lit");
        if (shader == null)
        {
            shader = Shader.Find("Standard");
        }

        if (shader != null)
        {
            Material mat = new Material(shader);
            if (mat.HasProperty("_BaseColor"))
            {
                mat.SetColor("_BaseColor", color);
            }
            if (mat.HasProperty("_Color"))
            {
                mat.SetColor("_Color", color);
            }
            renderer.sharedMaterial = mat;
        }
    }

    private static void FrameMainCamera()
    {
        Camera cam = Camera.main;
        if (cam != null)
        {
            cam.transform.position = new Vector3(9.5f, 7.5f, -4.5f);
            cam.transform.LookAt(new Vector3(0f, 4.2f, 2.5f));
        }
    }

    private static void CleanupExistingSimulator()
    {
        GameObject oldBase = GameObject.Find("Base");
        if (oldBase != null && oldBase.GetComponent<CraneController>() != null)
        {
            Object.DestroyImmediate(oldBase);
        }

        GameObject oldEnv = GameObject.Find("EntornoSimuladorGrua");
        if (oldEnv != null)
        {
            Object.DestroyImmediate(oldEnv);
        }
    }
}
