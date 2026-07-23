using ErkekTavlasi.AI;
using ErkekTavlasi.Game;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using Random = UnityEngine.Random;

namespace ErkekTavlasi.UnityView
{
    public class BackgammonGameController : MonoBehaviour
    {
        public GameObject WhiteCheckerPrefab;
        public GameObject BlackCheckerPrefab;
        public GameObject HighlightPrefab;
        public DiceView DieA;
        public DiceView DieB;
        public Button RollButton;
        public Button NewGameButton;
        public Button MenuButton;
        public Button Local3DButton;
        public Button Local2DButton;
        public Button PlayVsAi3DButton;
        public Button PlayVsAi2DButton;
        public Button Bo53DButton;
        public Button Bo52DButton;
        public Button TrainAiButton;
        public Button ExitButton;
        public InputField TrainMinutesInput;
        public Text StatusText;
        public Text MatchText;
        public Text MenuInfoText;
        public Transform CheckersRoot;
        public Transform HighlightsRoot;
        public GameObject MainMenuPanel;
        public GameObject HudPanel;
        public CameraRig CameraRig;

        private readonly List<GameObject> checkerObjects = new List<GameObject>();
        private readonly List<GameObject> highlightObjects = new List<GameObject>();
        private readonly Queue<BackgammonMove> queuedMoves = new Queue<BackgammonMove>();

        private GameState game;
        private MonteCarloMemory aiMemory;
        private MonteCarloWeights aiWeights;
        private MonteCarloAi ai;
        private int selectedPoint = int.MinValue;
        private bool busy;
        private bool playVsAi;
        private bool bo5Mode;
        private bool aiThinking;
        private bool training;
        private bool stopTrainingRequested;
        private bool openingRollPending;
        private bool openingWhiteRolled;
        private int openingWhiteRoll;
        private int whiteMatchScore;
        private int blackMatchScore;
        private PlayerColor nextRoundStarter = PlayerColor.None;
        private PlayerColor aiPlayer = PlayerColor.Black;
        private int dieAValue = 1;
        private int dieBValue = 1;

        private void Awake()
        {
            game = new GameState();

            if (RollButton != null)
            {
                RollButton.onClick.AddListener(RollDice);
            }

            if (NewGameButton != null)
            {
                NewGameButton.onClick.AddListener(NewGame);
            }

            if (MenuButton != null)
            {
                MenuButton.onClick.AddListener(ShowMainMenu);
            }

            if (Local3DButton != null)
            {
                Local3DButton.onClick.AddListener(StartThreeDMode);
            }

            if (Local2DButton != null)
            {
                Local2DButton.onClick.AddListener(StartTwoDMode);
            }

            if (PlayVsAi3DButton != null)
            {
                PlayVsAi3DButton.onClick.AddListener(StartVsAiMode);
            }

            if (PlayVsAi2DButton != null)
            {
                PlayVsAi2DButton.onClick.AddListener(StartVsAiTwoDMode);
            }

            if (Bo53DButton != null)
            {
                Bo53DButton.onClick.AddListener(StartBo5Mode);
            }

            if (Bo52DButton != null)
            {
                Bo52DButton.onClick.AddListener(StartBo5TwoDMode);
            }

            if (TrainAiButton != null)
            {
                TrainAiButton.onClick.AddListener(ShowTrainingPlaceholder);
            }

            if (ExitButton != null)
            {
                ExitButton.onClick.AddListener(ExitGame);
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
            Screen.fullScreenMode = FullScreenMode.FullScreenWindow;
            Screen.SetResolution(1920, 1080, FullScreenMode.FullScreenWindow);
#endif
            InitializeAi();
            RenderAll();
            UpdateStatus();
            ShowMainMenu();
        }

        public void StartThreeDMode()
        {
            playVsAi = false;
            bo5Mode = false;
            ResetMatchScore();
            StartMode(CameraRig.ViewMode.ThreeD, true);
        }

        public void StartTwoDMode()
        {
            playVsAi = false;
            bo5Mode = false;
            ResetMatchScore();
            StartMode(CameraRig.ViewMode.TwoD, true);
        }

        public void StartVsAiMode()
        {
            StartVsAiMode(CameraRig.ViewMode.ThreeD);
        }

        public void StartVsAiTwoDMode()
        {
            StartVsAiMode(CameraRig.ViewMode.TwoD);
        }

        private void StartVsAiMode(CameraRig.ViewMode mode)
        {
            playVsAi = true;
            bo5Mode = false;
            aiPlayer = PlayerColor.Black;
            ResetMatchScore();
            StartMode(mode, true);
            UpdateStatus("Play vs AI: Sen beyazsin. Baslama zari icin Zar At.");
        }

        public void StartBo5Mode()
        {
            StartBo5Mode(CameraRig.ViewMode.ThreeD);
        }

        public void StartBo5TwoDMode()
        {
            StartBo5Mode(CameraRig.ViewMode.TwoD);
        }

        private void StartBo5Mode(CameraRig.ViewMode mode)
        {
            playVsAi = true;
            bo5Mode = true;
            aiPlayer = PlayerColor.Black;
            ResetMatchScore();
            StartMode(mode, true);
            UpdateStatus("BO5 vs AI: ilk oyun baslama zariyla baslar.");
        }

        public void ShowTrainingPlaceholder()
        {
            StartTraining();
        }

        public void ExitGame()
        {
            if (training)
            {
                stopTrainingRequested = true;
                UpdateTrainButtonText("Stopping...");
                if (MenuInfoText != null)
                {
                    MenuInfoText.text = "Training kaydediliyor. Aktif oyun bitince duracak.";
                }

                return;
            }

            Application.Quit();
        }

        private void StartMode(CameraRig.ViewMode mode, bool useOpeningRoll)
        {
            if (CameraRig != null)
            {
                CameraRig.SetMode(mode);
            }

            if (MainMenuPanel != null)
            {
                MainMenuPanel.SetActive(false);
            }

            if (HudPanel != null)
            {
                HudPanel.SetActive(true);
            }

            NewGame(useOpeningRoll, PlayerColor.None);
        }

        private void ShowMainMenu()
        {
            playVsAi = false;
            bo5Mode = false;
            UpdateMatchText();
            if (MainMenuPanel != null)
            {
                MainMenuPanel.SetActive(true);
            }

            if (HudPanel != null)
            {
                HudPanel.SetActive(false);
            }

            if (MenuInfoText != null)
            {
                MenuInfoText.text = training ? "Training devam ediyor..." : "Hazir.";
            }
        }

        private void Update()
        {
            if (busy || aiThinking || training || Input.GetMouseButtonDown(0) == false)
            {
                return;
            }

            if (playVsAi && game.CurrentPlayer == aiPlayer)
            {
                return;
            }

            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
            RaycastHit[] hits = Physics.RaycastAll(ray, 200f);
            if (hits.Length == 0)
            {
                ClearSelection();
                return;
            }

            Array.Sort(hits, (a, b) => a.distance.CompareTo(b.distance));

            LegalMoveClickTarget legalTarget = FindHitTarget<LegalMoveClickTarget>(hits);
            if (legalTarget != null)
            {
                HandleLegalMoveClick(legalTarget);
                return;
            }

            PointClickTarget pointTarget = FindHitTarget<PointClickTarget>(hits);
            if (pointTarget != null)
            {
                HandlePointClick(pointTarget.PointIndex);
                return;
            }

            SpecialClickTarget specialTarget = FindHitTarget<SpecialClickTarget>(hits);
            if (specialTarget != null)
            {
                HandleSpecialClick(specialTarget.Name);
            }
        }

        private T FindHitTarget<T>(RaycastHit[] hits) where T : Component
        {
            for (int i = 0; i < hits.Length; i++)
            {
                T target = hits[i].collider.GetComponentInParent<T>();
                if (target != null)
                {
                    return target;
                }
            }

            return null;
        }

        public void RollDice()
        {
            if (busy || aiThinking || game.DiceRemaining.Count > 0)
            {
                return;
            }

            if (playVsAi && game.CurrentPlayer == aiPlayer)
            {
                return;
            }

            StartCoroutine(openingRollPending ? OpeningRollRoutine() : RollDiceRoutine());
        }

        public void NewGame()
        {
            ResetMatchScore();
            NewGame(true, PlayerColor.None);
        }

        private void NewGame(bool useOpeningRoll, PlayerColor forcedStarter)
        {
            StopAllCoroutines();
            game.StartNewGame();
            selectedPoint = int.MinValue;
            queuedMoves.Clear();
            busy = false;
            aiThinking = false;
            openingRollPending = useOpeningRoll;
            openingWhiteRolled = false;
            openingWhiteRoll = 0;
            if (!useOpeningRoll && forcedStarter != PlayerColor.None)
            {
                game.CurrentPlayer = forcedStarter;
            }

            if (RollButton != null)
            {
                RollButton.interactable = !playVsAi || openingRollPending || game.CurrentPlayer != aiPlayer;
            }

            DieA.Park(new Vector3(-1.5f, 0.62f, 0f), 1);
            DieB.Park(new Vector3(0.5f, 0.62f, 0f), 1);
            RenderAll();
            UpdateMatchText();
            UpdateStatus();
            MaybeStartAiTurn();
        }

        private IEnumerator OpeningRollRoutine()
        {
            busy = true;
            selectedPoint = int.MinValue;
            ClearHighlights();

            if (RollButton != null)
            {
                RollButton.interactable = false;
            }

            int attempts = 0;
            while (true)
            {
                PlayerColor roller = openingWhiteRolled ? PlayerColor.Black : PlayerColor.White;
                UpdateStatus(attempts == 0
                    ? PlayerName(roller) + " baslama zari atiyor..."
                    : PlayerName(roller) + " zari kirik geldi. Tekrar atiliyor...");

                yield return RollSingleOpeningDie(roller, attempts);
                yield return WaitForDiceToSettle();

                DiceView activeDie = roller == PlayerColor.White ? DieA : DieB;
                if (!activeDie.IsCrooked(0.90f))
                {
                    activeDie.Freeze();
                    int roll = activeDie.ReadTopFace();

                    if (!openingWhiteRolled)
                    {
                        busy = false;
                        openingWhiteRolled = true;
                        openingWhiteRoll = roll;
                        dieAValue = roll;
                        UpdateStatus("Beyaz " + roll + " atti. Siyah baslama zari atacak.");

                        if (RollButton != null)
                        {
                            RollButton.interactable = !playVsAi;
                        }

                        if (playVsAi)
                        {
                            StartCoroutine(AutoBlackOpeningRollRoutine());
                        }

                        yield break;
                    }

                    int whiteRoll = openingWhiteRoll;
                    int blackRoll = roll;
                    dieBValue = blackRoll;
                    if (whiteRoll == blackRoll)
                    {
                        busy = false;
                        openingWhiteRolled = false;
                        openingWhiteRoll = 0;
                        if (RollButton != null)
                        {
                            RollButton.interactable = true;
                        }

                        UpdateStatus("Baslama zari esit: " + whiteRoll + "-" + blackRoll + ". Beyaz tekrar atacak.");
                        yield break;
                    }

                    game.SetOpeningRoll(whiteRoll, blackRoll);
                    openingRollPending = false;
                    openingWhiteRolled = false;
                    openingWhiteRoll = 0;
                    busy = false;
                    if (RollButton != null)
                    {
                        RollButton.interactable = !playVsAi || game.CurrentPlayer != aiPlayer;
                    }

                    UpdateStatus("Baslama zari: Beyaz " + whiteRoll + ", Siyah " + blackRoll
                        + ". " + PlayerName(game.CurrentPlayer) + " basliyor. Zar atacak.");

                    if (playVsAi && game.CurrentPlayer == aiPlayer)
                    {
                        MaybeStartAiTurn();
                    }

                    yield break;
                }

                attempts++;
                if (attempts >= 8)
                {
                    busy = false;
                    if (RollButton != null)
                    {
                        RollButton.interactable = true;
                    }

                    UpdateStatus("Baslama zari net gelmedi. Tekrar Zar At.");
                    yield break;
                }
            }
        }

        private IEnumerator AutoBlackOpeningRollRoutine()
        {
            yield return new WaitForSeconds(0.65f);
            if (openingRollPending && openingWhiteRolled && !busy)
            {
                yield return OpeningRollRoutine();
            }
        }

        private void InitializeAi()
        {
            string dataDirectory = Path.Combine(Application.persistentDataPath, "AI_Data");
            string memoryPath = Path.Combine(dataDirectory, "alphazero_raw_memory.tsv");
            string weightsPath = Path.Combine(dataDirectory, "alphazero_raw_weights.tsv");
            aiMemory = new MonteCarloMemory(memoryPath);
            aiMemory.Load();
            aiWeights = new MonteCarloWeights(weightsPath);
            aiWeights.Load();
            ai = new MonteCarloAi(aiMemory, aiWeights, Environment.TickCount);
        }

        private void StartTraining()
        {
            if (training)
            {
                stopTrainingRequested = true;
                UpdateTrainButtonText("Stopping...");
                if (MenuInfoText != null)
                {
                    MenuInfoText.text = "Aktif oyun bitince kaydedip duracak.";
                }

                return;
            }

            if (ai == null)
            {
                InitializeAi();
            }

            int minutes = GetTrainingMinutes();
            StartCoroutine(TrainAiRoutine(minutes));
        }

        private int GetTrainingMinutes()
        {
            int minutes = 10;
            if (TrainMinutesInput != null && !string.IsNullOrWhiteSpace(TrainMinutesInput.text))
            {
                int.TryParse(TrainMinutesInput.text, out minutes);
            }

            return Mathf.Clamp(minutes, 1, 1440);
        }

        private IEnumerator TrainAiRoutine(int minutes)
        {
            training = true;
            stopTrainingRequested = false;
            int trainedMatches = 0;
            int trainedGames = 0;
            int whiteWins = 0;
            int blackWins = 0;
            int whiteScore = 0;
            int blackScore = 0;
            PlayerColor nextStarter = PlayerColor.None;
            float endTime = Time.realtimeSinceStartup + minutes * 60f;
            UpdateTrainButtonText("Stop & Save");

            if (MenuInfoText != null)
            {
                MenuInfoText.text = "Training basladi: " + minutes + " dk.";
            }

            while (Time.realtimeSinceStartup < endTime && !stopTrainingRequested)
            {
                bool useOpeningRoll = nextStarter == PlayerColor.None;
                GameTrainingResult result = ai.TrainSingleBo5Game(useOpeningRoll, nextStarter);
                nextStarter = result.Winner;
                int points = result.Mars ? 2 : 1;

                if (result.Winner == PlayerColor.White)
                {
                    whiteWins++;
                    whiteScore = Mathf.Min(3, whiteScore + points);
                }
                else if (result.Winner == PlayerColor.Black)
                {
                    blackWins++;
                    blackScore = Mathf.Min(3, blackScore + points);
                }

                trainedGames++;
                if (whiteScore >= 3 || blackScore >= 3)
                {
                    trainedMatches++;
                    whiteScore = 0;
                    blackScore = 0;
                    nextStarter = PlayerColor.None;
                }

                SaveAiData();

                if (MenuInfoText != null)
                {
                    int remainingSeconds = Mathf.Max(0, Mathf.CeilToInt(endTime - Time.realtimeSinceStartup));
                    MenuInfoText.text = "Training: " + trainedGames + " oyun, "
                        + trainedMatches + " BO5. Kalan ~" + Mathf.CeilToInt(remainingSeconds / 60f) + " dk.";
                }

                yield return null;
            }

            SaveAiData();
            training = false;
            stopTrainingRequested = false;
            UpdateTrainButtonText("Train AI");
            if (MenuInfoText != null)
            {
                MenuInfoText.text = "Training kaydedildi. " + trainedGames + " oyun, "
                    + trainedMatches + " BO5.";
            }
        }

        private void UpdateTrainButtonText(string value)
        {
            if (TrainAiButton == null)
            {
                return;
            }

            Text text = TrainAiButton.GetComponentInChildren<Text>();
            if (text != null)
            {
                text.text = value;
            }
        }

        private void SaveAiData()
        {
            if (aiMemory != null)
            {
                aiMemory.Save();
            }

            if (aiWeights != null)
            {
                aiWeights.Save();
            }
        }

        private IEnumerator RollDiceRoutine()
        {
            busy = true;
            selectedPoint = int.MinValue;
            ClearHighlights();

            if (RollButton != null)
            {
                RollButton.interactable = false;
            }

            int attempts = 0;
            while (true)
            {
                UpdateStatus(attempts == 0 ? "Zarlar atiliyor..." : "Zar kirik geldi. Tekrar atiliyor...");
                yield return RollPhysicalDice(attempts);
                yield return WaitForDiceToSettle();

                if (!DiceAreCrooked())
                {
                    break;
                }

                attempts++;
                if (attempts >= 8)
                {
                    busy = false;
                    if (RollButton != null)
                    {
                        RollButton.interactable = true;
                    }

                    UpdateStatus("Zarlar duz durmadi. Tekrar zar at.");
                    yield break;
                }
            }

            DieA.Freeze();
            DieB.Freeze();
            dieAValue = DieA.ReadTopFace();
            dieBValue = DieB.ReadTopFace();
            game.SetDice(dieAValue, dieBValue);

            if (game.GetLegalMoves().Count == 0)
            {
                game.EndTurn();
                busy = false;
                if (RollButton != null)
                {
                    RollButton.interactable = !playVsAi || game.CurrentPlayer != aiPlayer;
                }

                UpdateStatus("Hamle yok. Sira degisti.");
                MaybeStartAiTurn();
                yield break;
            }

            busy = false;
            UpdateStatus();
            MaybeStartAiTurn();
        }

        private IEnumerator RollPhysicalDice(int attempts)
        {
            float direction = game.CurrentPlayer == PlayerColor.White ? 1f : -1f;
            float handZ = game.CurrentPlayer == PlayerColor.White ? -4.05f : 4.05f;
            Vector3 startA = new Vector3(Random.Range(-2.75f, -1.25f), 1.42f, handZ + Random.Range(-0.15f, 0.15f));
            Vector3 startB = new Vector3(startA.x + Random.Range(0.65f, 1.05f), 1.50f, handZ + Random.Range(-0.15f, 0.15f));

            yield return MoveDiceToThrowHand(startA, startB, attempts);

            float sideCurveA = Random.Range(-1.35f, 1.35f);
            float sideCurveB = Random.Range(-1.35f, 1.35f);
            DieA.RollFromCurrentPose(
                new Vector3(sideCurveA, Random.Range(1.10f, 1.65f), direction * Random.Range(5.2f, 6.9f)),
                Random.onUnitSphere * Random.Range(26f, 42f));
            DieB.RollFromCurrentPose(
                new Vector3(sideCurveB, Random.Range(1.10f, 1.65f), direction * Random.Range(5.0f, 6.7f)),
                Random.onUnitSphere * Random.Range(26f, 42f));
        }

        private IEnumerator RollSingleOpeningDie(PlayerColor roller, int attempts)
        {
            DiceView die = roller == PlayerColor.White ? DieA : DieB;
            float direction = roller == PlayerColor.White ? 1f : -1f;
            float handZ = roller == PlayerColor.White ? -4.05f : 4.05f;
            float handX = roller == PlayerColor.White ? Random.Range(-2.65f, -1.25f) : Random.Range(1.25f, 2.65f);
            Vector3 start = new Vector3(handX, 1.44f, handZ + Random.Range(-0.15f, 0.15f));

            yield return MoveSingleDieToThrowHand(die, start, attempts);

            die.RollFromCurrentPose(
                new Vector3(Random.Range(-1.20f, 1.20f), Random.Range(1.05f, 1.55f), direction * Random.Range(4.8f, 6.3f)),
                Random.onUnitSphere * Random.Range(25f, 40f));
        }

        private IEnumerator MoveDiceToThrowHand(Vector3 targetA, Vector3 targetB, int attempts)
        {
            Vector3 fromA = DieA.transform.position;
            Vector3 fromB = DieB.transform.position;
            Quaternion rotA = Random.rotation;
            Quaternion rotB = Random.rotation;
            float duration = attempts == 0 ? 0.24f : 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 posA = Vector3.Lerp(fromA, targetA, eased);
                Vector3 posB = Vector3.Lerp(fromB, targetB, eased);
                posA.y += Mathf.Sin(t * Mathf.PI) * 0.22f;
                posB.y += Mathf.Sin(t * Mathf.PI) * 0.22f;
                DieA.SetKinematicPose(posA, Quaternion.Slerp(DieA.transform.rotation, rotA, eased));
                DieB.SetKinematicPose(posB, Quaternion.Slerp(DieB.transform.rotation, rotB, eased));
                yield return null;
            }

            DieA.SetKinematicPose(targetA, Random.rotation);
            DieB.SetKinematicPose(targetB, Random.rotation);
        }

        private IEnumerator MoveSingleDieToThrowHand(DiceView die, Vector3 target, int attempts)
        {
            Vector3 from = die.transform.position;
            Quaternion targetRotation = Random.rotation;
            float duration = attempts == 0 ? 0.24f : 0.16f;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 3f);
                Vector3 pos = Vector3.Lerp(from, target, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.22f;
                die.SetKinematicPose(pos, Quaternion.Slerp(die.transform.rotation, targetRotation, eased));
                yield return null;
            }

            die.SetKinematicPose(target, Random.rotation);
        }

        private IEnumerator WaitForDiceToSettle()
        {
            float elapsed = 0f;
            float settledTime = 0f;
            while (elapsed < 4.0f)
            {
                elapsed += Time.deltaTime;
                if (DieA.IsSettled() && DieB.IsSettled())
                {
                    settledTime += Time.deltaTime;
                    if (settledTime >= 0.35f)
                    {
                        yield break;
                    }
                }
                else
                {
                    settledTime = 0f;
                }

                yield return null;
            }
        }

        private bool DiceAreCrooked()
        {
            return DieA.IsCrooked(0.90f) || DieB.IsCrooked(0.90f);
        }

        private void HandlePointClick(int point)
        {
            if (openingRollPending)
            {
                UpdateStatus("Once baslama zari atilmali.");
                return;
            }

            if (game.DiceRemaining.Count == 0)
            {
                UpdateStatus("Once zar atmalisin.");
                return;
            }

            if (selectedPoint == int.MinValue)
            {
                TrySelectPoint(point);
                return;
            }

            if (selectedPoint == point)
            {
                ClearSelection("Secim iptal edildi.");
                return;
            }

            TrySelectPoint(point);
        }

        private void HandleLegalMoveClick(LegalMoveClickTarget target)
        {
            if (selectedPoint == int.MinValue)
            {
                return;
            }

            List<MoveOption> options = MoveOptionFinder.GetOptions(game, selectedPoint);
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].BearsOff == target.BearsOff && (target.BearsOff || options[i].TargetPoint == target.TargetPoint))
                {
                    StartMoveSequence(options[i]);
                    return;
                }
            }

            ClearSelection("Bu hedef mevcut zarlarla yasal degil.");
        }

        private void HandleSpecialClick(string name)
        {
            if (openingRollPending)
            {
                UpdateStatus("Once baslama zari atilmali.");
                return;
            }

            if (name == "bar")
            {
                if (selectedPoint == BackgammonMove.FromBar)
                {
                    ClearSelection("Secim iptal edildi.");
                    return;
                }

                TrySelectPoint(BackgammonMove.FromBar);
                return;
            }

            if (name == "white-off" && game.CurrentPlayer == PlayerColor.White)
            {
                TryBearOff();
                return;
            }

            if (name == "black-off" && game.CurrentPlayer == PlayerColor.Black)
            {
                TryBearOff();
            }
        }

        private void TrySelectPoint(int point)
        {
            if (PassTurnIfNoLegalMoves("Hamle yok. Sira degisti."))
            {
                MaybeStartAiTurn();
                return;
            }

            if (game.GetBarCount(game.CurrentPlayer) > 0 && point != BackgammonMove.FromBar)
            {
                UpdateStatus("Kirik tas varken once ortadaki bardan giris yapmalisin.");
                return;
            }

            if (point == BackgammonMove.FromBar)
            {
                if (game.GetBarCount(game.CurrentPlayer) == 0)
                {
                    UpdateStatus("Barda tasin yok.");
                    return;
                }

                if (MoveOptionFinder.GetOptions(game, point).Count == 0)
                {
                    PassTurnIfNoLegalMoves("Kirik tas giremiyor. Sira degisti.");
                    MaybeStartAiTurn();
                    return;
                }

                selectedPoint = point;
                RenderHighlights();
                UpdateStatus("Bardaki tas secildi. Yesil giris alanina tikla.");
                return;
            }

            PointStack stack = game.Points[point];
            if (stack.Owner != game.CurrentPlayer || stack.Count == 0)
            {
                UpdateStatus("Kendi tasini secmelisin.");
                return;
            }

            if (MoveOptionFinder.GetOptions(game, point).Count == 0)
            {
                UpdateStatus("Bu tas mevcut zarlarla oynayamiyor.");
                return;
            }

            selectedPoint = point;
            RenderHighlights();
            UpdateStatus("Tas secildi. Yesil hedeflerden birine tikla.");
        }

        private void TryBearOff()
        {
            if (selectedPoint == int.MinValue)
            {
                return;
            }

            List<MoveOption> options = MoveOptionFinder.GetOptions(game, selectedPoint);
            for (int i = 0; i < options.Count; i++)
            {
                if (options[i].BearsOff)
                {
                    StartMoveSequence(options[i]);
                    return;
                }
            }

            ClearSelection("Bu tas su anda toplanamaz.");
        }

        private void StartMoveSequence(MoveOption option)
        {
            queuedMoves.Clear();
            for (int i = 1; i < option.Steps.Count; i++)
            {
                queuedMoves.Enqueue(option.Steps[i]);
            }

            selectedPoint = int.MinValue;
            ClearHighlights();
            StartCoroutine(AnimateMove(option.Steps[0]));
        }

        private IEnumerator AnimateMove(BackgammonMove move)
        {
            busy = true;

            Vector3 start = GetMoveStart(move);
            Vector3 end = BoardLayout.TargetPosition(move, game);
            GameObject mover = Instantiate(GetCheckerPrefab(game.CurrentPlayer), start, Quaternion.identity, CheckersRoot);

            float elapsed = 0f;
            float duration = 0.34f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float eased = 1f - Mathf.Pow(1f - t, 2f);
                Vector3 pos = Vector3.Lerp(start, end, eased);
                pos.y += Mathf.Sin(t * Mathf.PI) * 0.55f;
                mover.transform.position = pos;
                yield return null;
            }

            Destroy(mover);
            game.ApplyMove(move);
            RenderAll();

            if (queuedMoves.Count > 0 && !game.IsGameOver())
            {
                StartCoroutine(AnimateMove(queuedMoves.Dequeue()));
                yield break;
            }

            busy = false;
            if (game.IsGameOver())
            {
                FinishGame();
                yield break;
            }

            if (game.DiceRemaining.Count == 0 || game.GetLegalMoves().Count == 0)
            {
                game.EndTurn();
                if (RollButton != null)
                {
                    RollButton.interactable = true;
                }

                UpdateStatus("Sira degisti. " + PlayerName(game.CurrentPlayer) + " zar atacak.");
                MaybeStartAiTurn();
            }
            else
            {
                UpdateStatus();
            }
        }

        private bool PassTurnIfNoLegalMoves(string message)
        {
            if (openingRollPending || game.IsGameOver() || game.DiceRemaining.Count == 0 || game.GetLegalMoves().Count > 0)
            {
                return false;
            }

            selectedPoint = int.MinValue;
            queuedMoves.Clear();
            ClearHighlights();
            game.EndTurn();
            if (RollButton != null)
            {
                RollButton.interactable = !playVsAi || game.CurrentPlayer != aiPlayer;
            }

            UpdateStatus(message);
            return true;
        }

        private void FinishGame()
        {
            PlayerColor winner = game.GetWinner();
            bool mars = IsMars(winner);
            int points = mars ? 2 : 1;
            string result = PlayerName(winner) + " kazandi" + (mars ? " - MARS!" : ".");

            if (!bo5Mode)
            {
                UpdateStatus(result);
                if (RollButton != null)
                {
                    RollButton.interactable = false;
                }

                return;
            }

            if (winner == PlayerColor.White)
            {
                whiteMatchScore = Mathf.Min(3, whiteMatchScore + points);
            }
            else if (winner == PlayerColor.Black)
            {
                blackMatchScore = Mathf.Min(3, blackMatchScore + points);
            }

            nextRoundStarter = winner;
            UpdateMatchText();

            if (whiteMatchScore >= 3 || blackMatchScore >= 3)
            {
                UpdateStatus("BO5 bitti. " + PlayerName(winner) + " maci kazandi. Skor "
                    + whiteMatchScore + "-" + blackMatchScore + (mars ? " (mars)" : ""));
                if (RollButton != null)
                {
                    RollButton.interactable = false;
                }

                return;
            }

            UpdateStatus(result + " BO5 skor " + whiteMatchScore + "-" + blackMatchScore
                + ". Sonraki oyuna " + PlayerName(nextRoundStarter) + " baslayacak.");
            StartCoroutine(StartNextBo5RoundRoutine());
        }

        private IEnumerator StartNextBo5RoundRoutine()
        {
            yield return new WaitForSeconds(1.3f);
            NewGame(false, nextRoundStarter);
        }

        private bool IsMars(PlayerColor winner)
        {
            if (winner == PlayerColor.White)
            {
                return game.BlackOff == 0;
            }

            if (winner == PlayerColor.Black)
            {
                return game.WhiteOff == 0;
            }

            return false;
        }

        private void ResetMatchScore()
        {
            whiteMatchScore = 0;
            blackMatchScore = 0;
            nextRoundStarter = PlayerColor.None;
            UpdateMatchText();
        }

        private void UpdateMatchText()
        {
            if (MatchText == null)
            {
                return;
            }

            MatchText.text = bo5Mode ? "BO5 " + whiteMatchScore + "-" + blackMatchScore : "";
        }

        private void MaybeStartAiTurn()
        {
            if (!playVsAi || aiThinking || busy || game.IsGameOver() || game.CurrentPlayer != aiPlayer)
            {
                return;
            }

            StartCoroutine(AiTurnRoutine());
        }

        private IEnumerator AiTurnRoutine()
        {
            aiThinking = true;
            ClearSelection();
            if (RollButton != null)
            {
                RollButton.interactable = false;
            }

            yield return new WaitForSeconds(0.35f);
            if (game.DiceRemaining.Count == 0)
            {
                yield return RollDiceRoutine();
            }

            while (game.CurrentPlayer == aiPlayer && game.DiceRemaining.Count > 0 && !game.IsGameOver())
            {
                yield return AiMoveRoutine();
                yield return new WaitForSeconds(0.18f);
            }

            aiThinking = false;
            if (RollButton != null && (!playVsAi || game.CurrentPlayer != aiPlayer))
            {
                RollButton.interactable = game.DiceRemaining.Count == 0;
            }
        }

        private IEnumerator AiMoveRoutine()
        {
            if (ai == null)
            {
                InitializeAi();
            }

            UpdateStatus("Siyah AI dusunuyor...");
            yield return null;

            MoveOption option = ai.ChooseMove(game, true);
            if (option == null)
            {
                game.EndTurn();
                aiThinking = false;
                UpdateStatus("AI hamle bulamadi. Sira degisti.");
                yield break;
            }

            StartMoveSequence(option);
            yield return null;
            while (busy)
            {
                yield return null;
            }
        }

        private Vector3 GetMoveStart(BackgammonMove move)
        {
            if (move.FromPoint == BackgammonMove.FromBar)
            {
                return BoardLayout.BarPosition(game.CurrentPlayer, Mathf.Max(0, game.GetBarCount(game.CurrentPlayer) - 1));
            }

            return BoardLayout.CheckerPosition(move.FromPoint, Mathf.Max(0, game.Points[move.FromPoint].Count - 1));
        }

        private void RenderAll()
        {
            RenderCheckers();
            RenderHighlights();
        }

        private void RenderCheckers()
        {
            for (int i = 0; i < checkerObjects.Count; i++)
            {
                Destroy(checkerObjects[i]);
            }

            checkerObjects.Clear();

            for (int point = 0; point < 24; point++)
            {
                PointStack stack = game.Points[point];
                for (int index = 0; index < stack.Count; index++)
                {
                    GameObject checker = Instantiate(GetCheckerPrefab(stack.Owner), BoardLayout.CheckerPosition(point, index), Quaternion.identity, CheckersRoot);
                    checker.AddComponent<PointClickTarget>().PointIndex = point;
                    checkerObjects.Add(checker);
                }
            }

            RenderBar(PlayerColor.White, game.WhiteBar);
            RenderBar(PlayerColor.Black, game.BlackBar);
            RenderBearOff(PlayerColor.White, game.WhiteOff);
            RenderBearOff(PlayerColor.Black, game.BlackOff);
        }

        private void RenderBar(PlayerColor color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject checker = Instantiate(GetCheckerPrefab(color), BoardLayout.BarPosition(color, i), Quaternion.identity, CheckersRoot);
                checker.AddComponent<SpecialClickTarget>().Name = "bar";
                checkerObjects.Add(checker);
            }
        }

        private void RenderBearOff(PlayerColor color, int count)
        {
            for (int i = 0; i < count; i++)
            {
                GameObject checker = Instantiate(GetCheckerPrefab(color), BoardLayout.BearOffPosition(color, i), Quaternion.identity, CheckersRoot);
                checker.AddComponent<SpecialClickTarget>().Name = color == PlayerColor.White ? "white-off" : "black-off";
                checkerObjects.Add(checker);
            }
        }

        private void RenderHighlights()
        {
            ClearHighlights();
            if (selectedPoint == int.MinValue)
            {
                return;
            }

            List<MoveOption> options = MoveOptionFinder.GetOptions(game, selectedPoint);
            for (int i = 0; i < options.Count; i++)
            {
                Vector3 pos = GetHighlightPosition(options[i]);

                GameObject highlight = Instantiate(HighlightPrefab, pos, Quaternion.identity, HighlightsRoot);
                highlight.transform.localScale = new Vector3(0.88f, 0.08f, 0.88f);

                LegalMoveClickTarget clickTarget = highlight.AddComponent<LegalMoveClickTarget>();
                clickTarget.BearsOff = options[i].BearsOff;
                if (options[i].BearsOff)
                {
                    clickTarget.TargetPoint = BackgammonMove.ToOffBoard;
                }
                else
                {
                    clickTarget.TargetPoint = options[i].TargetPoint;
                }

                highlightObjects.Add(highlight);
            }
        }

        private Vector3 GetHighlightPosition(MoveOption option)
        {
            if (option.Steps.Count == 0)
            {
                return Vector3.zero;
            }

            GameState preview = game.Clone();
            for (int i = 0; i < option.Steps.Count - 1; i++)
            {
                preview.ApplyMove(option.Steps[i]);
            }

            BackgammonMove finalStep = option.Steps[option.Steps.Count - 1];
            Vector3 pos = BoardLayout.TargetPosition(finalStep, preview);
            pos.y += 0.14f;
            return pos;
        }

        private void ClearHighlights()
        {
            for (int i = 0; i < highlightObjects.Count; i++)
            {
                Destroy(highlightObjects[i]);
            }

            highlightObjects.Clear();
        }

        private void ClearSelection(string message = null)
        {
            selectedPoint = int.MinValue;
            ClearHighlights();
            if (string.IsNullOrEmpty(message))
            {
                UpdateStatus();
            }
            else
            {
                UpdateStatus(message);
            }
        }

        private GameObject GetCheckerPrefab(PlayerColor color)
        {
            return color == PlayerColor.White ? WhiteCheckerPrefab : BlackCheckerPrefab;
        }

        private void UpdateStatus()
        {
            if (openingRollPending)
            {
                if (openingWhiteRolled)
                {
                    UpdateStatus("Beyaz " + openingWhiteRoll + " atti. Siyah baslama zari atacak.");
                }
                else
                {
                    UpdateStatus("Baslama zari icin Zar At. Once Beyaz, sonra Siyah.");
                }

                return;
            }

            if (game.DiceRemaining.Count == 0)
            {
                UpdateStatus(PlayerName(game.CurrentPlayer) + " sirada. Zar at.");
            }
            else
            {
                UpdateStatus(PlayerName(game.CurrentPlayer) + " oynuyor. Kalan zar: " + string.Join(", ", game.DiceRemaining));
            }
        }

        private void UpdateStatus(string message)
        {
            if (StatusText != null)
            {
                StatusText.text = message;
            }
        }

        private string PlayerName(PlayerColor player)
        {
            return player == PlayerColor.White ? "Beyaz" : "Siyah";
        }
    }
}
