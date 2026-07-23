namespace ErkekTavlasi.Game;

public interface IPlayer
{
    string Name { get; }
    PlayerColor Color { get; }

    BackgammonMove ChooseMove(GameState state);
}
