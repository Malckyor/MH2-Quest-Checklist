using System;
using System.Collections.Generic;
using System.Linq;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.Services;

/// <summary>
/// Maps Town (online) quest completion bits to quests.
/// Layout measured against real saves (see savedata/MH2-QUEST-FLAGS.md section 8):
///
///   the same one bitfield, bit = 129 + global position. defense block
///   (bits 316-326) has 1 extra empty slot without a quests.json
///   counterpart (bit 322, between 4E26 and 4E27) — always zero in every
///   known save. Total tracked slots = 197 (bits 129-326): exams block
///   also has 2 extra slots without a quests.json counterpart (bits
///   309/310; probably 3AAB and/or event quests, which never mark
///   completion). Ordering verified per category against real saves:
///     practice/free_low/free_high/defense: quest ID ascending
///     exams: quests.json array order, with 2 skipped slots after index 16
///     tournament (61A9): NO completion flag in the savestate — the Grand
///       Tournament never marks (confirmed: tournament done for Malckyor,
///       but bits 327+ stay zero). Manual tracking only.
///
/// Verified single-bit datapoints: 2B0B=161, 2B1A=176, 2F01=244, 4E27=323,
/// plus category counts in a real endgame save and the defense tail
/// {4E22,4E23,4E25,4E26,4E28,4E29,4E2A} = {317,318,320,321,324,325,326}.
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
            // exams: internal save order = json order; there are 2 extra
            // slots with no json counterpart between positions 16 and 17
            // (candidates: 3AAB and/or events, which never mark in the save).
            // defense: ID order, with 1 extra slot with no json counterpart
            // between 4E26 and 4E27 (bit 322, always zero — measured:
            // completing 4E27 gains bit 323). Remaining categories: ID
            // ascending.
            // tournament (61A9): no flag exists in the save — never marks,
            // manual tracking only.
            if (categoryKey != "exams")
            {
                quests = quests.OrderBy(ToId).ToList();
            }
            for (var i = 0; i < quests.Count; i++)
            {
                if (categoryKey == "tournament")
                {
                    // 61A9 (Grand Tournament): no completion flag in the save
                    // (Malckyor finished the tournament, bits 327+ are all
                    // zero). Never marks — manual tracking only.
                    break;
                }
                if (categoryKey == "exams" && i == 17)
                {
                    bit += 2;
                }
                if (categoryKey == "defense" && i == 6)
                {
                    bit += 1;
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
