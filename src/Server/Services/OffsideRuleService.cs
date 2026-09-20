using FootballBoardGame.Server.Models;

namespace FootballBoardGame.Server.Services;

public interface IOffsideRuleService
{
    int? GetOffsideLineCol(GameState state, TeamType attackingTeam);
    bool IsOffside(GameState state, TeamType attackingTeam, Position targetPosition);
}

public class OffsideRuleService : IOffsideRuleService
{
    public int? GetOffsideLineCol(GameState state, TeamType attackingTeam)
    {
        TeamType defendingTeam = attackingTeam == TeamType.TeamA ? TeamType.TeamB : TeamType.TeamA;
        var defendingFieldPlayers = state.Pieces
            .Where(p => p.Team == defendingTeam && !p.IsGoalkeeper)
            .ToList();

        if (!defendingFieldPlayers.Any())
        {
            return null;
        }

        // 左から右 (Col 1 -> 10) へ攻めるか
        bool attackingRightward = (state.Half == 1 && attackingTeam == TeamType.TeamA) ||
                                  (state.Half == 2 && attackingTeam == TeamType.TeamB);

        // 右向きに攻める場合 (Col 1 -> 10): 守備側の最も右 (Colが大きい) DFが最後尾ライン
        // 左向きに攻める場合 (Col 10 -> 1): 守備側の最も左 (Colが小さい) DFが最後尾ライン
        if (attackingRightward)
        {
            return defendingFieldPlayers.Max(p => p.Position.Col);
        }
        else
        {
            return defendingFieldPlayers.Min(p => p.Position.Col);
        }
    }

    public bool IsOffside(GameState state, TeamType attackingTeam, Position targetPosition)
    {
        int? offsideCol = GetOffsideLineCol(state, attackingTeam);
        if (!offsideCol.HasValue)
        {
            return false;
        }

        bool attackingRightward = (state.Half == 1 && attackingTeam == TeamType.TeamA) ||
                                  (state.Half == 2 && attackingTeam == TeamType.TeamB);

        // ルール: 受け手が「相手最後尾DF（GK除外）の縦ライン (Col)」よりも相手ゴール側のマスにいた場合オフサイド。
        // 同じColはオフサイドではない。
        if (attackingRightward)
        {
            return targetPosition.Col > offsideCol.Value;
        }
        else
        {
            return targetPosition.Col < offsideCol.Value;
        }
    }
}
