namespace ErkekTavlasi.Game;

public class RandomPlayer : IPlayer
{
    private readonly Random random;

    public string Name { get; private set; }
    public PlayerColor Color { get; private set; }

    public RandomPlayer(PlayerColor color)
    {
        random = new Random();
        Color = color;
        Name = "Random AI";
    }

    public BackgammonMove ChooseMove(GameState state)
    {
        List<BackgammonMove> moves = state.GetLegalMoves();
        if (moves.Count == 0)
        {
            return null;
        }

        int index = random.Next(moves.Count);
        return moves[index];
    }
}
