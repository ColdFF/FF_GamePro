using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Rebuilds the Level04 dual-light scene-only setup from existing scene objects.
/// </summary>
public static class Level04DualLightSetup
{
    private const string ScenePath = "Assets/Scenes/Level04_DualLight.unity";
    private const string LightSwitchOnGuid = "2df1b0746b4051e46b51a482153cbc42";

    /// <summary>
    /// Purpose: Runs the Level04 setup automatically when the marker file exists.
    /// Input: Unity editor load event and optional autorun marker file.
    /// Output: The Level04 setup is executed once and the marker file is removed.
    /// </summary>
    [InitializeOnLoadMethod]
    private static void AutoRunIfRequested()
    {
        string markerPath = Path.Combine(Application.dataPath, "Editor", "Level04DualLightSetup.autorun");
        if (!File.Exists(markerPath))
        {
            return;
        }

        EditorApplication.delayCall += () =>
        {
            try
            {
                Run();
            }
            finally
            {
                if (File.Exists(markerPath))
                {
                    File.Delete(markerPath);
                }

                AssetDatabase.Refresh();
            }
        };
    }

    /// <summary>
    /// Purpose: Exposes a manual Unity editor menu command for rebuilding the Level04 dual-light setup.
    /// Input: User selecting Tools/ShadowPath/Setup Level04 Dual Light.
    /// Output: The Level04 setup routine is executed for the active Level04 scene.
    /// </summary>
    [MenuItem("Tools/ShadowPath/Setup Level04 Dual Light")]
    public static void RunFromMenu()
    {
        Run();
    }

    /// <summary>
    /// Purpose: Coordinates all Level04 dual-light scene setup steps.
    /// Input: The active Level04 scene or the Level04 scene opened in batch mode.
    /// Output: The scene is configured, marked dirty, and saved.
    /// </summary>
    public static void Run()
    {
        Scene scene;
        if (Application.isBatchMode)
        {
            scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
        }
        else
        {
            scene = SceneManager.GetActiveScene();
            if (scene.path != ScenePath)
            {
                Debug.LogWarning("Level04 dual light setup skipped. Open Assets/Scenes/Level04_DualLight.unity, then run Tools/ShadowPath/Setup Level04 Dual Light.");
                return;
            }
        }

        GameObject root = GameObject.Find("Level04_LightSystem_A");
        if (root == null)
        {
            throw new System.InvalidOperationException("Missing Level04_LightSystem_A in Level04_DualLight.");
        }

        GameObject lightAObject = FindChild(root.transform, "Directional Light_A");
        GameObject lightBObject = FindChild(root.transform, "Directional Light_B");
        GameObject shadowScreen = FindChild(root.transform, "ShadowScreen");
        GameObject stageLampA = FindChild(root.transform, "StageLampTrussRig_A") ?? FindChild(root.transform, "StageLampTrussRig");
        GameObject screenWashA = FindChild(shadowScreen.transform, "ScreenWashLightRig_A") ?? FindChild(shadowScreen.transform, "ScreenWashLightRig");

        if (lightAObject == null || lightBObject == null || shadowScreen == null || stageLampA == null || screenWashA == null)
        {
            throw new System.InvalidOperationException("Level04 light setup is missing one or more source objects.");
        }

        stageLampA.name = "StageLampTrussRig_A";
        shadowScreen.SetActive(true);
        screenWashA.name = "ScreenWashLightRig_A";
        GameObject screenSpotA = FindChild(screenWashA.transform, "ScreenWashSpot_A") ?? FindChild(screenWashA.transform, "ScreenWashSpot");
        if (screenSpotA != null)
        {
            screenSpotA.name = "ScreenWashSpot_A";
        }

        GameObject stageLampB = FindChild(root.transform, "StageLampTrussRig_B");
        if (stageLampB == null)
        {
            stageLampB = Object.Instantiate(stageLampA, root.transform);
            stageLampB.name = "StageLampTrussRig_B";
        }

        SetupStageLampB(root.transform, stageLampA, stageLampB, lightBObject.transform);

        GameObject screenWashB = FindChild(shadowScreen.transform, "ScreenWashLightRig_B");
        if (screenWashB == null)
        {
            screenWashB = Object.Instantiate(screenWashA, shadowScreen.transform);
            screenWashB.name = "ScreenWashLightRig_B";
        }

        SetupScreenWashRig(screenWashA, lightAObject.transform, 0.26f, "ScreenWashSpot_A");
        SetupScreenWashRig(screenWashB, lightBObject.transform, 0.74f, "ScreenWashSpot_B");

        SetupDirectionalLights(lightAObject, lightBObject);
        SetupController(lightAObject, lightBObject, stageLampA, stageLampB, screenWashA, screenWashB);
        SetupPhaseBoundShadowUsers(shadowScreen, lightAObject);
        AddBlackoutFadeLights(lightBObject, screenWashB);

        EditorSceneManager.MarkSceneDirty(scene);
        EditorSceneManager.SaveScene(scene);
        AssetDatabase.SaveAssets();
        Debug.Log("Level04 dual light setup complete.");
    }

    /// <summary>
    /// Purpose: Mirrors and configures the right-side visible lamp rig for Phase B.
    /// Input: Light-system root, Phase A lamp rig, Phase B lamp rig, and Phase B light transform.
    /// Output: Phase B lamp visuals are positioned and wired to the Phase B light.
    /// </summary>
    private static void SetupStageLampB(Transform root, GameObject stageLampA, GameObject stageLampB, Transform lightB)
    {
        Transform pivotA = FindChild(stageLampA.transform, "StageLamp_CenterPivot")?.transform ?? FindChild(stageLampA.transform, "StageLamp_CenterPivot_A")?.transform;
        Transform pivotB = FindChild(stageLampB.transform, "StageLamp_CenterPivot")?.transform ?? FindChild(stageLampB.transform, "StageLamp_CenterPivot_B")?.transform;
        if (pivotA == null || pivotB == null)
        {
            throw new System.InvalidOperationException("Missing StageLamp_CenterPivot on A or B lamp rig.");
        }

        pivotB.name = "StageLamp_CenterPivot_B";

        Vector3 pivotBLocal = pivotB.localPosition;
        pivotBLocal.x = Mathf.Abs(pivotA.localPosition.x);
        pivotB.localPosition = pivotBLocal;

        Camera mainCamera = Object.FindObjectOfType<Camera>();
        float mirrorCenterX = mainCamera != null ? mainCamera.transform.position.x : root.position.x;
        float pivotAWorldX = pivotA.position.x;
        float desiredPivotBWorldX = mirrorCenterX * 2f - pivotAWorldX;

        Vector3 stageLocal = stageLampA.transform.localPosition;
        stageLocal.x = desiredPivotBWorldX - root.position.x - pivotB.localPosition.x;
        stageLampB.transform.localPosition = stageLocal;
        stageLampB.transform.localRotation = stageLampA.transform.localRotation;
        stageLampB.transform.localScale = stageLampA.transform.localScale;

        Vector3 mirroredScale = pivotA.localScale;
        mirroredScale.x = -Mathf.Abs(mirroredScale.x);
        pivotB.localScale = mirroredScale;

        StageLampVisualController visualA = pivotA.GetComponent<StageLampVisualController>();
        StageLampVisualController visualB = pivotB.GetComponent<StageLampVisualController>();
        if (visualB != null)
        {
            visualB.directionalLight = lightB;
            visualB.enabled = visualA != null && visualA.enabled;
            visualB.invertHorizontal = visualA != null && visualA.invertHorizontal;
            visualB.invertVertical = visualA != null && visualA.invertVertical;
        }
    }

    /// <summary>
    /// Purpose: Configures a screen wash rig to follow one directional light's visual position.
    /// Input: Screen wash rig, directional light transform, viewport X anchor, and spot object name.
    /// Output: The wash rig controllers, pulse sound, and spot object are configured.
    /// </summary>
    private static void SetupScreenWashRig(GameObject rig, Transform directionalLight, float baseViewportX, string spotName)
    {
        rig.transform.localPosition = Vector3.zero;
        rig.transform.localRotation = Quaternion.identity;
        rig.transform.localScale = Vector3.one;

        ScreenLightVisualController visual = rig.GetComponent<ScreenLightVisualController>();
        if (visual != null)
        {
            visual.directionalLight = directionalLight;
            visual.player = GameObject.Find("Player")?.transform;
            visual.shadowScreen = FindChild(GameObject.Find("Level04_LightSystem_A").transform, "ShadowScreen")?.transform;
        }

        foreach (ScreenWashLightController wash in rig.GetComponents<ScreenWashLightController>())
        {
            wash.directionalLight = directionalLight;
        }

        CameraLocalWashLightController localWash = rig.GetComponent<CameraLocalWashLightController>();
        if (localWash != null)
        {
            Camera mainCamera = Object.FindObjectOfType<Camera>();
            localWash.targetCamera = mainCamera;
            localWash.controlledTransform = rig.transform;
            localWash.baseViewportX = baseViewportX;
            localWash.baseViewportY = 0.32f;
            localWash.acceptInput = true;
        }

        GameObject spot = FindFirstChild(rig.transform);
        if (spot != null)
        {
            spot.name = spotName;
            Light spotLight = spot.GetComponent<Light>();
            LightUnlockPulse pulse = spot.GetComponent<LightUnlockPulse>();
            if (pulse != null)
            {
                pulse.targetLight = spotLight;
                pulse.pulseAudioSource = spot.GetComponent<AudioSource>();
                pulse.pulseSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(LightSwitchOnGuid));
            }
        }
    }

    /// <summary>
    /// Purpose: Initializes Phase A and Phase B directional light transforms and enabled states.
    /// Input: Phase A and Phase B directional light GameObjects.
    /// Output: Legacy angle controllers are disabled and Phase A starts active.
    /// </summary>
    private static void SetupDirectionalLights(GameObject lightAObject, GameObject lightBObject)
    {
        LightAngleController angleA = lightAObject.GetComponent<LightAngleController>();
        LightAngleController angleB = lightBObject.GetComponent<LightAngleController>();
        if (angleA != null)
        {
            angleA.enabled = false;
        }
        if (angleB != null)
        {
            angleB.enabled = false;
        }

        lightAObject.transform.localRotation = Quaternion.Euler(-15f, 10f, 0f);
        lightBObject.transform.localRotation = Quaternion.Euler(-15f, -10f, 0f);

        Light lightA = lightAObject.GetComponent<Light>();
        Light lightB = lightBObject.GetComponent<Light>();
        if (lightA != null)
        {
            lightA.enabled = true;
        }
        if (lightB != null)
        {
            lightB.enabled = false;
        }
    }

    /// <summary>
    /// Purpose: Creates or updates the Level04 dual-light runtime controller.
    /// Input: Light objects, lamp rigs, and screen wash rigs used by both phases.
    /// Output: The controller is wired with references, audio, and separate A/B rotation ranges.
    /// </summary>
    private static void SetupController(
        GameObject lightAObject,
        GameObject lightBObject,
        GameObject stageLampA,
        GameObject stageLampB,
        GameObject screenWashA,
        GameObject screenWashB)
    {
        GameObject controllerObject = GameObject.Find("Level04_DualLightController");
        if (controllerObject == null)
        {
            controllerObject = new GameObject("Level04_DualLightController");
        }

        AudioSource audioSource = controllerObject.GetComponent<AudioSource>();
        if (audioSource == null)
        {
            audioSource = controllerObject.AddComponent<AudioSource>();
        }
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f;
        audioSource.volume = 0.7f;

        Level04DualLightController controller = controllerObject.GetComponent<Level04DualLightController>();
        if (controller == null)
        {
            controller = controllerObject.AddComponent<Level04DualLightController>();
        }

        controller.switchKey = KeyCode.F;
        controller.directionalLightA = lightAObject.transform;
        controller.lightA = lightAObject.GetComponent<Light>();
        controller.stageLampRigA = stageLampA;
        controller.screenWashRigA = screenWashA;
        controller.washPulseA = FindPulse(screenWashA);
        controller.legacyAngleControllerA = lightAObject.GetComponent<LightAngleController>();
        controller.startXAngleA = -15f;
        controller.startYAngleA = 10f;

        controller.directionalLightB = lightBObject.transform;
        controller.lightB = lightBObject.GetComponent<Light>();
        controller.stageLampRigB = stageLampB;
        controller.screenWashRigB = screenWashB;
        controller.washPulseB = FindPulse(screenWashB);
        controller.legacyAngleControllerB = lightBObject.GetComponent<LightAngleController>();
        controller.startXAngleB = -15f;
        controller.startYAngleB = -10f;

        controller.rotateSpeed = 25f;
        controller.useSeparatePhaseAngleLimits = true;
        controller.minYAngle = -30f;
        controller.maxYAngle = 30f;
        controller.minXAngle = -20f;
        controller.maxXAngle = 5f;
        controller.minYAngleA = 4f;
        controller.maxYAngleA = 30f;
        controller.minXAngleA = -20f;
        controller.maxXAngleA = 5f;
        controller.minYAngleB = -30f;
        controller.maxYAngleB = -4f;
        controller.minXAngleB = -20f;
        controller.maxXAngleB = 5f;
        controller.switchAudioSource = audioSource;
        controller.switchSound = AssetDatabase.LoadAssetAtPath<AudioClip>(AssetDatabase.GUIDToAssetPath(LightSwitchOnGuid));
        controller.switchSoundVolume = 0.7f;
        controller.pulseWashLightOnSwitch = true;
        controller.keepBothStageLampsVisible = true;
        controller.dropAttachedRopesOnLightSwitch = true;

        stageLampA.SetActive(true);
        screenWashA.SetActive(true);
        stageLampB.SetActive(true);
        screenWashB.SetActive(false);
    }

    /// <summary>
    /// Purpose: Marks selected Level04 shadow users as Phase A only, Phase B only, or shared.
    /// Input: Shadow screen and Phase A light object used for projected-shadow initialization.
    /// Output: Key Level04 shadow users receive phase bindings and the decoy block receives platform setup.
    /// </summary>
    private static void SetupPhaseBoundShadowUsers(GameObject shadowScreen, GameObject lightAObject)
    {
        ConfigurePhaseBinding("MovingCaster_01", Level04LightPhaseBinding.PhaseAvailability.PhaseAOnly);
        ConfigurePhaseBinding("MovingCaster_02", Level04LightPhaseBinding.PhaseAvailability.PhaseBOnly);
        ConfigurePhaseBinding("RopeShadowSwing_01", Level04LightPhaseBinding.PhaseAvailability.Both);
        ConfigurePhaseBinding("Ladder_ClimbZone1", Level04LightPhaseBinding.PhaseAvailability.Both);

        GameObject decoyBlock = ConfigurePhaseBinding("Caster_B_DecoyBlock", Level04LightPhaseBinding.PhaseAvailability.PhaseBOnly);
        if (decoyBlock != null)
        {
            SetupProjectedShadowPlatform(decoyBlock, shadowScreen, lightAObject);
        }
    }

    /// <summary>
    /// Purpose: Adds or updates a phase binding on a named Level04 object.
    /// Input: Target object name and desired phase availability.
    /// Output: The configured GameObject is returned, or null if it was not found.
    /// </summary>
    private static GameObject ConfigurePhaseBinding(string objectName, Level04LightPhaseBinding.PhaseAvailability availability)
    {
        GameObject target = GameObject.Find(objectName);
        if (target == null)
        {
            return null;
        }

        Level04LightPhaseBinding binding = target.GetComponent<Level04LightPhaseBinding>();
        if (binding == null)
        {
            binding = target.AddComponent<Level04LightPhaseBinding>();
        }

        binding.availability = availability;
        EditorUtility.SetDirty(target);
        return target;
    }

    /// <summary>
    /// Purpose: Ensures the B-phase decoy caster can generate a walkable projected shadow platform.
    /// Input: Caster object, shadow screen, and initial directional light object.
    /// Output: ProjectedShadowAllEdgePlatform is configured on the caster when required references exist.
    /// </summary>
    private static void SetupProjectedShadowPlatform(GameObject caster, GameObject shadowScreen, GameObject lightAObject)
    {
        MeshRenderer renderer = caster.GetComponent<MeshRenderer>();
        if (renderer == null || shadowScreen == null || lightAObject == null)
        {
            return;
        }

        ProjectedShadowAllEdgePlatform platform = caster.GetComponent<ProjectedShadowAllEdgePlatform>();
        if (platform == null)
        {
            platform = caster.AddComponent<ProjectedShadowAllEdgePlatform>();
        }

        platform.shadowCasterRenderer = renderer;
        platform.shadowScreen = shadowScreen.transform;
        platform.directionalLight = lightAObject.GetComponent<Light>();
        platform.platformThickness = 0.04f;
        platform.platformDepth = 8f;
        platform.edgePadding = 0.02f;
        platform.surfaceOffset = -0.02f;
        platform.minWalkableNormalY = 0.65f;
        platform.addCurvedCasterSamples = true;
        platform.addCurvedTopSupport = true;
        platform.buildAllEdgesForDebug = true;
        platform.walkableEdgesMustBeUpperEnvelope = false;
        platform.useOppositeNormalFallback = true;
        platform.rebuildEveryFrame = true;
        platform.groundLayerName = "Ground";
        platform.addKinematicRigidbody = true;
        platform.carryPlayerWithShadow = true;
        platform.playerTag = "Player";
        platform.showDebugSurface = false;

        EditorUtility.SetDirty(platform);
    }

    /// <summary>
    /// Purpose: Adds the new Phase B lights to Level04 end-blackout fading.
    /// Input: Phase B directional light object and Phase B screen wash rig.
    /// Output: Blackout controllers fade both Phase B light sources at level end.
    /// </summary>
    private static void AddBlackoutFadeLights(GameObject lightBObject, GameObject screenWashB)
    {
        Light directionalB = lightBObject.GetComponent<Light>();
        Light washB = FindFirstChild(screenWashB.transform)?.GetComponent<Light>();
        foreach (LevelEndBlackoutController blackout in Object.FindObjectsOfType<LevelEndBlackoutController>(true))
        {
            SerializedObject serialized = new SerializedObject(blackout);
            SerializedProperty lights = serialized.FindProperty("lightsToFade");
            AddLightReference(lights, directionalB);
            AddLightReference(lights, washB);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }

    /// <summary>
    /// Purpose: Adds a light reference to a serialized list without duplicates.
    /// Input: Serialized light list property and the light to add.
    /// Output: The light appears once in the serialized list.
    /// </summary>
    private static void AddLightReference(SerializedProperty list, Light light)
    {
        if (list == null || light == null)
        {
            return;
        }

        for (int i = 0; i < list.arraySize; i++)
        {
            if (list.GetArrayElementAtIndex(i).objectReferenceValue == light)
            {
                return;
            }
        }

        list.arraySize++;
        list.GetArrayElementAtIndex(list.arraySize - 1).objectReferenceValue = light;
    }

    /// <summary>
    /// Purpose: Finds the LightUnlockPulse attached to a rig's first child.
    /// Input: A screen wash rig GameObject.
    /// Output: The child pulse component, or null if none exists.
    /// </summary>
    private static LightUnlockPulse FindPulse(GameObject rig)
    {
        GameObject spot = FindFirstChild(rig.transform);
        return spot != null ? spot.GetComponent<LightUnlockPulse>() : null;
    }

    /// <summary>
    /// Purpose: Finds a named child anywhere under a transform, including inactive children.
    /// Input: Parent transform and child name to search for.
    /// Output: The matching child GameObject, or null if not found.
    /// </summary>
    private static GameObject FindChild(Transform parent, string name)
    {
        if (parent == null)
        {
            return null;
        }

        foreach (Transform child in parent.GetComponentsInChildren<Transform>(true))
        {
            if (child.name == name)
            {
                return child.gameObject;
            }
        }

        return null;
    }

    /// <summary>
    /// Purpose: Gets the first direct child GameObject of a transform.
    /// Input: Parent transform.
    /// Output: First child GameObject, or null when there is no child.
    /// </summary>
    private static GameObject FindFirstChild(Transform parent)
    {
        if (parent == null || parent.childCount == 0)
        {
            return null;
        }

        return parent.GetChild(0).gameObject;
    }
}
