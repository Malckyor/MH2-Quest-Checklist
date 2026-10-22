using System;
using System.Collections.Generic;
using System.Linq;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.Services;

/// <summary>
/// Maps Town (online) quest completion bits to quests.
/// Layout measured against real saves (see savedata/MH2-QUEST-FLAGS.md section 8):
///
///   the same one bitfield, bit = 129 + global position. 198 slots total
///   (bits 129-326): exams block has 2 extra slots without a quests.json
///   counterpart (probably 3AAB and/or event quests, which never mark
///   completion). Ordering verified per category against real saves:
///     practice/free_low/free_high/defense/tournament: quest ID ascending
///     exams: quests.json array order, with 2 skipped slots after index 16
///
/// Verified with 3 single-bit datapoints (2B0B=161, 2B1A=176, 2F01=244)
/// plus category counts in a real endgame save.
/// </summary>
public static class TownQuestMap
{
    private const int TownBase = 129;
    private static readonly string[] TownOrder =
    {
        "practice",
        "free_low",
        "free_high",
        "exams",
        "defense",
        "tournament",
    };

    public static List<VillageQuestMap.QuestBit> Build(QuestsFile file)
    {
        var map = new List<VillageQuestMap.QuestBit>();
        var online = file.Groups.FirstOrDefault(g => g.Key == "online");
        if (online == null)
        {
            return map;
        }

        var bit = TownBase;
        foreach (var categoryKey in TownOrder)
        {
            var quests = QuestIds(online, categoryKey).ToList();
            // exams: ordem interna do save = ordem do json; ha 2 slots
            // extras sem contraparte no json entre as posicoes 16 e 17
            // (candidatos: 3AAB e/ou events, que nao marcam no save).
            // Demais categorias: ID crescente.
            if (categoryKey != "exams")
            {
                quests = quests.OrderBy(ToId).ToList();
            }
            for (var i = 0; i < quests.Count; i++)
            {
                if (categoryKey == "exams" && i == 17)
                {
                    bit += 2;
                }
                map.Add(new VillageQuestMap.QuestBit(bit++, categoryKey, quests[i]));
            }
        }

        return map;
    }

    private static IEnumerable<string> QuestIds(QuestGroupModel group, string categoryKey)
    {
        var category = group.Categories.FirstOrDefault(c => c.Key == categoryKey);
        if (category == null)
        {
            return Enumerable.Empty<string>();
        }

        return category.Quests
            .Where(q => !string.IsNullOrWhiteSpace(q.Hex))
            .Select(q => q.Hex);
    }

    private static int ToId(string hex)
        => Convert.ToInt32(hex, 16);
}
