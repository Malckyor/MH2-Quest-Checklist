using System;
using System.Collections.Generic;
using System.Linq;
using MH2QuestChecklist.Models;

namespace MH2QuestChecklist.Services;

/// <summary>
/// Maps village (offline) quest completion bits to quests.
/// Layout measured against real saves (see savedata/MH2-QUEST-FLAGS.md):
///
///   bits 0-31    basics, quests.json order
///   bits 32-118  free, sorted by quest ID, except 03EB/03ED displaced
///                to internal positions 8/9 (bits 40/41)
///   bits 119-127 urgent, sorted by quest ID
///   bit  128     defense (0x1772)
/// </summary>
public static class VillageQuestMap
{
    private const string DisplacedHex1 = "03EB";
    private const string DisplacedHex2 = "03ED";
    private const int DisplacedIndex1 = 8;
    private const int DisplacedIndex2 = 9;

    public static List<QuestBit> Build(QuestsFile file)
    {
        var map = new List<QuestBit>();
        var offline = file.Groups.FirstOrDefault(g => g.Key == "offline") ?? file.Groups.FirstOrDefault();
        if (offline == null)
        {
            return map;
        }

        var bit = 0;
        foreach (var hex in QuestIds(offline, "basics"))
        {
            map.Add(new QuestBit(bit++, "basics", hex));
        }

        foreach (var hex in FreeOrder(QuestIds(offline, "free")))
        {
            map.Add(new QuestBit(bit++, "free", hex));
        }

        foreach (var hex in QuestIds(offline, "urgent").OrderBy(ToId))
        {
            map.Add(new QuestBit(bit++, "urgent", hex));
        }

        foreach (var hex in QuestIds(offline, "defense"))
        {
            map.Add(new QuestBit(bit++, "defense", hex));
        }

        return map;
    }

    /// <summary>Free list internal order: sorted by ID with the two displaced variants re-inserted at 8/9.</summary>
    private static IEnumerable<string> FreeOrder(IEnumerable<string> hexes)
    {
        var sorted = hexes.OrderBy(ToId).ToList();
        var displaced1 = sorted.FirstOrDefault(h => string.Equals(h, DisplacedHex1, StringComparison.OrdinalIgnoreCase));
        var displaced2 = sorted.FirstOrDefault(h => string.Equals(h, DisplacedHex2, StringComparison.OrdinalIgnoreCase));

        if (displaced1 == null || displaced2 == null || sorted.Count < DisplacedIndex2 + 1)
        {
            // Fallback: plain ID order (mapping will be off only if the json changes).
            return sorted;
        }

        var front = sorted
            .Where(h => !string.Equals(h, DisplacedHex1, StringComparison.OrdinalIgnoreCase)
                     && !string.Equals(h, DisplacedHex2, StringComparison.OrdinalIgnoreCase))
            .ToList();
        front.Insert(DisplacedIndex1, displaced1);
        front.Insert(DisplacedIndex2, displaced2);
        return front;
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

    public sealed record QuestBit(int Bit, string CategoryKey, string Hex);
}
