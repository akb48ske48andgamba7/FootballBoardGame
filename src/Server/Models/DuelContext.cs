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
    public int AbilityBonus { get; set; } // シュート時のGK手を使ったセーブなどで+1
    public bool IsGoalkeeper { get; set; }

    public int TotalAbility => Ability + AbilityBonus;
}

public class DuelContext
{
    public Guid DuelId { get; set; } = Guid.NewGuid();
    public DuelType Type { get; set; }
    public Position DuelPosition { get; set; } = new(4, 1);
    public Position? PassTargetPosition { get; set; }

    public TeamType AttackingTeam { get; set; }
    public TeamType DefendingTeam { get; set; }

    public List<DuelParticipant> Attackers { get; set; } = new();
    public List<DuelParticipant> Defenders { get; set; } = new();

    public int AttackerAbilitySum => Attackers.Sum(p => p.TotalAbility);
    public int DefenderAbilitySum => Defenders.Sum(p => p.TotalAbility);

    public int? AttackerDice { get; set; }
    public int? DefenderDice { get; set; }

    public int AttackerTotal => AttackerAbilitySum + (AttackerDice ?? 0);
    public int DefenderTotal => DefenderAbilitySum + (DefenderDice ?? 0);

    public TeamType? Winner { get; set; }
    public bool IsResolved => Winner.HasValue;
    public string Message { get; set; } = string.Empty;
    public bool HasGkHandBonus => Defenders.Any(d => d.IsGoalkeeper && d.AbilityBonus > 0);
}
