namespace ErkekTavlasi.Game;

public class BackgammonMove
{
    public const int FromBar = -1;
    public const int ToOffBoard = -2;

    public int FromPoint { get; set; }
    public int ToPoint { get; set; }
    public int Die { get; set; }
    public bool HitsOpponent { get; set; }
    public bool BearsOff { get; set; }

    public string GetName()
    {
        string from = FromPoint == FromBar ? "bar" : (FromPoint + 1).ToString();
        string to = ToPoint == ToOffBoard ? "toplama" : (ToPoint + 1).ToString();
        return from + " -> " + to + " (" + Die + ")";
    }
}
