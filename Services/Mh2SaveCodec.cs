using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using MH2QuestChecklist.ViewModels;

namespace MH2QuestChecklist.Services;

/// <summary>
/// One character slot read from a MH2 (dos) memory card save (.psu).
/// Slot 1 = play00.bin, 2 = play01.bin, 3 = play02.bin.
/// </summary>
public sealed class SaveSlot : ObservableObject
{
    private const int BitmapOffset = 0x200C;
    // Quest bitfield bytes 0x200C-0x203B: village bits 0-128 plus the Town
    // extension (see savedata/MH2-QUEST-FLAGS.md section 8).
    private const int BitmapLength = 48;

    private readonly byte[] _payload;
    private bool _japanese;

    public SaveSlot(int slot, string name, byte[] payload)
    {
        Slot = slot;
        Name = name;
        _payload = payload;
    }

    public int Slot { get; }

    public string Name { get; }

    public bool Japanese
    {
        get => _japanese;
        set
        {
            if (_japanese == value)
            {
                return;
            }

            _japanese = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(Label));
        }
    }

    public string Label => _japanese
        ? $"スロット{Slot:00}: {Name}"
        : $"Slot{Slot:00}: {Name}";

    /// <summary>Quest completion bit N of the character bitmap (payload 0x200C, LSB first).</summary>
    public bool IsQuestCompleted(int bit)
    {
        if (bit < 0 || bit >= BitmapLength * 8)
        {
            return false;
        }

        var offset = BitmapOffset + bit / 8;
        if (offset >= _payload.Length)
        {
            return false;
        }

        return (_payload[offset] & (1 << (bit % 8))) != 0;
    }
}

/// <summary>
/// Decrypt-only reader for MH2 (dos, SLPM-66280) .psu memory card saves.
/// Cipher: 16-bit XOR stream, key schedule K[0]=seed (header offset 2),
/// K[i+1] = (176 * K[i]) mod 0xFF53 (K==0 -> 1), one u16 LE word at a time
/// over the payload starting at file offset 0x40. See savedata/MH2-SAVE-ANALYSIS.md.
/// </summary>
public static class Mh2SaveCodec
{
    private const ushort Modulus = 0xFF53;
    private const ushort Multiplier = 176;
    private const uint ModeRoot = 0x842F;
    private const uint ModeDir = 0x8427;
    private const uint ModeFile = 0x8497;

    private static readonly Regex PlayFilePattern = new("^play(\\d\\d)\\.bin$", RegexOptions.IgnoreCase | RegexOptions.Compiled);

    /// <summary>Character slots found in the save, sorted by slot number.</summary>
    public static List<SaveSlot> ReadSlots(byte[] psu)
    {
        var slots = new List<SaveSlot>();
        foreach (var (name, data) in ReadEntries(psu))
        {
            var match = PlayFilePattern.Match(name);
            if (!match.Success || !int.TryParse(match.Groups[1].Value, out var fileNumber) || fileNumber > 98)
            {
                continue;
            }

            var payload = DecryptPlayFile(data);
            if (payload == null || payload.Length < 0x2100)
            {
                continue;
            }

            var characterName = Encoding.ASCII
                .GetString(payload, 0x18, 16)
                .TrimEnd('\0')
                .Trim();
            if (characterName.Length == 0)
            {
                continue;
            }

            slots.Add(new SaveSlot(fileNumber + 1, characterName, payload));
        }

        slots.Sort((a, b) => a.Slot.CompareTo(b.Slot));
        return slots;
    }

    /// <summary>Decrypts a playNN.bin entry; returns the plaintext payload or null when not a valid play file.</summary>
    private static byte[]? DecryptPlayFile(byte[] raw)
    {
        if (raw.Length < 0x48)
        {
            return null;
        }

        // play0x.bin files carry version 0x0001 in the header.
        if (ReadUInt16(raw, 0) != 0x0001)
        {
            return null;
        }

        var seed = ReadUInt16(raw, 2);
        var plain = new byte[raw.Length - 0x40];
        Buffer.BlockCopy(raw, 0x40, plain, 0, plain.Length);
        XorWords(plain, seed);
        return plain;
    }

    private static void XorWords(byte[] buffer, ushort seed)
    {
        var key = seed;
        for (var i = 0; i + 1 < buffer.Length; i += 2)
        {
            var word = (ushort)(ReadUInt16(buffer, i) ^ key);
            buffer[i] = (byte)(word & 0xFF);
            buffer[i + 1] = (byte)(word >> 8);
            key = key == 0 ? (ushort)1 : (ushort)((Multiplier * key) % Modulus);
        }
    }

    /// <summary>
    /// Walks the .psu container. Each entry is a 0x200 byte header
    /// (mode u32, size u32, name[32] at 0x40) followed by its data;
    /// entries are padded up to the next header, so after a file we scan
    /// forward for the next plausible header.
    /// </summary>
    private static IEnumerable<(string Name, byte[] Data)> ReadEntries(byte[] psu)
    {
        var offset = 0;
        var length = psu.Length;
        while (offset + 0x200 <= length)
        {
            var mode = BitConverter.ToUInt32(psu, offset);
            var name = ReadName(psu, offset + 0x40);
            if (mode is not (ModeRoot or ModeDir or ModeFile) || name.Length == 0)
            {
                offset += 4;
                continue;
            }

            if (mode != ModeFile)
            {
                offset += 4;
                continue;
            }

            var size = BitConverter.ToUInt32(psu, offset + 4);
            var dataEnd = offset + 0x200 + (int)size;
            if (dataEnd > length)
            {
                offset += 4;
                continue;
            }

            var data = new byte[size];
            Buffer.BlockCopy(psu, offset + 0x200, data, 0, (int)size);
            yield return (name, data);

            // Skip past the data plus any tail padding: find the next header.
            var scan = dataEnd;
            var found = false;
            while (scan + 0x200 <= length)
            {
                var nextMode = BitConverter.ToUInt32(psu, scan);
                var nextName = ReadName(psu, scan + 0x40);
                if (nextMode is (ModeRoot or ModeDir or ModeFile) && nextName.Length > 0)
                {
                    offset = scan;
                    found = true;
                    break;
                }

                scan += 4;
            }

            if (!found)
            {
                yield break;
            }
        }
    }

    private static string ReadName(byte[] psu, int offset)
    {
        var end = offset;
        var limit = offset + 32;
        while (end < limit && psu[end] != 0)
        {
            end++;
        }

        return Encoding.ASCII.GetString(psu, offset, end - offset).Trim();
    }

    private static ushort ReadUInt16(byte[] buffer, int offset)
        => (ushort)(buffer[offset] | (buffer[offset + 1] << 8));
}
