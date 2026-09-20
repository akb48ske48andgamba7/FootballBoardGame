namespace FootballBoardGame.Server.Models;

public enum DuelType
{
    Tackle,     // 進入タックル
    Intercept,  // パス経路上でのパスカット
    Shot        // シュート阻止・GKセーブ
}

public class DuelParticipant
{
    public string PieceId { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Number { get; set; }
    public int Ability { get; set; }
    public bool IsGoalkeeper { get; set; }
}

public class DuelContext
{
    public Guid DuelId { get; set; } = Guid.NewGuid();
    public DuelType Type { get; set; }
    public Position DuelPosition { get; set; } = new(1, 1);
    public Position? PassTargetPosition { get; set; }

    public TeamType AttackingTeam { get; set; }
    public TeamType DefendingTeam { get; set; }

    public List<DuelParticipant> Attackers { get; set; } = new();
    public List<DuelParticipant> Defenders { get; set; } = new();

    public int AttackerAbilitySum => Attackers.Sum(p => p.Ability);
    public int DefenderAbilitySum => Defenders.Sum(p => p.Ability);

    public int? AttackerDice { get; set; }
    public int? DefenderDice { get; set; }

    public int AttackerTotal => AttackerAbilitySum + (AttackerDice ?? 0);
    public int DefenderTotal => DefenderAbilitySum + (DefenderDice ?? 0);

    public TeamType? Winner { get; set; }
    public bool IsResolved => Winner.HasValue;
    public string Message { get; set; } = string.Empty;
}
