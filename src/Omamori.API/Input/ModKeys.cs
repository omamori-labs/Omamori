using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Input.Bindings;

namespace Omamori.API
{
    public sealed class ModKeys : IEquatable<ModKeys>
    {
        public static readonly ModKeys NONE = new ModKeys();

        public IReadOnlyList<InputKey> Keys { get; }

        public bool IsNone => Keys.Count == 0;

        public ModKeys(params InputKey[] keys)
        {
            Keys = [.. keys.Where(k => k != InputKey.None).Distinct().Order()];
        }

        public KeyCombination ToCombination() => IsNone ? new KeyCombination(InputKey.None) : new KeyCombination([.. Keys]);

        public static ModKeys From(KeyCombination combination) => combination.Keys.IsDefault ? NONE : new ModKeys([.. combination.Keys]);

        public bool Equals(ModKeys? other) => other is not null && Keys.SequenceEqual(other.Keys);

        public override bool Equals(object? obj) => Equals(obj as ModKeys);

        public override int GetHashCode()
        {
            var hash = new HashCode();

            foreach (var key in Keys)
                hash.Add(key);

            return hash.ToHashCode();
        }

        public override string ToString() => string.Join(',', Keys.Select(k => (int)k));
    }
}
