using ErkekTavlasi.UnityView;
using System.IO;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public static class BackgammonSceneBuilder
{
    private const string MaterialsPath = "Assets/Materials";
    private const string PrefabsPath = "Assets/Prefabs";
    private const string ScenesPath = "Assets/Scenes";
    private const string TexturesPath = "Assets/Textures";

    [MenuItem("Erkek Tavlasi/Build 2026 Scene")]
    public static void BuildScene()
    {
        EnsureFolders();
        AssetDatabase.Refresh();
        Material wood = CreateMaterial("Polished Walnut Case", new Color(0.32f, 0.14f, 0.050f), 0.48f, 0.14f, $"{TexturesPath}/walnut_grain.png");
        Material rail = CreateMaterial("Dark Raised Walnut Rail", new Color(0.17f, 0.070f, 0.026f), 0.54f, 0.18f, $"{TexturesPath}/walnut_grain.png");
        Material innerWood = CreateMaterial("Warm Maple Playing Field", new Color(0.42f, 0.22f, 0.10f), 0.35f, 0.03f);
        Material ivoryPoint = CreateMaterial("QA Point White", new Color(0.98f, 0.94f, 0.78f), 0.26f, 0.00f);
        Material redPoint = CreateMaterial("QA Point Black", new Color(0.030f, 0.028f, 0.025f), 0.30f, 0.00f);
        Material whiteChecker = CreateMaterial("Carved Ivory Checker", new Color(0.92f, 0.87f, 0.72f), 0.36f, 0.22f, $"{TexturesPath}/ivory_checker.png");
        Material blackChecker = CreateMaterial("Polished Ebony Checker", new Color(0.012f, 0.010f, 0.009f), 0.32f, 0.55f, $"{TexturesPath}/ebony_checker.png");
        Material highlight = CreateTransparentMaterial("Legal Move Glow", new Color(0.22f, 1.0f, 0.55f, 0.92f));
        Material diceMaterial = CreateMaterial("Clean White Dice", new Color(0.96f, 0.94f, 0.88f), 0.26f, 0.04f);
        Material pipMaterial = CreateMaterial("Large Black Pips", new Color(0.005f, 0.005f, 0.006f), 0.28f, 0.16f);
        Material tableMaterial = CreateMaterial("Dark Dining Table", new Color(0.11f, 0.07f, 0.045f), 0.55f, 0.08f);
        Material brass = CreateMaterial("Muted Brass Hinge", new Color(0.78f, 0.55f, 0.22f), 0.32f, 0.65f);
        Material trayWhite = CreateMaterial("Right Bottom Home Slot", new Color(0.28f, 0.14f, 0.070f), 0.38f, 0.05f);
        Material trayBlack = CreateMaterial("Right Top Home Slot", new Color(0.24f, 0.115f, 0.055f), 0.40f, 0.06f);

        GameObject whitePrefab = CreateCheckerPrefab("WhiteChecker", whiteChecker);
        GameObject blackPrefab = CreateCheckerPrefab("BlackChecker", blackChecker);
        GameObject highlightPrefab = CreateHighlightPrefab(highlight);
        GameObject diePrefab = CreateDiePrefab(diceMaterial, pipMaterial);

        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "BackgammonScene";

        GameObject root = new GameObject("Backgammon Table");
        CreateCamera();
        CreateLights();
        CreateTable(tableMaterial);
        CreateBoard(root.transform, wood, rail, innerWood, ivoryPoint, redPoint, brass, trayWhite, trayBlack);

        Transform checkersRoot = new GameObject("Runtime Checkers").transform;
        Transform highlightsRoot = new GameObject("Runtime Highlights").transform;
        GameObject controllerObject = new GameObject("Game Controller");
        BackgammonGameController controller = controllerObject.AddComponent<BackgammonGameController>();
        controller.WhiteCheckerPrefab = whitePrefab;
        controller.BlackCheckerPrefab = blackPrefab;
        controller.HighlightPrefab = highlightPrefab;
        controller.CheckersRoot = checkersRoot;
        controller.HighlightsRoot = highlightsRoot;

        GameObject dieA = (GameObject)PrefabUtility.InstantiatePrefab(diePrefab);
        dieA.name = "Die A";
        dieA.transform.position = new Vector3(-1.5f, 0.62f, 0f);
        GameObject dieB = (GameObject)PrefabUtility.InstantiatePrefab(diePrefab);
        dieB.name = "Die B";
        dieB.transform.position = new Vector3(0.5f, 0.62f, 0f);
        controller.DieA = dieA.GetComponent<DiceView>();
        controller.DieB = dieB.GetComponent<DiceView>();

        CreateUi(controller, cameraObject: GameObject.Find("Player Camera"));

        EditorSceneManager.SaveScene(scene, $"{ScenesPath}/BackgammonScene.unity");
        EditorBuildSettings.scenes = new[]
        {
            new EditorBuildSettingsScene($"{ScenesPath}/BackgammonScene.unity", true)
        };
        PlayerSettings.SplashScreen.show = false;
        PlayerSettings.SplashScreen.showUnityLogo = false;
        PlayerSettings.fullScreenMode = FullScreenMode.FullScreenWindow;
        PlayerSettings.defaultScreenWidth = 1920;
        PlayerSettings.defaultScreenHeight = 1080;
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
    }

    [MenuItem("Erkek Tavlasi/Build Windows Player")]
    public static void BuildWindowsPlayer()
    {
        BuildScene();
        Directory.CreateDirectory("Builds");
        BuildReport report = BuildPipeline.BuildPlayer(new BuildPlayerOptions
        {
            scenes = new[] { $"{ScenesPath}/BackgammonScene.unity" },
            locationPathName = "Builds/ErkekTavlasiUnity.exe",
            target = BuildTarget.StandaloneWindows64,
            options = BuildOptions.None
        });

        if (report.summary.result != BuildResult.Succeeded)
        {
            throw new System.Exception("Windows player build failed: " + report.summary.result);
        }
    }

    private static void EnsureFolders()
    {
        Directory.CreateDirectory(MaterialsPath);
        Directory.CreateDirectory(PrefabsPath);
        Directory.CreateDirectory(ScenesPath);
        Directory.CreateDirectory(TexturesPath);
    }

    private static Material CreateMaterial(string name, Color color, float smoothness, float metallic, string texturePath = null)
    {
        string path = $"{MaterialsPath}/{name}.mat";
        AssetDatabase.DeleteAsset(path);
        Material material = new Material(Shader.Find("Standard"));
        material.name = name;
        material.color = color;
        if (!string.IsNullOrEmpty(texturePath))
        {
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(texturePath);
            if (texture != null)
            {
                material.mainTexture = texture;
            }
        }

        material.SetFloat("_Glossiness", smoothness);
        material.SetFloat("_Metallic", metallic);
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static Material CreateTransparentMaterial(string name, Color color)
    {
        string path = $"{MaterialsPath}/{name}.mat";
        AssetDatabase.DeleteAsset(path);
        Material material = new Material(Shader.Find("Sprites/Default"));
        material.name = name;
        material.color = color;
        material.renderQueue = 4000; // Overlay - renders after everything
        AssetDatabase.CreateAsset(material, path);
        return material;
    }

    private static GameObject CreateCheckerPrefab(string name, Material material)
    {
        GameObject root = new GameObject(name);
        GameObject body = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        body.name = "Body";
        body.transform.SetParent(root.transform, false);
        body.transform.localScale = new Vector3(BoardLayout.CheckerRadius * 2f, BoardLayout.CheckerHeight * 0.5f, BoardLayout.CheckerRadius * 2f);
        body.GetComponent<Renderer>().sharedMaterial = material;

        GameObject bevel = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        bevel.name = "Raised Rim";
        bevel.transform.SetParent(root.transform, false);
        bevel.transform.localPosition = Vector3.up * 0.085f;
        bevel.transform.localScale = new Vector3(BoardLayout.CheckerRadius * 1.68f, 0.015f, BoardLayout.CheckerRadius * 1.68f);
        bevel.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(bevel.GetComponent<Collider>());

        GameObject groove = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        groove.name = "Inner Carved Ring";
        groove.transform.SetParent(root.transform, false);
        groove.transform.localPosition = Vector3.up * 0.104f;
        groove.transform.localScale = new Vector3(BoardLayout.CheckerRadius * 1.18f, 0.009f, BoardLayout.CheckerRadius * 1.18f);
        groove.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(groove.GetComponent<Collider>());

        GameObject center = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        center.name = "Small Center Medallion";
        center.transform.SetParent(root.transform, false);
        center.transform.localPosition = Vector3.up * 0.116f;
        center.transform.localScale = new Vector3(BoardLayout.CheckerRadius * 0.38f, 0.008f, BoardLayout.CheckerRadius * 0.38f);
        center.GetComponent<Renderer>().sharedMaterial = material;
        Object.DestroyImmediate(center.GetComponent<Collider>());

        string path = $"{PrefabsPath}/{name}.prefab";
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(root, path);
        Object.DestroyImmediate(root);
        return prefab;
    }

    private static GameObject CreateHighlightPrefab(Material material)
    {
        GameObject highlight = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
        highlight.name = "LegalMoveHighlight";
        highlight.transform.localScale = new Vector3(0.88f, 0.08f, 0.88f);
        highlight.GetComponent<Renderer>().sharedMaterial = material;
        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(highlight, $"{PrefabsPath}/LegalMoveHighlight.prefab");
        Object.DestroyImmediate(highlight);
        return prefab;
    }

    private static GameObject CreateDiePrefab(Material diceMaterial, Material pipMaterial)
    {
        GameObject die = new GameObject("PhysicsDie");
        DiceView view = die.AddComponent<DiceView>();
        view.Initialize(diceMaterial, pipMaterial);

        GameObject prefab = PrefabUtility.SaveAsPrefabAsset(die, $"{PrefabsPath}/PhysicsDie.prefab");
        Object.DestroyImmediate(die);
        return prefab;
    }

    private static void CreateCamera()
    {
        GameObject cameraObject = new GameObject("Player Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.035f, 0.038f, 0.042f);
        camera.transform.position = new Vector3(0.35f, 11.5f, -13.0f);
        camera.transform.LookAt(new Vector3(0.25f, 0f, 0f));
        camera.fieldOfView = 52f;
        cameraObject.tag = "MainCamera";

        CameraRig rig = cameraObject.AddComponent<CameraRig>();
        rig.TargetCamera = camera;
    }

    private static void CreateLights()
    {
        RenderSettings.ambientLight = new Color(0.33f, 0.31f, 0.28f);

        GameObject key = new GameObject("Warm Overhead Key Light");
        Light keyLight = key.AddComponent<Light>();
        keyLight.type = LightType.Directional;
        keyLight.color = new Color(1.0f, 0.88f, 0.68f);
        keyLight.intensity = 1.15f;
        key.transform.rotation = Quaternion.Euler(52f, -34f, 18f);

        GameObject fill = new GameObject("Soft Blue Fill Light");
        Light fillLight = fill.AddComponent<Light>();
        fillLight.type = LightType.Directional;
        fillLight.color = new Color(0.55f, 0.66f, 0.9f);
        fillLight.intensity = 0.45f;
        fill.transform.rotation = Quaternion.Euler(58f, 130f, -20f);
    }

    private static void CreateTable(Material tableMaterial)
    {
        GameObject table = GameObject.CreatePrimitive(PrimitiveType.Cube);
        table.name = "Wide Table Surface";
        table.transform.position = new Vector3(0f, -0.16f, 0f);
        table.transform.localScale = new Vector3(17.5f, 0.18f, 15.0f);
        table.GetComponent<Renderer>().sharedMaterial = tableMaterial;
    }

    private static void CreateBoard(Transform root, Material wood, Material rail, Material innerWood, Material ivoryPoint, Material redPoint, Material brass, Material trayWhite, Material trayBlack)
    {
        CreateCube("Board Base", root, new Vector3(0, 0.02f, 0), new Vector3(BoardLayout.BoardLength, BoardLayout.BoardHeight, BoardLayout.BoardDepth), wood);
        CreateCube("Left Cream Playing Field", root, new Vector3(-2.65f, 0.235f, 0), new Vector3(4.95f, 0.060f, 9.0f), innerWood);
        CreateCube("Right Cream Playing Field", root, new Vector3(2.62f, 0.235f, 0), new Vector3(4.95f, 0.060f, 9.0f), innerWood);
        CreateCube("Bottom Raised Rail", root, new Vector3(0, 0.46f, -5.28f), new Vector3(13.35f, 0.54f, 0.42f), rail);
        CreateCube("Top Raised Rail", root, new Vector3(0, 0.46f, 5.28f), new Vector3(13.35f, 0.54f, 0.42f), rail);
        CreateCube("Left Raised Rail", root, new Vector3(-6.55f, 0.46f, 0), new Vector3(0.42f, 0.54f, 10.5f), rail);
        CreateCube("Right Raised Rail", root, new Vector3(6.55f, 0.46f, 0), new Vector3(0.42f, 0.54f, 10.5f), rail);
        CreateCube("Left Inner Lip", root, new Vector3(-6.08f, 0.52f, 0), new Vector3(0.18f, 0.30f, 9.0f), wood);
        CreateCube("Central Left Wood Strip", root, new Vector3(BoardLayout.CenterBarX - 0.16f, 0.43f, 0), new Vector3(0.20f, 0.38f, 9.1f), rail);
        CreateCube("Central Right Wood Strip", root, new Vector3(BoardLayout.CenterBarX + 0.16f, 0.43f, 0), new Vector3(0.20f, 0.38f, 9.1f), rail);
        CreateCube("Clickable Center Bar", root, new Vector3(BoardLayout.CenterBarX, 0.31f, 0), new Vector3(0.58f, 0.08f, 9.1f), rail).AddComponent<SpecialClickTarget>().Name = "bar";
        CreateCube("Upper Hinge Plate", root, new Vector3(BoardLayout.CenterBarX, 0.65f, 2.10f), new Vector3(0.60f, 0.045f, 0.45f), brass);
        CreateCube("Lower Hinge Plate", root, new Vector3(BoardLayout.CenterBarX, 0.65f, -2.10f), new Vector3(0.60f, 0.045f, 0.45f), brass);
        CreateCube("Lower Brass Latch", root, new Vector3(BoardLayout.CenterBarX, 0.66f, -4.90f), new Vector3(0.70f, 0.05f, 0.13f), brass);
        CreateCube("Upper Brass Latch", root, new Vector3(BoardLayout.CenterBarX, 0.66f, 4.90f), new Vector3(0.70f, 0.05f, 0.13f), brass);

        CreateCube("Right Home Recess Back", root, new Vector3(5.72f, 0.30f, 0f), new Vector3(1.24f, 0.10f, 8.70f), rail);
        CreateCube("Right Home Inner Edge", root, new Vector3(5.08f, 0.46f, 0f), new Vector3(0.10f, 0.22f, 8.70f), wood);
        CreateCube("Right Home Outer Edge", root, new Vector3(6.30f, 0.46f, 0f), new Vector3(0.10f, 0.22f, 8.70f), wood);
        GameObject whiteTray = CreateCube("WHITE HOME - Right Bottom", root, new Vector3(5.72f, 0.39f, -3.25f), new Vector3(1.06f, 0.08f, 2.95f), trayWhite);
        whiteTray.AddComponent<SpecialClickTarget>().Name = "white-off";
        GameObject blackTray = CreateCube("BLACK HOME - Right Top", root, new Vector3(5.72f, 0.39f, 3.25f), new Vector3(1.06f, 0.08f, 2.95f), trayBlack);
        blackTray.AddComponent<SpecialClickTarget>().Name = "black-off";
        CreateCube("Right Home Center Divider", root, new Vector3(5.72f, 0.49f, 0f), new Vector3(1.06f, 0.08f, 0.16f), brass);
        CreateCube("White Home End Stop", root, new Vector3(5.72f, 0.49f, -4.78f), new Vector3(1.06f, 0.08f, 0.10f), wood);
        CreateCube("Black Home End Stop", root, new Vector3(5.72f, 0.49f, 4.78f), new Vector3(1.06f, 0.08f, 0.10f), wood);

        for (int point = 0; point < 24; point++)
        {
            Material material = BoardLayout.PointToColumn(point) % 2 == 0 ? redPoint : ivoryPoint;
            GameObject triangle = CreatePointTriangle($"Point {point + 1}", point, material);
            triangle.transform.SetParent(root, false);
        }

        CreateCube("Lower Inner Shadow Groove", root, new Vector3(0, 0.288f, -4.50f), new Vector3(10.5f, 0.035f, 0.06f), rail);
        CreateCube("Upper Inner Shadow Groove", root, new Vector3(0, 0.288f, 4.50f), new Vector3(10.5f, 0.035f, 0.06f), rail);

        // Invisible walls above rails to contain dice
        CreateInvisibleWall("Bottom Dice Wall", root, new Vector3(0, 1.5f, -5.28f), new Vector3(13.35f, 2.0f, 0.42f));
        CreateInvisibleWall("Top Dice Wall", root, new Vector3(0, 1.5f, 5.28f), new Vector3(13.35f, 2.0f, 0.42f));
        CreateInvisibleWall("Left Dice Wall", root, new Vector3(-6.55f, 1.5f, 0), new Vector3(0.42f, 2.0f, 10.5f));
        CreateInvisibleWall("Right Dice Wall", root, new Vector3(6.55f, 1.5f, 0), new Vector3(0.42f, 2.0f, 10.5f));

        // Invisible flat floor for dice physics; visual triangles do not receive clicks.
        GameObject diceFloor = CreateInvisibleWall("Dice Floor", root,
            new Vector3(0, BoardLayout.BoardHeight + 0.024f, 0),
            new Vector3(11.5f, 0.008f, 9.8f));
        diceFloor.layer = 2; // Ignore Raycast - only affects dice physics, clicks pass through
    }

    private static GameObject CreateCube(string name, Transform parent, Vector3 position, Vector3 scale, Material material)
    {
        GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
        cube.name = name;
        cube.transform.SetParent(parent, false);
        cube.transform.localPosition = position;
        cube.transform.localScale = scale;
        cube.GetComponent<Renderer>().sharedMaterial = material;
        return cube;
    }

    private static GameObject CreateInvisibleWall(string name, Transform parent, Vector3 position, Vector3 scale)
    {
        GameObject wall = GameObject.CreatePrimitive(PrimitiveType.Cube);
        wall.name = name;
        wall.transform.SetParent(parent, false);
        wall.transform.localPosition = position;
        wall.transform.localScale = scale;
        wall.GetComponent<Renderer>().enabled = false;
        return wall;
    }

    private static GameObject CreatePointTriangle(string name, int point, Material material)
    {
        float x1 = BoardLayout.PointLeft(point);
        float x2 = BoardLayout.PointRight(point);
        float xm = x1 + BoardLayout.PointWidth * 0.5f;
        float y = BoardLayout.BoardHeight + 0.025f;

        Vector3 a;
        Vector3 b;
        Vector3 c;
        if (point <= 11)
        {
            a = new Vector3(x1, y, BoardLayout.BottomBaseZ);
            b = new Vector3(x2, y, BoardLayout.BottomBaseZ);
            c = new Vector3(xm, y, -0.25f);
        }
        else
        {
            a = new Vector3(x1, y, BoardLayout.TopBaseZ);
            b = new Vector3(xm, y, 0.25f);
            c = new Vector3(x2, y, BoardLayout.TopBaseZ);
        }

        Mesh mesh = new Mesh();
        mesh.vertices = new[] { a, b, c };
        mesh.uv = new[] { new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 1f) };
        mesh.triangles = new[] { 0, 2, 1 };
        mesh.normals = new[] { Vector3.up, Vector3.up, Vector3.up };
        mesh.RecalculateBounds();

        GameObject triangle = new GameObject(name);
        triangle.AddComponent<MeshFilter>().sharedMesh = mesh;
        triangle.AddComponent<MeshRenderer>().sharedMaterial = material;
        return triangle;
    }

    private static void CreateUi(BackgammonGameController controller, GameObject cameraObject)
    {
        GameObject eventSystem = new GameObject("EventSystem");
        eventSystem.AddComponent<EventSystem>();
        eventSystem.AddComponent<StandaloneInputModule>();

        GameObject canvasObject = new GameObject("Canvas");
        Canvas canvas = canvasObject.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObject.AddComponent<CanvasScaler>().uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
        canvasObject.GetComponent<CanvasScaler>().referenceResolution = new Vector2(1920, 1080);
        canvasObject.AddComponent<GraphicRaycaster>();

        GameObject panel = new GameObject("Top HUD");
        panel.transform.SetParent(canvasObject.transform, false);
        RectTransform panelRect = panel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 1);
        panelRect.anchorMax = new Vector2(1, 1);
        panelRect.pivot = new Vector2(0.5f, 1);
        panelRect.sizeDelta = new Vector2(0, 96);
        panelRect.anchoredPosition = Vector2.zero;
        Image panelImage = panel.AddComponent<Image>();
        panelImage.color = new Color(0.08f, 0.075f, 0.065f, 0.88f);

        Button roll = CreateButton(panel.transform, "Zar At", new Vector2(118, -48));
        Button reset = CreateButton(panel.transform, "Yeni Oyun", new Vector2(278, -48));
        Text status = CreateText(panel.transform, "Beyaz sirada. Zar at.", new Vector2(760, -50));
        Button menuButton = CreateButton(panel.transform, "Ana Menu", new Vector2(450, -48));
        Text matchText = CreateText(panel.transform, "", new Vector2(560, -50));
        matchText.fontStyle = FontStyle.Bold;
        matchText.alignment = TextAnchor.MiddleLeft;
        RectTransform matchRect = matchText.GetComponent<RectTransform>();
        matchRect.sizeDelta = new Vector2(180, 50);

        controller.RollButton = roll;
        controller.NewGameButton = reset;
        controller.MenuButton = menuButton;
        controller.StatusText = status;
        controller.MatchText = matchText;
        controller.HudPanel = panel;
        controller.CameraRig = cameraObject.GetComponent<CameraRig>();

        GameObject menu = new GameObject("Main Menu");
        menu.transform.SetParent(canvasObject.transform, false);
        RectTransform menuRect = menu.AddComponent<RectTransform>();
        menuRect.anchorMin = Vector2.zero;
        menuRect.anchorMax = Vector2.one;
        menuRect.offsetMin = Vector2.zero;
        menuRect.offsetMax = Vector2.zero;
        Image menuImage = menu.AddComponent<Image>();
        menuImage.color = new Color(0.015f, 0.013f, 0.011f, 0.76f);

        Text title = CreateText(menu.transform, "ERKEK TAVLASI", new Vector2(0, 244));
        title.alignment = TextAnchor.MiddleCenter;
        title.fontSize = 62;
        title.fontStyle = FontStyle.Bold;
        title.gameObject.AddComponent<Shadow>().effectColor = new Color(0f, 0f, 0f, 0.75f);
        RectTransform titleRect = title.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 0.5f);
        titleRect.anchorMax = new Vector2(0.5f, 0.5f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(900, 80);

        Text subtitle = CreateText(menu.transform, "Choose your table.", new Vector2(0, 186));
        subtitle.alignment = TextAnchor.MiddleCenter;
        subtitle.fontSize = 24;
        RectTransform subtitleRect = subtitle.GetComponent<RectTransform>();
        subtitleRect.anchorMin = new Vector2(0.5f, 0.5f);
        subtitleRect.anchorMax = new Vector2(0.5f, 0.5f);
        subtitleRect.pivot = new Vector2(0.5f, 0.5f);
        subtitleRect.sizeDelta = new Vector2(900, 52);

        Button local3D;
        Button local2D;
        Button ai3D;
        Button ai2D;
        Button bo53D;
        Button bo52D;
        CreateModePanel(menu.transform, "Normal Play", "Classic two-player tavla.", new Vector2(-430, 12), out local3D, out local2D);
        CreateModePanel(menu.transform, "Play vs AI", "Practice against the current AI.", new Vector2(0, 12), out ai3D, out ai2D);
        CreateModePanel(menu.transform, "Play BO5", "Best of five with mars scoring.", new Vector2(430, 12), out bo53D, out bo52D);

        GameObject trainingPanel = CreateMenuPanel(menu.transform, "AI Training Panel", new Vector2(-205, -246), new Vector2(520, 124));
        Text trainTitle = CreateText(trainingPanel.transform, "AI Training", new Vector2(24, -28));
        trainTitle.fontSize = 24;
        trainTitle.fontStyle = FontStyle.Bold;
        RectTransform trainTitleRect = trainTitle.GetComponent<RectTransform>();
        trainTitleRect.sizeDelta = new Vector2(220, 36);
        InputField trainMinutes = CreateInputField(trainingPanel.transform, "30", new Vector2(30, -82));
        Button trainAi = CreateButton(trainingPanel.transform, "Train AI", new Vector2(320, -82));
        trainAi.GetComponent<RectTransform>().sizeDelta = new Vector2(190, 52);

        Button exit = CreateButton(menu.transform, "Exit", new Vector2(310, -246));
        ConfigureMenuButton(exit, new Vector2(230, 72));

        Text menuInfo = CreateText(menu.transform, "Hazir.", new Vector2(0, -346));
        menuInfo.alignment = TextAnchor.MiddleCenter;
        menuInfo.fontSize = 22;
        RectTransform menuInfoRect = menuInfo.GetComponent<RectTransform>();
        menuInfoRect.anchorMin = new Vector2(0.5f, 0.5f);
        menuInfoRect.anchorMax = new Vector2(0.5f, 0.5f);
        menuInfoRect.pivot = new Vector2(0.5f, 0.5f);
        menuInfoRect.sizeDelta = new Vector2(900, 48);

        controller.Local3DButton = local3D;
        controller.Local2DButton = local2D;
        controller.PlayVsAi3DButton = ai3D;
        controller.PlayVsAi2DButton = ai2D;
        controller.Bo53DButton = bo53D;
        controller.Bo52DButton = bo52D;
        controller.TrainAiButton = trainAi;
        controller.ExitButton = exit;
        controller.TrainMinutesInput = trainMinutes;
        controller.MenuInfoText = menuInfo;
        controller.MainMenuPanel = menu;
    }

    private static GameObject CreateMenuPanel(Transform parent, string name, Vector2 position, Vector2 size)
    {
        GameObject panel = new GameObject(name);
        panel.transform.SetParent(parent, false);
        RectTransform rect = panel.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        Image image = panel.AddComponent<Image>();
        image.color = new Color(0.10f, 0.075f, 0.050f, 0.82f);
        Outline outline = panel.AddComponent<Outline>();
        outline.effectColor = new Color(0.86f, 0.56f, 0.18f, 0.34f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return panel;
    }

    private static void CreateModePanel(Transform parent, string title, string subtitle, Vector2 position, out Button threeD, out Button twoD)
    {
        GameObject panel = CreateMenuPanel(parent, title + " Panel", position, new Vector2(360, 230));

        Text titleText = CreateText(panel.transform, title, new Vector2(0, -36));
        titleText.alignment = TextAnchor.MiddleCenter;
        titleText.fontSize = 28;
        titleText.fontStyle = FontStyle.Bold;
        RectTransform titleRect = titleText.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.pivot = new Vector2(0.5f, 0.5f);
        titleRect.sizeDelta = new Vector2(310, 40);

        Text subText = CreateText(panel.transform, subtitle, new Vector2(0, -76));
        subText.alignment = TextAnchor.MiddleCenter;
        subText.fontSize = 17;
        subText.color = new Color(0.82f, 0.78f, 0.68f, 1f);
        RectTransform subRect = subText.GetComponent<RectTransform>();
        subRect.anchorMin = new Vector2(0.5f, 1f);
        subRect.anchorMax = new Vector2(0.5f, 1f);
        subRect.pivot = new Vector2(0.5f, 0.5f);
        subRect.sizeDelta = new Vector2(310, 34);

        threeD = CreateButton(panel.transform, "Play 3B", new Vector2(-82, -78));
        twoD = CreateButton(panel.transform, "Play 2B", new Vector2(82, -78));
        ConfigureMenuButton(threeD, new Vector2(142, 52));
        ConfigureMenuButton(twoD, new Vector2(142, 52));
    }

    private static InputField CreateInputField(Transform parent, string value, Vector2 position)
    {
        GameObject inputObject = new GameObject("Training Minutes Input");
        inputObject.transform.SetParent(parent, false);
        RectTransform rect = inputObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(250, 52);
        rect.anchoredPosition = position;

        Image image = inputObject.AddComponent<Image>();
        image.color = new Color(0.045f, 0.038f, 0.030f, 0.96f);
        Outline outline = inputObject.AddComponent<Outline>();
        outline.effectColor = new Color(0.85f, 0.56f, 0.20f, 0.55f);
        outline.effectDistance = new Vector2(1f, -1f);

        Text text = CreateText(inputObject.transform, value, Vector2.zero);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontSize = 24;
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(12, 0);
        textRect.offsetMax = new Vector2(-12, 0);
        textRect.anchoredPosition = Vector2.zero;

        Text placeholder = CreateText(inputObject.transform, "minutes", Vector2.zero);
        placeholder.alignment = TextAnchor.MiddleCenter;
        placeholder.fontSize = 20;
        placeholder.color = new Color(0.56f, 0.52f, 0.45f, 1f);
        RectTransform placeholderRect = placeholder.GetComponent<RectTransform>();
        placeholderRect.anchorMin = Vector2.zero;
        placeholderRect.anchorMax = Vector2.one;
        placeholderRect.offsetMin = new Vector2(12, 0);
        placeholderRect.offsetMax = new Vector2(-12, 0);
        placeholderRect.anchoredPosition = Vector2.zero;

        InputField input = inputObject.AddComponent<InputField>();
        input.textComponent = text;
        input.placeholder = placeholder;
        input.text = value;
        input.contentType = InputField.ContentType.IntegerNumber;
        input.characterLimit = 4;
        return input;
    }

    private static void ConfigureMenuButton(Button button, Vector2 size)
    {
        RectTransform rect = button.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = size;
    }

    private static void ConfigureMenuLabel(Text label)
    {
        label.alignment = TextAnchor.MiddleRight;
        label.fontStyle = FontStyle.Bold;
        label.fontSize = 28;
        RectTransform rect = label.GetComponent<RectTransform>();
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(1f, 0.5f);
        rect.sizeDelta = new Vector2(260, 56);
    }

    private static Button CreateButton(Transform parent, string label, Vector2 position)
    {
        GameObject buttonObject = new GameObject(label);
        buttonObject.transform.SetParent(parent, false);
        RectTransform rect = buttonObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.sizeDelta = new Vector2(136, 48);
        rect.anchoredPosition = position;

        Image image = buttonObject.AddComponent<Image>();
        image.color = new Color(0.95f, 0.72f, 0.24f, 1f);
        Outline outline = buttonObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.45f);
        outline.effectDistance = new Vector2(1.2f, -1.2f);
        Button button = buttonObject.AddComponent<Button>();
        ColorBlock colors = button.colors;
        colors.normalColor = new Color(0.95f, 0.72f, 0.24f, 1f);
        colors.highlightedColor = new Color(1.0f, 0.84f, 0.36f, 1f);
        colors.pressedColor = new Color(0.76f, 0.48f, 0.13f, 1f);
        colors.selectedColor = colors.highlightedColor;
        colors.disabledColor = new Color(0.36f, 0.30f, 0.22f, 0.70f);
        colors.colorMultiplier = 1f;
        colors.fadeDuration = 0.08f;
        button.colors = colors;

        Text text = CreateText(buttonObject.transform, label, Vector2.zero);
        text.alignment = TextAnchor.MiddleCenter;
        text.fontStyle = FontStyle.Bold;
        text.color = new Color(0.08f, 0.06f, 0.03f, 1f);
        text.gameObject.AddComponent<Shadow>().effectColor = new Color(1f, 1f, 1f, 0.10f);
        RectTransform textRect = text.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.anchoredPosition = Vector2.zero;
        return button;
    }

    private static Text CreateText(Transform parent, string value, Vector2 position)
    {
        GameObject textObject = new GameObject("Text");
        textObject.transform.SetParent(parent, false);
        RectTransform rect = textObject.AddComponent<RectTransform>();
        rect.anchorMin = new Vector2(0, 1);
        rect.anchorMax = new Vector2(0, 1);
        rect.pivot = new Vector2(0, 0.5f);
        rect.sizeDelta = new Vector2(900, 50);
        rect.anchoredPosition = position;

        Text text = textObject.AddComponent<Text>();
        text.text = value;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 25;
        text.color = new Color(0.96f, 0.94f, 0.87f, 1f);
        text.alignment = TextAnchor.MiddleLeft;
        return text;
    }
}
