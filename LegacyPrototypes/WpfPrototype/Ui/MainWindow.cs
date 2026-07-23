using ErkekTavlasi.Game;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Threading;

namespace ErkekTavlasi.Ui;

public class MainWindow : Window
{
    private const double BoardTop = 0.14;
    private const double PlayLeft = -5.7;
    private const double ColumnWidth = 0.9;
    private const double BottomBaseZ = 3.6;
    private const double TopBaseZ = -3.6;
    private const double BottomTipZ = 0.35;
    private const double TopTipZ = -0.35;

    private readonly GameState game;
    private readonly Random random;
    private readonly Viewport3D viewport;
    private readonly Model3DGroup scene;
    private readonly PerspectiveCamera camera;
    private readonly Button rollButton;
    private readonly Button newGameButton;
    private readonly TextBlock statusText;
    private readonly DispatcherTimer frameTimer;
    private readonly Dictionary<Model3D, int> pointHits;
    private readonly Dictionary<Model3D, string> specialHits;

    private int selectedPoint;
    private int dieA;
    private int dieB;
    private bool diceRolling;
    private double diceTime;
    private MoveAnimation moveAnimation;
    private Queue<BackgammonMove> queuedMoves;

    public MainWindow()
    {
        game = new GameState();
        random = new Random();
        scene = new Model3DGroup();
        pointHits = new Dictionary<Model3D, int>();
        specialHits = new Dictionary<Model3D, string>();
        selectedPoint = int.MinValue;
        dieA = 1;
        dieB = 1;
        queuedMoves = new Queue<BackgammonMove>();

        Title = "Erkek Tavlasi - 3D C#";
        Width = 1200;
        Height = 800;
        MinWidth = 980;
        MinHeight = 660;
        Background = new SolidColorBrush(Color.FromRgb(22, 24, 27));

        DockPanel layout = new DockPanel();
        Content = layout;

        Border topBar = CreateTopBar();
        DockPanel.SetDock(topBar, Dock.Top);
        layout.Children.Add(topBar);

        viewport = new Viewport3D();
        viewport.ClipToBounds = true;
        viewport.MouseDown += Viewport_MouseDown;
        layout.Children.Add(viewport);

        camera = new PerspectiveCamera();
        camera.Position = new Point3D(0.55, 9.3, 12.3);
        camera.LookDirection = new Vector3D(0, -8.1, -12.3);
        camera.UpDirection = new Vector3D(0, 1, 0);
        camera.FieldOfView = 50;
        viewport.Camera = camera;

        ModelVisual3D visual = new ModelVisual3D();
        visual.Content = scene;
        viewport.Children.Add(visual);

        rollButton = CreateButton("Zar At");
        rollButton.Click += RollButton_Click;
        newGameButton = CreateButton("Yeni Oyun");
        newGameButton.Click += NewGameButton_Click;
        statusText = CreateStatusText();

        StackPanel actions = (StackPanel)topBar.Child;
        actions.Children.Add(rollButton);
        actions.Children.Add(newGameButton);
        actions.Children.Add(statusText);

        frameTimer = new DispatcherTimer();
        frameTimer.Interval = TimeSpan.FromMilliseconds(16);
        frameTimer.Tick += FrameTimer_Tick;
        frameTimer.Start();

        UpdateStatus();
        BuildScene();
    }

    private Border CreateTopBar()
    {
        Border border = new Border();
        border.Background = new SolidColorBrush(Color.FromRgb(244, 240, 229));
        border.BorderBrush = new SolidColorBrush(Color.FromRgb(36, 35, 32));
        border.BorderThickness = new Thickness(0, 0, 0, 2);
        border.Padding = new Thickness(16, 12, 16, 12);

        StackPanel panel = new StackPanel();
        panel.Orientation = Orientation.Horizontal;
        panel.VerticalAlignment = VerticalAlignment.Center;
        border.Child = panel;

        return border;
    }

    private Button CreateButton(string text)
    {
        Button button = new Button();
        button.Content = text;
        button.MinWidth = 118;
        button.Height = 38;
        button.Margin = new Thickness(0, 0, 12, 0);
        button.Padding = new Thickness(16, 4, 16, 4);
        button.FontSize = 15;
        button.FontWeight = FontWeights.SemiBold;
        button.Foreground = new SolidColorBrush(Color.FromRgb(18, 20, 23));
        button.Background = new SolidColorBrush(Color.FromRgb(255, 198, 70));
        button.BorderBrush = new SolidColorBrush(Color.FromRgb(93, 67, 20));
        button.BorderThickness = new Thickness(2);
        return button;
    }

    private TextBlock CreateStatusText()
    {
        TextBlock text = new TextBlock();
        text.VerticalAlignment = VerticalAlignment.Center;
        text.Margin = new Thickness(8, 0, 0, 0);
        text.FontSize = 16;
        text.Foreground = new SolidColorBrush(Color.FromRgb(31, 34, 38));
        text.TextWrapping = TextWrapping.Wrap;
        return text;
    }

    private void RollButton_Click(object sender, RoutedEventArgs e)
    {
        if (diceRolling || moveAnimation != null || game.DiceRemaining.Count > 0)
        {
            return;
        }

        diceRolling = true;
        diceTime = 0;
        selectedPoint = int.MinValue;
        rollButton.IsEnabled = false;
        UpdateStatus("Zarlar tahtada sekiyor...");
    }

    private void NewGameButton_Click(object sender, RoutedEventArgs e)
    {
        game.StartNewGame();
        selectedPoint = int.MinValue;
        diceRolling = false;
        diceTime = 0;
        moveAnimation = null;
        queuedMoves.Clear();
        dieA = 1;
        dieB = 1;
        rollButton.IsEnabled = true;
        UpdateStatus();
        BuildScene();
    }

    private void FrameTimer_Tick(object sender, EventArgs e)
    {
        bool needsRedraw = false;

        if (diceRolling)
        {
            diceTime += 0.016;
            dieA = random.Next(1, 7);
            dieB = random.Next(1, 7);
            needsRedraw = true;

            if (diceTime >= 1.25)
            {
                FinishDiceRoll();
            }
        }

        if (moveAnimation != null)
        {
            moveAnimation.Progress += 0.045;
            needsRedraw = true;

            if (moveAnimation.Progress >= 1)
            {
                FinishMoveAnimation();
            }
        }

        if (needsRedraw)
        {
            BuildScene();
        }
    }

    private void FinishDiceRoll()
    {
        diceRolling = false;
        game.RollDice(random);
        dieA = game.DiceRemaining[0];
        dieB = game.DiceRemaining.Count > 1 ? game.DiceRemaining[1] : dieA;

        if (game.GetLegalMoves().Count == 0)
        {
            game.EndTurn();
            rollButton.IsEnabled = true;
            UpdateStatus("Hamle yok. Sira degisti, " + GetPlayerName(game.CurrentPlayer) + " zar atacak.");
        }
        else
        {
            UpdateStatus();
        }
    }

    private void FinishMoveAnimation()
    {
        BackgammonMove move = moveAnimation.Move;
        moveAnimation = null;
        game.ApplyMove(move);

        if (queuedMoves.Count > 0 && !game.IsGameOver())
        {
            StartMoveAnimation(queuedMoves.Dequeue());
            return;
        }

        if (game.IsGameOver())
        {
            rollButton.IsEnabled = false;
            UpdateStatus(GetPlayerName(game.GetWinner()) + " kazandi!");
            return;
        }

        if (game.DiceRemaining.Count == 0 || game.GetLegalMoves().Count == 0)
        {
            game.EndTurn();
            selectedPoint = int.MinValue;
            rollButton.IsEnabled = true;
            UpdateStatus("Sira degisti. " + GetPlayerName(game.CurrentPlayer) + " zar atacak.");
        }
        else
        {
            selectedPoint = int.MinValue;
            UpdateStatus();
        }
    }

    private void Viewport_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (diceRolling || moveAnimation != null || game.IsGameOver())
        {
            return;
        }

        if (game.DiceRemaining.Count == 0)
        {
            UpdateStatus("Once zar atmalisin.");
            return;
        }

        HitTestResult result = VisualTreeHelper.HitTest(viewport, e.GetPosition(viewport));
        RayMeshGeometry3DHitTestResult ray = result as RayMeshGeometry3DHitTestResult;
        if (ray == null)
        {
            selectedPoint = int.MinValue;
            UpdateStatus();
            BuildScene();
            return;
        }

        Model3D hit = ray.ModelHit;
        if (specialHits.ContainsKey(hit))
        {
            HandleSpecialClick(specialHits[hit]);
            return;
        }

        if (pointHits.ContainsKey(hit))
        {
            int point = pointHits[hit];
            HandlePointClick(point);
        }
    }

    private void HandleSpecialClick(string name)
    {
        if (name == "bar")
        {
            TrySelectPoint(BackgammonMove.FromBar);
            return;
        }

        if (name == "off" && selectedPoint != int.MinValue)
        {
            TryBearOff();
        }
    }

    private void HandlePointClick(int point)
    {
        if (selectedPoint == int.MinValue)
        {
            TrySelectPoint(point);
            return;
        }

        List<MoveOption> options = GetMoveOptionsForSelection();
        for (int i = 0; i < options.Count; i++)
        {
            if (!options[i].BearsOff && options[i].TargetPoint == point)
            {
                StartMoveSequence(options[i]);
                return;
            }
        }

        selectedPoint = int.MinValue;
        UpdateStatus("Bu hedef mevcut zarlarla yasal degil.");
        BuildScene();
    }

    private void TrySelectPoint(int point)
    {
        if (game.GetBarCount(game.CurrentPlayer) > 0 && point != BackgammonMove.FromBar)
        {
            UpdateStatus("Kirik tas varken once bardan giris yapmalisin.");
            return;
        }

        if (point == BackgammonMove.FromBar)
        {
            if (game.GetBarCount(game.CurrentPlayer) == 0)
            {
                UpdateStatus("Barda tasin yok.");
                return;
            }

            selectedPoint = point;
            UpdateStatus("Bar secildi. Yesil isaretli giris hanesine tikla.");
            BuildScene();
            return;
        }

        PointStack stack = game.Points[point];
        if (stack.Owner != game.CurrentPlayer || stack.Count == 0)
        {
            UpdateStatus("Kendi tasini secmelisin.");
            return;
        }

        if (game.GetLegalMovesFrom(point).Count == 0)
        {
            UpdateStatus("Bu tas mevcut zarlarla oynayamiyor.");
            return;
        }

        selectedPoint = point;
        UpdateStatus("Tas secildi. Yesil isaretli alanlardan birine tikla.");
        BuildScene();
    }

    private void TryBearOff()
    {
        List<MoveOption> options = GetMoveOptionsForSelection();
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].BearsOff)
            {
                StartMoveSequence(options[i]);
                return;
            }
        }

        selectedPoint = int.MinValue;
        UpdateStatus("Bu tas su anda toplanamaz.");
        BuildScene();
    }

    private void StartMoveSequence(MoveOption option)
    {
        queuedMoves.Clear();
        for (int i = 1; i < option.Steps.Count; i++)
        {
            queuedMoves.Enqueue(option.Steps[i]);
        }

        StartMoveAnimation(option.Steps[0]);
    }

    private void StartMoveAnimation(BackgammonMove move)
    {
        moveAnimation = new MoveAnimation();
        moveAnimation.Move = move;
        moveAnimation.Start = GetMovingCheckerCenter(move.FromPoint, game.CurrentPlayer);
        moveAnimation.End = GetMoveEndCenter(move);
        moveAnimation.Color = game.CurrentPlayer;
        moveAnimation.Progress = 0;
        selectedPoint = int.MinValue;
        BuildScene();
    }

    private void BuildScene()
    {
        scene.Children.Clear();
        pointHits.Clear();
        specialHits.Clear();

        AddLights();
        AddRoom();
        AddBoard();
        AddPointsAndHighlights();
        AddCheckers();
        AddBarAndOffCheckers();
        AddDice();
        AddMovingChecker();
    }

    private void AddLights()
    {
        scene.Children.Add(new AmbientLight(Color.FromRgb(92, 86, 78)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(255, 244, 219), new Vector3D(-0.35, -1.0, -0.45)));
        scene.Children.Add(new DirectionalLight(Color.FromRgb(118, 145, 180), new Vector3D(0.8, -0.65, 0.5)));
    }

    private void AddRoom()
    {
        scene.Children.Add(CreateBox(new Point3D(0, -0.11, 0), new Vector3D(22, 0.08, 15), Material(Color.FromRgb(34, 37, 39))));
    }

    private void AddBoard()
    {
        Material darkWood = ShinyMaterial(Color.FromRgb(88, 47, 24), Color.FromRgb(170, 118, 70), 22);
        Material felt = Material(Color.FromRgb(62, 97, 84));
        Material railWood = ShinyMaterial(Color.FromRgb(66, 34, 18), Color.FromRgb(150, 92, 48), 24);

        scene.Children.Add(CreateBox(new Point3D(0, 0, 0), new Vector3D(13.4, 0.28, 8.4), darkWood));
        scene.Children.Add(CreateBox(new Point3D(-0.25, BoardTop + 0.01, 0), new Vector3D(11.8, 0.05, 7.15), felt));

        scene.Children.Add(CreateBox(new Point3D(0, 0.33, -4.05), new Vector3D(13.4, 0.58, 0.32), railWood));
        scene.Children.Add(CreateBox(new Point3D(0, 0.33, 4.05), new Vector3D(13.4, 0.58, 0.32), railWood));
        scene.Children.Add(CreateBox(new Point3D(-6.55, 0.33, 0), new Vector3D(0.35, 0.58, 8.4), railWood));
        scene.Children.Add(CreateBox(new Point3D(6.55, 0.33, 0), new Vector3D(0.35, 0.58, 8.4), railWood));
        scene.Children.Add(CreateBox(new Point3D(-0.25, 0.22, 0), new Vector3D(0.08, 0.12, 7.15), ShinyMaterial(Color.FromRgb(116, 72, 38), Color.FromRgb(226, 164, 92), 16)));
        scene.Children.Add(CreateBox(new Point3D(-0.25, 0.245, 0), new Vector3D(11.7, 0.04, 0.08), Material(Color.FromRgb(34, 55, 50))));

        GeometryModel3D bar = CreateBox(new Point3D(5.55, 0.36, 0), new Vector3D(0.55, 0.62, 7.2), railWood);
        scene.Children.Add(bar);
        specialHits[bar] = "bar";

        GeometryModel3D off = CreateBox(new Point3D(7.45, 0.24, 0), new Vector3D(1.15, 0.48, 7.2), ShinyMaterial(Color.FromRgb(48, 52, 55), Color.FromRgb(125, 130, 132), 16));
        scene.Children.Add(off);
        specialHits[off] = "off";
    }

    private void AddPointsAndHighlights()
    {
        HashSet<int> targetPoints = GetTargetPointsForSelection();
        bool canBearOff = CanSelectedBearOff();

        for (int point = 0; point < 24; point++)
        {
            Color color = PointToColumn(point) % 2 == 0
                ? Color.FromRgb(36, 65, 76)
                : Color.FromRgb(229, 218, 191);

            GeometryModel3D triangle = CreatePointTriangle(point, Material(color), BoardTop + 0.035);
            scene.Children.Add(triangle);
            pointHits[triangle] = point;

            if (targetPoints.Contains(point))
            {
                GeometryModel3D highlight = CreatePointTriangle(point, TransparentMaterial(Color.FromArgb(155, 56, 238, 142)), BoardTop + 0.065);
                scene.Children.Add(highlight);
                pointHits[highlight] = point;
            }
        }

        if (selectedPoint != int.MinValue)
        {
            AddSelectionMarker();
        }

        if (canBearOff)
        {
            GeometryModel3D offGlow = CreateBox(new Point3D(7.45, 0.52, 0), new Vector3D(1.22, 0.05, 7.25), TransparentMaterial(Color.FromArgb(125, 76, 230, 145)));
            scene.Children.Add(offGlow);
            specialHits[offGlow] = "off";
        }
    }

    private void AddSelectionMarker()
    {
        Point3D center = selectedPoint == BackgammonMove.FromBar
            ? GetBarCheckerCenter(game.CurrentPlayer, game.GetBarCount(game.CurrentPlayer))
            : GetMovingCheckerCenter(selectedPoint, game.CurrentPlayer);

        scene.Children.Add(CreateCylinder(new Point3D(center.X, 0.17, center.Z), 0.47, 0.035, 42, TransparentMaterial(Color.FromArgb(185, 255, 207, 58))));
    }

    private void AddCheckers()
    {
        for (int point = 0; point < 24; point++)
        {
            PointStack stack = game.Points[point];
            for (int index = 0; index < stack.Count; index++)
            {
                if (IsAnimatedChecker(point, index))
                {
                    continue;
                }

                Point3D center = GetCheckerCenter(point, index);
                GeometryModel3D checker = CreateChecker(center, stack.Owner);
                scene.Children.Add(checker);
                pointHits[checker] = point;
            }
        }
    }

    private void AddBarAndOffCheckers()
    {
        AddSmallStack(PlayerColor.Black, game.BlackBar, new Point3D(5.55, 0.24, -1.45), "bar");
        AddSmallStack(PlayerColor.White, game.WhiteBar, new Point3D(5.55, 0.24, 1.45), "bar");
        AddSmallStack(PlayerColor.Black, game.BlackOff, new Point3D(7.45, 0.24, -1.75), "off");
        AddSmallStack(PlayerColor.White, game.WhiteOff, new Point3D(7.45, 0.24, 1.75), "off");
    }

    private void AddSmallStack(PlayerColor color, int count, Point3D start, string hitName)
    {
        int shown = Math.Min(count, 5);
        for (int i = 0; i < shown; i++)
        {
            Point3D center = new Point3D(start.X, start.Y + i * 0.18, start.Z);
            GeometryModel3D checker = CreateChecker(center, color);
            scene.Children.Add(checker);
            specialHits[checker] = hitName;
        }
    }

    private void AddDice()
    {
        if (diceRolling)
        {
            double bounce = Math.Abs(Math.Sin(diceTime * 13.0));
            double slide = Math.Sin(diceTime * 5.0) * 0.65;

            scene.Children.Add(CreateDie(new Point3D(-0.75 + slide, 0.56 + bounce * 0.9, -0.32), dieA, diceTime * 720, diceTime * 520));
            scene.Children.Add(CreateDie(new Point3D(0.8 - slide * 0.5, 0.56 + Math.Abs(Math.Sin(diceTime * 11.0)) * 0.8, 0.38), dieB, diceTime * 610, diceTime * 800));
            return;
        }

        scene.Children.Add(CreateDie(new Point3D(-0.65, 0.50, 0.0), dieA, -8, 12));
        scene.Children.Add(CreateDie(new Point3D(0.25, 0.50, 0.0), dieB, 6, -9));
    }

    private void AddMovingChecker()
    {
        if (moveAnimation == null)
        {
            return;
        }

        double t = Math.Min(1, moveAnimation.Progress);
        double eased = 1 - Math.Pow(1 - t, 2);
        double x = moveAnimation.Start.X + (moveAnimation.End.X - moveAnimation.Start.X) * eased;
        double z = moveAnimation.Start.Z + (moveAnimation.End.Z - moveAnimation.Start.Z) * eased;
        double y = moveAnimation.Start.Y + (moveAnimation.End.Y - moveAnimation.Start.Y) * eased + Math.Sin(t * Math.PI) * 0.9;

        scene.Children.Add(CreateChecker(new Point3D(x, y, z), moveAnimation.Color));

        if (moveAnimation.Move.HitsOpponent && t > 0.55)
        {
            scene.Children.Add(CreateCylinder(new Point3D(moveAnimation.End.X, 0.2, moveAnimation.End.Z), 0.55, 0.035, 40, TransparentMaterial(Color.FromArgb(170, 255, 72, 72))));
        }
    }

    private HashSet<int> GetTargetPointsForSelection()
    {
        HashSet<int> targets = new HashSet<int>();
        if (selectedPoint == int.MinValue)
        {
            return targets;
        }

        List<MoveOption> options = GetMoveOptionsForSelection();
        for (int i = 0; i < options.Count; i++)
        {
            if (!options[i].BearsOff)
            {
                targets.Add(options[i].TargetPoint);
            }
        }

        return targets;
    }

    private bool CanSelectedBearOff()
    {
        if (selectedPoint == int.MinValue)
        {
            return false;
        }

        List<MoveOption> options = GetMoveOptionsForSelection();
        for (int i = 0; i < options.Count; i++)
        {
            if (options[i].BearsOff)
            {
                return true;
            }
        }

        return false;
    }

    private List<MoveOption> GetMoveOptionsForSelection()
    {
        List<MoveOption> options = new List<MoveOption>();
        if (selectedPoint == int.MinValue)
        {
            return options;
        }

        BuildMoveOptions(game.Clone(), selectedPoint, new List<BackgammonMove>(), options);
        return PreferLongestOptions(options);
    }

    private void BuildMoveOptions(GameState state, int fromPoint, List<BackgammonMove> steps, List<MoveOption> options)
    {
        List<BackgammonMove> moves = state.GetLegalMovesFrom(fromPoint);
        for (int i = 0; i < moves.Count; i++)
        {
            BackgammonMove move = CopyMove(moves[i]);
            GameState nextState = state.Clone();
            nextState.ApplyMove(move);

            List<BackgammonMove> nextSteps = new List<BackgammonMove>(steps);
            nextSteps.Add(move);

            MoveOption option = new MoveOption();
            option.TargetPoint = move.ToPoint;
            option.BearsOff = move.BearsOff;
            option.Steps = nextSteps;
            options.Add(option);

            if (!move.BearsOff && nextState.DiceRemaining.Count > 0)
            {
                BuildMoveOptions(nextState, move.ToPoint, nextSteps, options);
            }
        }
    }

    private List<MoveOption> PreferLongestOptions(List<MoveOption> options)
    {
        Dictionary<string, MoveOption> best = new Dictionary<string, MoveOption>();
        for (int i = 0; i < options.Count; i++)
        {
            string key = options[i].BearsOff ? "off" : options[i].TargetPoint.ToString();
            if (!best.ContainsKey(key) || options[i].Steps.Count > best[key].Steps.Count)
            {
                best[key] = options[i];
            }
        }

        return best.Values.ToList();
    }

    private BackgammonMove CopyMove(BackgammonMove move)
    {
        return new BackgammonMove
        {
            FromPoint = move.FromPoint,
            ToPoint = move.ToPoint,
            Die = move.Die,
            HitsOpponent = move.HitsOpponent,
            BearsOff = move.BearsOff
        };
    }

    private Point3D GetCheckerCenter(int point, int index)
    {
        double x = GetPointX(point);
        bool bottom = point <= 11;
        int rowIndex = index % 5;
        int layer = index / 5;
        double z = bottom ? BottomBaseZ - 0.46 - rowIndex * 0.56 : TopBaseZ + 0.46 + rowIndex * 0.56;
        double y = 0.24 + layer * 0.2;
        return new Point3D(x, y, z);
    }

    private Point3D GetMovingCheckerCenter(int point, PlayerColor color)
    {
        if (point == BackgammonMove.FromBar)
        {
            return GetBarCheckerCenter(color, game.GetBarCount(color));
        }

        int index = Math.Max(0, game.Points[point].Count - 1);
        return GetCheckerCenter(point, index);
    }

    private Point3D GetMoveEndCenter(BackgammonMove move)
    {
        if (move.BearsOff)
        {
            double z = game.CurrentPlayer == PlayerColor.White ? 1.75 : -1.75;
            int count = game.GetOffCount(game.CurrentPlayer);
            return new Point3D(7.45, 0.25 + Math.Min(count, 4) * 0.18, z);
        }

        int index = game.Points[move.ToPoint].Count;
        if (move.HitsOpponent)
        {
            index = 0;
        }

        return GetCheckerCenter(move.ToPoint, index);
    }

    private Point3D GetBarCheckerCenter(PlayerColor color, int count)
    {
        double z = color == PlayerColor.White ? 1.45 : -1.45;
        return new Point3D(5.55, 0.25 + Math.Max(0, Math.Min(count - 1, 4)) * 0.18, z);
    }

    private bool IsAnimatedChecker(int point, int index)
    {
        if (moveAnimation == null)
        {
            return false;
        }

        if (moveAnimation.Move.FromPoint != point)
        {
            return false;
        }

        return index == game.Points[point].Count - 1;
    }

    private GeometryModel3D CreateChecker(Point3D center, PlayerColor color)
    {
        Material checkerMaterial = color == PlayerColor.White
            ? ShinyMaterial(Color.FromRgb(237, 234, 220), Color.FromRgb(255, 255, 255), 48)
            : ShinyMaterial(Color.FromRgb(24, 27, 31), Color.FromRgb(106, 112, 120), 42);

        GeometryModel3D checker = CreateCylinder(center, 0.35, 0.19, 56, checkerMaterial);
        checker.BackMaterial = checker.Material;
        return checker;
    }

    private Model3DGroup CreateDie(Point3D center, int value, double xAngle, double zAngle)
    {
        Model3DGroup group = new Model3DGroup();
        group.Children.Add(CreateBox(new Point3D(0, 0, 0), new Vector3D(0.68, 0.68, 0.68), ShinyMaterial(Color.FromRgb(244, 241, 229), Color.FromRgb(255, 255, 255), 34)));

        AddDieFace(group, value, new Vector3D(0, 1, 0), new Vector3D(1, 0, 0), new Vector3D(0, 0, 1));
        AddDieFace(group, 7 - value, new Vector3D(0, -1, 0), new Vector3D(1, 0, 0), new Vector3D(0, 0, -1));
        AddDieFace(group, 2, new Vector3D(1, 0, 0), new Vector3D(0, 0, 1), new Vector3D(0, 1, 0));
        AddDieFace(group, 5, new Vector3D(-1, 0, 0), new Vector3D(0, 0, -1), new Vector3D(0, 1, 0));
        AddDieFace(group, 3, new Vector3D(0, 0, 1), new Vector3D(1, 0, 0), new Vector3D(0, 1, 0));
        AddDieFace(group, 4, new Vector3D(0, 0, -1), new Vector3D(-1, 0, 0), new Vector3D(0, 1, 0));

        Transform3DGroup transform = new Transform3DGroup();
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), xAngle)));
        transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), zAngle)));
        transform.Children.Add(new TranslateTransform3D(center.X, center.Y, center.Z));
        group.Transform = transform;
        return group;
    }

    private void AddDieFace(Model3DGroup group, int value, Vector3D normal, Vector3D right, Vector3D up)
    {
        double face = 0.348;
        double a = 0.18;
        List<Point> spots = GetPipSpots(value);

        for (int i = 0; i < spots.Count; i++)
        {
            Vector3D offset = normal * face + right * (spots[i].X * a) + up * (spots[i].Y * a);
            Point3D pipCenter = new Point3D(offset.X, offset.Y, offset.Z);
            group.Children.Add(CreatePip(pipCenter, normal));
        }
    }

    private List<Point> GetPipSpots(int value)
    {
        List<Point> spots = new List<Point>();

        if (value == 1) spots.Add(new Point(0, 0));
        if (value == 2) { spots.Add(new Point(-1, -1)); spots.Add(new Point(1, 1)); }
        if (value == 3) { spots.Add(new Point(-1, -1)); spots.Add(new Point(0, 0)); spots.Add(new Point(1, 1)); }
        if (value == 4) { spots.Add(new Point(-1, -1)); spots.Add(new Point(1, -1)); spots.Add(new Point(-1, 1)); spots.Add(new Point(1, 1)); }
        if (value == 5) { spots.Add(new Point(-1, -1)); spots.Add(new Point(1, -1)); spots.Add(new Point(0, 0)); spots.Add(new Point(-1, 1)); spots.Add(new Point(1, 1)); }
        if (value == 6) { spots.Add(new Point(-1, -1)); spots.Add(new Point(1, -1)); spots.Add(new Point(-1, 0)); spots.Add(new Point(1, 0)); spots.Add(new Point(-1, 1)); spots.Add(new Point(1, 1)); }

        return spots;
    }

    private GeometryModel3D CreatePip(Point3D center, Vector3D normal)
    {
        GeometryModel3D pip = CreateCylinder(new Point3D(0, 0, 0), 0.052, 0.018, 18, Material(Color.FromRgb(22, 22, 22)));
        Transform3DGroup transform = new Transform3DGroup();

        if (normal.X > 0.5)
        {
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), -90)));
        }
        else if (normal.X < -0.5)
        {
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(0, 0, 1), 90)));
        }
        else if (normal.Y < -0.5)
        {
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 180)));
        }
        else if (normal.Z > 0.5)
        {
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), 90)));
        }
        else if (normal.Z < -0.5)
        {
            transform.Children.Add(new RotateTransform3D(new AxisAngleRotation3D(new Vector3D(1, 0, 0), -90)));
        }

        transform.Children.Add(new TranslateTransform3D(center.X, center.Y, center.Z));
        pip.Transform = transform;
        return pip;
    }

    private GeometryModel3D CreatePointTriangle(int point, Material material, double y)
    {
        int column = PointToColumn(point);
        double x1 = PlayLeft + column * ColumnWidth;
        double x2 = x1 + ColumnWidth;
        double xm = x1 + ColumnWidth / 2;

        Point3D a;
        Point3D b;
        Point3D c;

        if (point <= 11)
        {
            a = new Point3D(x1, y, BottomBaseZ);
            b = new Point3D(x2, y, BottomBaseZ);
            c = new Point3D(xm, y, BottomTipZ);
        }
        else
        {
            a = new Point3D(x1, y, TopBaseZ);
            b = new Point3D(xm, y, TopTipZ);
            c = new Point3D(x2, y, TopBaseZ);
        }

        MeshGeometry3D mesh = new MeshGeometry3D();
        mesh.Positions.Add(a);
        mesh.Positions.Add(b);
        mesh.Positions.Add(c);
        mesh.TriangleIndices.Add(0);
        mesh.TriangleIndices.Add(1);
        mesh.TriangleIndices.Add(2);

        GeometryModel3D model = new GeometryModel3D(mesh, material);
        model.BackMaterial = material;
        return model;
    }

    private GeometryModel3D CreateBox(Point3D center, Vector3D size, Material material)
    {
        double x = size.X / 2;
        double y = size.Y / 2;
        double z = size.Z / 2;

        double xm = center.X - x;
        double xp = center.X + x;
        double ym = center.Y - y;
        double yp = center.Y + y;
        double zm = center.Z - z;
        double zp = center.Z + z;
        MeshGeometry3D mesh = new MeshGeometry3D();

        void AddFace(Point3D a, Point3D b, Point3D c, Point3D d, Vector3D normal)
        {
            int start = mesh.Positions.Count;
            mesh.Positions.Add(a);
            mesh.Positions.Add(b);
            mesh.Positions.Add(c);
            mesh.Positions.Add(d);

            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);
            mesh.Normals.Add(normal);

            mesh.TriangleIndices.Add(start);
            mesh.TriangleIndices.Add(start + 1);
            mesh.TriangleIndices.Add(start + 2);
            mesh.TriangleIndices.Add(start);
            mesh.TriangleIndices.Add(start + 2);
            mesh.TriangleIndices.Add(start + 3);
        }

        AddFace(new Point3D(xm, ym, zm), new Point3D(xm, yp, zm), new Point3D(xp, yp, zm), new Point3D(xp, ym, zm), new Vector3D(0, 0, -1));
        AddFace(new Point3D(xm, ym, zp), new Point3D(xp, ym, zp), new Point3D(xp, yp, zp), new Point3D(xm, yp, zp), new Vector3D(0, 0, 1));
        AddFace(new Point3D(xm, ym, zm), new Point3D(xp, ym, zm), new Point3D(xp, ym, zp), new Point3D(xm, ym, zp), new Vector3D(0, -1, 0));
        AddFace(new Point3D(xm, yp, zm), new Point3D(xm, yp, zp), new Point3D(xp, yp, zp), new Point3D(xp, yp, zm), new Vector3D(0, 1, 0));
        AddFace(new Point3D(xp, ym, zm), new Point3D(xp, yp, zm), new Point3D(xp, yp, zp), new Point3D(xp, ym, zp), new Vector3D(1, 0, 0));
        AddFace(new Point3D(xm, ym, zm), new Point3D(xm, ym, zp), new Point3D(xm, yp, zp), new Point3D(xm, yp, zm), new Vector3D(-1, 0, 0));

        GeometryModel3D model = new GeometryModel3D(mesh, material);
        model.BackMaterial = material;
        return model;
    }

    private GeometryModel3D CreateCylinder(Point3D center, double radius, double height, int sides, Material material)
    {
        MeshGeometry3D mesh = new MeshGeometry3D();
        double top = center.Y + height / 2;
        double bottom = center.Y - height / 2;

        int topCenter = 0;
        int bottomCenter = 1;
        mesh.Positions.Add(new Point3D(center.X, top, center.Z));
        mesh.Positions.Add(new Point3D(center.X, bottom, center.Z));

        for (int i = 0; i < sides; i++)
        {
            double angle = Math.PI * 2 * i / sides;
            double x = center.X + Math.Cos(angle) * radius;
            double z = center.Z + Math.Sin(angle) * radius;
            mesh.Positions.Add(new Point3D(x, top, z));
            mesh.Positions.Add(new Point3D(x, bottom, z));
        }

        for (int i = 0; i < sides; i++)
        {
            int next = (i + 1) % sides;
            int topA = 2 + i * 2;
            int bottomA = topA + 1;
            int topB = 2 + next * 2;
            int bottomB = topB + 1;

            mesh.TriangleIndices.Add(topCenter);
            mesh.TriangleIndices.Add(topA);
            mesh.TriangleIndices.Add(topB);

            mesh.TriangleIndices.Add(bottomCenter);
            mesh.TriangleIndices.Add(bottomB);
            mesh.TriangleIndices.Add(bottomA);

            mesh.TriangleIndices.Add(topA);
            mesh.TriangleIndices.Add(bottomA);
            mesh.TriangleIndices.Add(bottomB);

            mesh.TriangleIndices.Add(topA);
            mesh.TriangleIndices.Add(bottomB);
            mesh.TriangleIndices.Add(topB);
        }

        GeometryModel3D model = new GeometryModel3D(mesh, material);
        model.BackMaterial = material;
        return model;
    }

    private Material Material(Color color)
    {
        SolidColorBrush brush = new SolidColorBrush(color);
        return new DiffuseMaterial(brush);
    }

    private Material ShinyMaterial(Color diffuse, Color specular, double power)
    {
        MaterialGroup group = new MaterialGroup();
        group.Children.Add(new DiffuseMaterial(new SolidColorBrush(diffuse)));
        group.Children.Add(new SpecularMaterial(new SolidColorBrush(specular), power));
        return group;
    }

    private Material TransparentMaterial(Color color)
    {
        SolidColorBrush brush = new SolidColorBrush(color);
        brush.Opacity = color.A / 255.0;
        return new DiffuseMaterial(brush);
    }

    private int PointToColumn(int point)
    {
        if (point <= 11)
        {
            return point;
        }

        return 23 - point;
    }

    private double GetPointX(int point)
    {
        int column = PointToColumn(point);
        return PlayLeft + column * ColumnWidth + ColumnWidth / 2;
    }

    private void UpdateStatus()
    {
        string player = GetPlayerName(game.CurrentPlayer);
        if (game.DiceRemaining.Count == 0)
        {
            UpdateStatus(player + " sirada. Zar at.");
            return;
        }

        UpdateStatus(player + " oynuyor. Kalan zar: " + string.Join(", ", game.DiceRemaining));
    }

    private void UpdateStatus(string text)
    {
        statusText.Text = text;
    }

    private string GetPlayerName(PlayerColor player)
    {
        if (player == PlayerColor.White)
        {
            return "Beyaz";
        }

        if (player == PlayerColor.Black)
        {
            return "Siyah";
        }

        return "Yok";
    }

    private class MoveAnimation
    {
        public BackgammonMove Move { get; set; }
        public Point3D Start { get; set; }
        public Point3D End { get; set; }
        public PlayerColor Color { get; set; }
        public double Progress { get; set; }
    }

    private class MoveOption
    {
        public int TargetPoint { get; set; }
        public bool BearsOff { get; set; }
        public List<BackgammonMove> Steps { get; set; }
    }
}
