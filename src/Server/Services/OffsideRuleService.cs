using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

public interface IOffsideRuleService
{
    int? GetOffsideLineRow(GameState state, TeamType attackingTeam);
    bool IsOffside(GameState state, TeamType attackingTeam, Position targetPosition);
}

public class OffsideRuleService : IOffsideRuleService
{
    public int? GetOffsideLineRow(GameState state, TeamType attackingTeam)
    {
        TeamType defendingTeam = attackingTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var defendingFieldPlayers = state.Pieces
            .Where(p => p.Team == defendingTeam && !p.IsGoalkeeper)
            .ToList();

        if (!defendingFieldPlayers.Any())
        {
            return null;
        }

        bool attackingDownward = (state.Half == 1 && attackingTeam == TeamType.TeamA) ||
                                 (state.Half == 2 && attackingTeam == TeamType.TeamB);

        // 下向きに攻める場合（Row 1 -> 10）: 守備側の最も下（Rowが大きい）のDFが最後尾ライン
        // 上向きに攻める場合（Row 10 -> 1）: 守備側の最も上（Rowが小さい）のDFが最後尾ライン
        if (attackingDownward)
        {
            return defendingFieldPlayers.Max(p => p.Position.Row);
        }
        else
        {
            return defendingFieldPlayers.Min(p => p.Position.Row);
        }
    }

    public bool IsOffside(GameState state, TeamType attackingTeam, Position targetPosition)
    {
        int? offsideLine = GetOffsideLineRow(state, attackingTeam);
        if (!offsideLine.HasValue)
        {
            return false;
        }

        bool attackingDownward = (state.Half == 1 && attackingTeam == TeamType.TeamA) ||
                                 (state.Half == 2 && attackingTeam == TeamType.TeamB);

        // ルール: 受け手が「相手最後尾DF（GK除外）の横ライン」よりも相手ゴール側のマスにいた場合オフサイド。
        // 同じ高さ（同一Row）はオフサイドではない。
        if (attackingDownward)
        {
            return targetPosition.Row > offsideLine.Value;
        }
        else
        {
            return targetPosition.Row < offsideLine.Value;
        }
    }
}
