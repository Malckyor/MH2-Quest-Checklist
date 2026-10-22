# MH2 (dos) – Completed Quest Flags in the Savedata

Continuation of `MH2-SAVE-ANALYSIS.md` (encryption already solved). This file
documents the location of the completed-quest flags in the `playNN.bin` files
(decrypted payload).

Analysis date: comparison of save `BISLPM-66280MH2 (786F1CE3).psu`
(Alexia, pre-quest) with `BISLPM-66280MH2 blue (3016026A).psu` (Alexia, after
completing the quest **0x03EF "Blue Bird Watching"**, blue yian kut-ku,
village/free). Reference characters for cross-validation: Malckyor (play00)
and Pandora (play01) from the same save.

## 1. Main conclusion

**The village's completed quests are stored in a bitmap in the payload
starting at `0x200C`, 1 bit per quest, LSB first:**

```
bit N  ->  byte = 0x200C + N/8,  mask = 1 << (N%8)
quest 0x03EF (free)   ->  bit 36  (byte 0x2010, mask 0x10)
quest 0x1389 (urgent) ->  bit 119 (byte 0x201A, mask 0x80)
quest 0x03F3 (free)   ->  bit 42  (byte 0x2011, mask 0x04)
quest 0x03F4 (free)   ->  bit 43  (byte 0x2011, mask 0x08)
quest 0x041A (free)   ->  bit 81  (byte 0x2016, mask 0x02)
quest 0x0419 (free)   ->  bit 80  (byte 0x2016, mask 0x01)
```

Evidence: between save pairs, exactly 1 new bit in the bitmap per completed
quest (0x03EF, 0x1389, 0x03F3, 0x03F4, 0x041A, 0x0419).

### 1.1 Internal list layout (CONFIRMED by 0x1389)

```
bits 0-31    basics (32 quests, order = quests.json: Alexia bits 0-17
             = json basics #0-17 exactly)
bits 32-118  free   (87 quests: 32+87 = 119)
bits 119-127 urgent (9 quests; bit 119 = 0x1389 = urgent #0)
bit  128     defense (1 quest, 0x1772)
total 129    (Pandora: exactly 129 bits set = 100% village)
```

- 0x1389 = bit 119 = urgent position 0. Sorted by ID, 0x1389 is the first
  urgent (1389 < 138A < ... < 1771). **The internal urgent order = sorted
  by ID** — the quests.json order (urgent #0 = 0x1771) does NOT work for
  mapping.
- 0x03EF = bit 36 = free position 4; 0x03F3 = bit 42 = free position 10;
  0x03F4 = bit 43 = free position 11 (adjacent variant pair).
- Alexia had bits 117, 118 (free positions 85/86) set in the pre-quest save
  — under the model below = 043E/043F "Getting to Know the Jungle"
  (gathering, 1★, easy) — consistent with her progress.

### 1.1.1 Internal order model of free (CONFIRMED in the tail)

Measured data-points: 03EF@4, 03F3@10, 03F4@11, 041A@49, 0419@48
(bits 36, 42, 43, 81, 80); positions 85/86 = 043E/043F and 44/46 = 03F5/03F7
(Alexia's pre-existing bits, consistent).

```
free internal = [03E9, 03EA, 03EC, 03EE, 03EF, 03F0, 03F1, 03F2,
                 03EB, 03ED,                     <- variants moved to 8/9
                 03F3, 03F4, 03F5, ..., 043F]    <- 03F3..043F sorted by ID
```

**Sorted by ID, except 03EB and 03ED (duplicated variants of 03EA/03EC)
moved to positions 8 and 9.** Tail 03F3..043F = pure ID order
(75 quests, positions 10-86), confirmed in 4 datapoints spread around
(03F3/03F4 at positions 10/11, 0419/041A at 48/49) + tips 85/86.

Note: the variant pair 0419 (warm) / 041A (breeding) is NOT moved — it sits
at adjacent sorted positions. The 03EB/03ED displacement is an exception
(positions 8/9), not a general variant rule; still lacks direct
verification (quests not yet available to Alexia).

Predicted bits (bit = 32 + position):

```
03E9=32  03EA=33  03EC=34  03EE=35  03EF=36* 03F0=37  03F1=38  03F2=39
03EB=40  03ED=41  03F3=42* 03F4=43* 03F5=44  03F6=45  03F7=46  03F8=47
03F9=48  03FA=49  03FB=50  03FC=51  03FD=52  03FE=53  03FF=54  0400=55
```

**Tail formula: for IDs 03F3..043F, bit = ID - 0x3C9 (= ID - 969).**
Examples: 03F3-3C9=42* 03F4-3C9=43* 0419-3C9=80* 041A-3C9=81*
043E-3C9=117 043F-3C9=118.

Urgent (bits 119-127, sorted by ID; 1389=119* measured):

```
1389=119* 138A=120 138B=121 138C=122 138D=123 138E=124 138F=125
1390=126 1771=127      defense: 1772=128
```

Pending verifications (expected bit): 03E9→32, 03EC→34, 03EE→35,
03EB→40, 03ED→41, 03F0→37, 03F1→38, 03F2→39, 03F5→44, 03F6→45,
03FD→52 (desert), 040E→64 (swamp).

### 1.2 Cross-validation (3 characters)

Bitmap in `0x200C..0x201F` (4 chars of the same save + Alexia post-quest):

```
offset  Malckyor   Pandora    Alexia(pre)  Alexia(post)
200C    1f 00 fc    ff ff ff   ff ff 03     ff ff 03
2010    f9+FFx12    FFx12+01   bf 00 50     ff 00 50
        7f fc ff
        ff ff ff
        ff ff ff
        f3 ff ff
        ff ff bb
        bf 7b c7
        fb c4 6e
        ef 5c fd
        ff 9f 6f
        73
```

- **Pandora**: exactly 129 bits set (`0x200C..0x201B` = FF, byte
  `0x201C` = 0x01) = **100% of the village's 129 quests**. Confirms the
  list size: 32 basics + 87 free + 9 urgent + 1 defense (identical to
  `data/quests.json`).
- **Malckyor**: bits 0-4 and 18-31 (did the first 5 tutorials + all
  Hunting Exercises + Martial Arts, skipped tutorials 5-17), bits 32-127
  all set (free + urgent complete) + bit 128 (defense). Matches the
  profile.
- **Alexia**: bits 0-17 = exactly the 18 "Hunter Basics" of quests.json;
  bits 32-39 = first 8 positions of the internal free list; bits 44, 46 =
  positions 12 and 14 of the free list; bit 36 (position 4) only appeared
  in the "blue" save = quest 0x03EF.

### 1.3 Open problem: internal order of free

Urgent = sorted by ID (0x1389 = bit 119 = #0). Free is not: 0x03EF is at
position 4 (bit 36), 2 positions before ID order. The basics order
(bits 0-31) matches quests.json 1:1. **Mapping bit -> quest of free
requires more save pairs** (see section 5).

## 2. Second bitmap (probably Town/online) – hypothesis

Region `0x20E8`..`0x210A` (~35 bytes) contains a second bitmap:

- Pandora (online character) has 226 bits set there, far more than
  Malckyor (181) and Alexia (9).
- Alexia gained no bit in this region in the village quest (expected, if
  the region is Town-exclusive).

Still without a confirmed datapoint. Map with Town (online) quests using
the same method of section 1. Events stay out of scope as decided.

## 3. All changes of play02.bin between the two saves

Full diff (115 bytes in 18 blocks), classified:

### 3.1 Quest flag (the find)

| Offset | Before | After | Meaning |
|--------|--------|-------|---------|
| 0x2010 | 0xBF | 0xFF | **village bitmap bit 36 = quest 0x03EF completed** |

### 3.2 Probable monster-hunted flag (encyclopedia)

| Offset | Before | After | Meaning |
|--------|--------|-------|---------|
| 0x0083 | 0x00 | 0x10 | bit of "blue kut-ku hunted" in region 0x74-0xB0 (monster bitfield). Consistent: Malckyor lacks this bit, Pandora has it |

### 3.3 Counters (all changed on quests)

| Offset | Type | Change | Note |
|--------|------|--------|------|
| 0x29A4 | u32 | 50 -> 51 -> 52 | counter of completed quests or total hunts (Malckyor 599, Pandora 625); +1 per quest |
| 0x204C | u16 | +665 / +278 | time counter (delta varies per session; in play00 the deltas diverge) |
| 0x273C | u16 | +665 / +278 | playtime in seconds (665s and 278s = duration of each quest) |
| 0x2CA4 | u16 | 0x45 -> 0x47 | +2 per quest (also +2 on quest 03EF); unidentified |
| 0x2CCE | u16 | +1 per quest (5->6); unidentified |
| 0x23EC | u16 | 0x18 -> 0x1A | +2 on the first quest; unchanged on 1389 |
| 0x242E | u16? | 0x00 -> 0x01 | unidentified |
| 0x222E/0x232E | ? | 0x00 -> 0x6A | on 1389 changed 0x2242/0x2342 (0x00 -> 0x64) and 0x2442 (0x00 -> 0x01): tables every 0x100 with entries appearing per quest; unidentified |
| 0x2CCC | u16 | +1 per quest (0x3F -> 0x40); unidentified |
| 0x36B2+ | 8B records | (u16 type, u16 id, u16 qty, u16 value 1000/1500/2000) | log of items sold/obtained with price? new region activated on 1389 |
| 0x370C-0x377F | sparse bits | new bits on 1389 | unidentified (same family as 0x36B2+?) |
| 0x74-0xB0 | bitfield | quest 03EF: 1 bit (0x83:0x10); quest 1389: 7 bits (0x76:0x04, 0x7A:0x04, 0x7B:0x10, 0x7D:0x02, 0x87:0x02, 0x96:0x40, 0x99:0x08) | mixed encyclopedia (monsters + items obtained for the first time); new carves from 1389 = more bits |
| 0x2B0C/0x2B4C/0x2B58/0x2B68/0x2B71 | ? | +12/+12/+6/+6/+32 on 1389 | probably farm points; unidentified |

### 3.4 Known noise (not quests)

| Offset | Change | Meaning |
|--------|--------|---------|
| 0x0000-0x000F | all | save timestamp + nonce |
| 0x0070 | u32 16278 -> 18740 | zenny |
| 0x27DD-0x27E0 | `00 14 14 14` -> `06 08 08 00` | the ~4 inventory items (quantities) |
| 0x2070-0x2082 | 5 new records (`72 01 01 00`, `73 01 02 00`, `74 01 02 00`, `38 01 02 00`, `39 01 01 00`) | items obtained in the quest (record tables u16 id + u16 qty in 0x2050/0x2070) |
| 0x21A2-0x21E1 | 30x u16 0xE676 -> 0x00B0 | unknown table, 30 entries change together every quest (Malckyor = 0xC62B, Pandora = 0x7900 – per-character value that changes with progress) |

## 4. Structure confirmed per region

| Payload | Content |
|---------|---------|
| 0x00-0x0F | timestamp (8B) + nonce (8B) |
| 0x10 | slot start: 01 00, fixed id 14 0f 44 3b 37 ff |
| 0x18 | character name (16B ASCII) |
| 0x70 | zenny u32 |
| 0x74-0xB0 | bitfield ~480 bits (probably monster-hunted encyclopedia; bit 0x10 of 0x83 = blue kut-ku) |
| 0xCC | item box (12-byte records `01 06 XX YY`) |
| 0x200C-0x201C | **village quest bitmap (129 quests, 1 bit each)** |
| 0x204C | u16 time counter |
| 0x2050/0x2070 | tables of 8 records (u16 id + u16 qty) – items/rewards |
| 0x20A0+ | 12-byte item-box-style records |
| 0x20E8-0x210A | **second bitmap (probably Town/online quests)** |
| 0x212C+ | guild card: member ASCII text (Malckyor: "Squad Hunters of Oblivion: Member 0147...") |
| 0x2196-0x21E1 | u16 table (6 valid values + padding up to 30 slots) |
| 0x23E4+ | slots (u16 id + u16 qty) of items |
| 0x273C | u32/u16 playtime in seconds |
| 0x27C0-0x2800 | inventory item quantities |
| 0x29A4 | u32 quest/hunt counter |
| 0x2CA8 | pet name (ASCII, e.g. "Poogie") |

## 5. Protocol to map bit -> quest (next step)

Status: layout confirmed ([basics 32][free 87][urgent 9][defense 1],
base 0x200C). Internal order of free = model of section 1.1.1 (sorted by
ID with 03EB/03ED moved to 8/9). Missing spot-checks and the Town
(online).

Data-points collected (Alexia):

| Quest | List | Bit | Internal position |
|-------|------|-----|-------------------|
| 0x03EF Blue Bird Watching | free | 36 | free #4 |
| 0x1389 Hunting a Daimyo Hermitaur! | urgent | 119 | urgent #0 |
| 0x03F3 The Pink Ape's Territory (cold) | free | 42 | free #10 |
| 0x03F4 The Pink Ape's Territory (breeding) | free | 43 | free #11 |
| 0x041A A Gluttonous Ape (breeding) | free | 81 | free #49 |
| 0x0419 A Gluttonous Ape (warm) | free | 80 | free #48 |

Next target quests (Alexia, 1 export per quest, filename with the ID),
to check the model (expected bits):

1. `03E9` Explore the Jungle! (warm) — bit 32 (position 0)
2. `03EC` Hunt the Boar King! (warm) — bit 34 (position 2)
3. `03EE` Tracking the Bird's Call (warm) — bit 35 (position 3)
4. `03EB` The Blue Hunter's Domain (breeding, variant) — bit 40 (position 8)
5. `03ED` Hunt the Boar King! (cold, variant) — bit 41 (position 9)
6. `03FD` Leaping in the Desert — bit 52 (start of desert under the model)
7. `040E` A Splash of Red and Poison — bit 64 (start of the swamp)

After that: 1 quest per remaining locale (snow 0420 = bit 77, volcano
0429 = bit 86, forest 0434 = bit 97, tower 0439 = bit 102) to lock the
sorted tail, and then the Town (online) quests for bitmap 0x20E8.

Rules for a clean diff:
- 1 quest per save/export (batches OK if you note exactly which IDs).
- Avoid failing/abandoning quests, buying/selling, changing equipment.
- Repeating the same quest 1x (second completion): expect only counters.

## 6. Open questions

1. Exact internal order of the free list (hypothesis: base by ID, variants
   later — confirm with the 7 datapoints of section 5).
2. Bits 129-134 in Malckyor ("mystery" of the old version of this list) and
   138-143 — now fit as a bitfield extension (Town misc, see section 8.0):
   129–142 = Town misc region (14 bits, probably `practice`), 143+ = Town
   quests per-quest. Pandora/Alexia zero = never touched Town in that part.
3. Semantics of counters 0x222E..0x2442 (tables every 0x100),
   0x2CA4 (+2/quest), 0x2CCE, 0x2CCC, 0x2B0C+ (farm?).
4. Exact function of counter 0x204C (deltas diverge between play00 and play02).
5. Confirm bitmap 0x20E8 = Town with online datapoints.
6. Table of 30 u16 in 0x21A2 (changes whole per quest, per-char value).
7. Region 0x36B2+ (records type/id/qty/price 1000/1500/2000) and 0x370C+ —
   sale/acquisition log? Activated for the first time on quest 1389.
8. Region 0x74-0xB0: mixed encyclopedia monsters+items (1 bit on 03EF,
   7 bits on 1389) — map individual monsters/items later.

## 7. Tool

`mh2_save_tool.py unpack <save.psu> <outdir>` generates the `.dec`
(decrypted payload) for diffing. Analysis workflow:

```
python mh2_save_tool.py unpack "new save.psu" work_new
python - <<EOF
a = open('work_blue/play02.bin.dec','rb').read()
b = open('work_new/play02.bin.dec','rb').read()
base = 0x200C
for i in range(len(a)):
    if a[i]!=b[i]:
        n = (i-base)*8
        print(f'byte 0x{i:04X}: {a[i]:02X}->{b[i]:02X}  bits ' +
              ', '.join(str(n+j) for j in range(8) if b[i]&(1<<j) and not a[i]&(1<<j)))
EOF
```

## 8. Town (online) — **CONFIRMED: per-quest flag exists**, in the bitfield extension (0x201C+)

> **Correction (update):** this section's earlier conclusion
> ("no per-quest Town flag exists") was **wrong** — the diff of the
> `ale1 → ale2` step missed the gain of bit 176 (byte `0x2022`). With 4
> verified datapoints, the per-quest mapping is confirmed.

Datapoints (6 saves / 4 known Town completions):

| Pair | Character (file) | New quest |
|------|------------------|-----------|
| `BISLPM-66280MH2 (786F1CE3).psu` → `mal io 2f01 (AA40311F).psu` | Malckyor (play00) | `2F01` (free_high pos 32, sorted by ID) |
| `ale bull 2afd (2D647FFB).psu` (baseline: 2AFD already done) | Alexia (play02) | — |
| `ale bull 2afd` → `ale bull 2b1a (6F462FC8).psu` | Alexia (play02) | `2B1A` (free_low pos 33) |
| `ale bull 2b1a` → `ale 2b0b (E89A0038).psu` | Alexia (play02) | `2B0B` (free_low pos 18) |

### 8.0 Confirmed bitfield layout (base `0x200C`, bit 0 = bit 7 of the first byte)

The village bitmap **continues past bit 128** — the same per-... bitfield
holds the Town quests. Ordering = **ID ascending** within the category
(same as the village "free", section 1).

| Bits of `0x200C` | Region | Status |
|------------------|--------|--------|
| 0–128 (129) | **Village** quests (bitmap already implemented) | confirmed (section 1) |
| **129–326 (198)** | **ALL Town quests** (see §8.0.2) | confirmed by 3 flips + counts |

Formula (global position = index into the single list of all Town quests,
concatenated by category in the order below, each `practice`-style ordered
as noted):

```
bit = 129 + global position
byte = 0x200C + (bit / 8) ; mask = 1 << (bit % 8)

Category order and bit ranges (198 slots, bits 129-326):
practice     129-142 (14 quests, IDs 2711-2779)
free_low     143-211 (69 quests, IDs 2AF9-2B3D)   = 143 + pos
free_high    212-291 (80 quests, IDs 2EE1-2F30)   = 212 + pos
exams        292-315 (24 slots: 17 exams (3A99-3AAF) + 2 slots WITHOUT
                       a quests.json counterpart (bits 309/310) + 5 exams
                       (3AB0, 3AA5, 3AA9, 3AAC, 3AAD) in json order)
defense      316-325 (10 quests, IDs 4E21-4E2A)
tournament   326     (1 quest,  ID 61A9)
```

### 8.0.2 Global model: `bit = 129 + pos` over all Town quests

All the Town quests (practice 14, free_low 69, free_high 80, exams 22,
defense 10, tournament 1) form a single list inside the same bitfield —
each quest = `bit 129 + global position`. The old formulas
`free_low = 143+pos` / `free_high = 212+pos` are special cases and remain
valid.

**Internal order PER CATEGORY** (checked against the saves and the user's
in-game verification, 2026-10-22):

- `practice`, `free_low`, `free_high`, `defense`, `tournament`: ID
  ascending. (practice confirmed in-game: exact gaps 2717/2718/2719;
  free_low/free_high confirmed by the 4 flips; pure json order
  **falsified** — `2AFD`/`2B0B`/`2B1A` land on wrong bits.)
- `exams`: **the quests.json array order** (≠ ID!) — the user confirmed
  in-game that **all 22 exams are complete** in Malckyor's save;
  therefore the 2 empty slots (309/310) are NOT exams: they are 2 extra
  slots with no json counterpart, between positions 16 and 17 of the
  block (candidates: `3AAB`, which exists in the game but not in the
  json, and/or an **event** quest — and events "do not mark in the
  save", user report). Skip: `exams idx 0-16` → bits 292-308, `skip 2
  bits` (309/310 with no quest), `exams idx 17-21` → bits 311-315.
  Result: Malckyor **exams 22/22** ✓, tournament **1/1** ✓ (`61A9` at
  bit 326 — not 324), defense **6/10** (prediction: not done 4E21, 4E24,
  4E27, 4E28 — not confirmable now, availability-gated quests).
- This is the **only** layout consistent with all the facts: free_high
  ends at 291 (80 quests verified), and the 2 zero bits of 309/310 fall
  inside the exams block with the 22 exams set.

**Events**: do not mark in the save — out of tracking; if 1 or 2 of the
extra slots are event quests, they stay empty forever (consistent with
the saves: Pandora/Alexia = 0 everywhere).

**Independent verifications of the model** (besides the 4 datapoints of
8.0.1):

- Malckyor — in-game verification by the user (2026-10-22):
  practice **11/14** ✓ confirmed (missing 2717/2718/2719);
  exams **22/22** ✓ confirmed (all mandatory and completed);
  tournament **1/1** ✓ confirmed (`61A9`, bit 326);
  defense **6/10** — availability-gated quests, **not confirmable now**
  (prediction: done 4E22, 4E23, 4E25, 4E26, 4E29, 4E2A).
- Bits 309/310 (central piece): **2 extra slots with no json
  counterpart** inside the exams block (always empty in all 3
  characters) — candidates: `3AAB` (exists in the game, not in the
  json) and/or an event quest.
- The old "mystery" bits 325/326: **solved** — with the 2-slot skip,
  defense = 316-325 and tournament = 326 (Malckyor set on both ✓).
  Nothing remains beyond bit 326.
- Save-vs-in-game check before the correction (history): mapping exams
  by ID + tournament at 324 produced "3AAC/3AAD not completed" and a
  direct disagreement with the user.

### 8.0.1 Evidence (verified byte-by-byte on the 6 saves)

| Quest | Cat | pos | bit | byte | mask | Gained in |
|-------|-----|-----|-----|------|------|-----------|
| `2AFD` (Hunt the Boar King!) | free_low | 4 | 147 | `0x201E` | 0x08 | ale1+ (Malckyor always set) |
| `2B0B` (Leaping in the Desert) | free_low | 18 | 161 | `0x2020` | 0x02 | ale2 → ale3 |
| `2B1A` (A Reckless Rush) | free_low | 33 | 176 | `0x2022` | 0x01 | ale1 → ale2 |
| `2F01` | free_high | 32 | 244 | `0x202A` | 0x10 | malA → malB (Malckyor `0xC7`→`0xD7`) |

Controls (coherence): `2AF9` (pos 0, bit 143), `2AFE` (pos 5, bit 148)
and `2AFF` (pos 6, bit 149) **zero** across Alexia's whole chain (never
completed) and set in Malckyor (100% mode town). The mundane 15-bit
difference between `2B1A` (176) and `2B0B` (161) is exactly the sorted
position gap (33 − 18).

### 8.1 What is NOT Town tracking (discarded)

- **`patch.bin` and the header `BISLPM-66280MH2.bin`**: byte-identical
  in both pairs. No Town data there.
- **Village bitmap (0x200C)**: unchanged (expected).
- **Second bitmap (`0x20E8-0x210A`, hypothesis of section 2)**:
  **FALSIFIED** — does not change in any of the 3 Town completions
  (nor in Alexia's first, nor second, nor Malckyor's). The region is
  per-character (Pandora ~226 bits, Malckyor ~181, Alexia ~9) but does
  not track completed Town quests.
- **Table `0x21A2-0x21E1` (32x u16)**: flips **once per character**
  (pre value `0xC62B`/`0xE676`/`0x7900` → `0x00B0`) at some progress
  milestone — not per-quest (Alexia was already `0x00B0` in both
  "ale bull" saves).
- **Area `0x1AC0+`**: it is the item box (records `u16 id + u16 qty` LE,
  e.g. `2F 01 35 00` = item 0x012F qty 53). NOT a quest list — fake
  lead resolved.
- **New records in `0x2080-0x20A0`** (e.g. Malckyor: `0167 x2`,
  `0169 x2`, `0168 x1`): records of **items/rewards** (`u16 id +
  u16 qty`), same pattern as 0x2070 from the village analysis
  (section 3.4). The ids do not exist in quests.json — they are not
  quest ids.

### 8.2 What changes ON a Town completion (aggregates, 2 datapoints each)

| Offset | 2F01 (Malckyor play00) | 2B1A (Alexia play02) | Semantics |
|--------|------------------------|----------------------|-----------|
| `0x246A` | 61 → 62 (+1) | 3 → 4 (+1) | **Town completions counter (aggregated per character)** |
| `0x2CD0` | 151 → 152 (+1) | 2 → 3 (+1) | **aggregated counter, +1 per Town quest** |
| `0x2CA4` | 96 → 65506 (anomalous) | 81 → 83 (+2) | +2/quest (Alexia); Malckyor suggests another/signed field |
| `0x204C` | +1206 | +278 | playtime (u16/u32) |
| `0x273C` | +... | — | playtime |
| `0x27C7`+ `0x28xx` | yes | yes | session/inventory noise |

- **Important**: `0x29A4` (+1 per VILLAGE quest per section 3.3) does
  **not** change on Town quests — the game keeps village and Town
  counts separate.
- Correction: the old note saying the `2B1A` completion changed **no
  bit** was an incomplete read of the diff — bit 176 (`0x2022` & 0x01)
  was gained in the `ale1 → ale2` step (see section 8.0.1).
- These counters are **secondary effects** (profiles: completions,
  time, resources), not the state tracking; and they are not reliable
  per quest individually — `0x246A`/`0x2CD0` incremented on `2F01` and
  `2B1A` but **not** on `2B0B`.

### 8.3 Implication for the tool

- **Village**: per-quest possible (bitmap bits 0–128 `0x200C`, already
  implemented).
- **Town**: per-quest possible **in the same structure** — global model
  `bit = 129 + pos` (§8.0.2), implemented in `Services/TownQuestMap.cs`
  (order: practice, free_low, free_high, exams, defense, tournament;
  each by ID except exams = json order with a 2-slot skip after index
  16).
- Pending: (1) the 2 unknown slots (309/310) — candidates `3AAB` and/or
  events; (2) in-game confirmation of the defense list (availability-
  gated quests).
