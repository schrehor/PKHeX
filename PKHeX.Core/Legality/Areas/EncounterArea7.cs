﻿using System;
using System.Collections.Generic;
using static System.Buffers.Binary.BinaryPrimitives;

namespace PKHeX.Core
{
    /// <inheritdoc cref="EncounterArea" />
    /// <summary>
    /// <see cref="GameVersion.Gen7"/> encounter area
    /// </summary>
    public sealed record EncounterArea7 : EncounterArea
    {
        public readonly EncounterSlot7[] Slots;

        protected override IReadOnlyList<EncounterSlot> Raw => Slots;

        public static EncounterArea7[] GetAreas(byte[][] input, GameVersion game)
        {
            var result = new EncounterArea7[input.Length];
            for (int i = 0; i < input.Length; i++)
                result[i] = new EncounterArea7(input[i], game);
            return result;
        }

        private EncounterArea7(ReadOnlySpan<byte> data, GameVersion game) : base(game)
        {
            Location = data[0] | (data[1] << 8);
            Type = (SlotType)data[2];

            Slots = ReadSlots(data);
        }

        private EncounterSlot7[] ReadSlots(ReadOnlySpan<byte> data)
        {
            const int size = 4;
            int count = (data.Length - 4) / size;
            var slots = new EncounterSlot7[count];
            for (int i = 0; i < slots.Length; i++)
            {
                int offset = 4 + (size * i);
                var entry = data.Slice(offset, size);
                slots[i] = ReadSlot(entry);
            }

            return slots;
        }

        private EncounterSlot7 ReadSlot(ReadOnlySpan<byte> entry)
        {
            ushort SpecForm = ReadUInt16LittleEndian(entry);
            int species = SpecForm & 0x3FF;
            int form = SpecForm >> 11;
            int min = entry[2];
            int max = entry[3];
            return new EncounterSlot7(this, species, form, min, max);
        }

        public override IEnumerable<EncounterSlot> GetMatchingSlots(PKM pkm, IReadOnlyList<EvoCriteria> chain)
        {
            foreach (var slot in Slots)
            {
                foreach (var evo in chain)
                {
                    if (slot.Species != evo.Species)
                        continue;

                    if (!slot.IsLevelWithinRange(pkm.Met_Level))
                        break;

                    if (slot.Form != evo.Form && slot.Species is not ((int)Species.Furfrou or (int)Species.Oricorio))
                    {
                        if (!slot.IsRandomUnspecificForm) // Minior, etc
                            break;
                    }

                    yield return slot;
                    break;
                }
            }
        }
    }
}
