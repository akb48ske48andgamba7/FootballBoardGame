using System.Security.Cryptography;

namespace FootballBoardGame.Server.Services;

public interface IDiceService
{
    int Roll();
    (int Die1, int Die2, int Total) RollTwo();
}

public class DiceService : IDiceService
{
    public int Roll()
    {
        return RandomNumberGenerator.GetInt32(1, 7); // 1〜6
    }

    public (int Die1, int Die2, int Total) RollTwo()
    {
        int d1 = Roll();
        int d2 = Roll();
        return (d1, d2, d1 + d2);
    }
}
