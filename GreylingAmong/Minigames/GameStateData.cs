using System.Collections.Generic;

namespace GreylingHunt.Minigames
{
    public class GameStateData
    {
        public GameState GameState;
        public HashSet<string> hiderNameLookup = new();
        public HashSet<string> seekerNameLookup = new();

        public void Serialize(ref ZPackage pkg)
        {
            pkg.Write((int)GameState);
            WriteHashSet(ref pkg, ref seekerNameLookup);
            WriteHashSet(ref pkg, ref hiderNameLookup);
        }

        public void Deserialize(ref ZPackage pkg)
        {
            GameState = (GameState)pkg.ReadInt();
            seekerNameLookup = ReadToHash(ref pkg);
            hiderNameLookup = ReadToHash(ref pkg);
        }

        private HashSet<string> ReadToHash(ref ZPackage pkg)
        {
            int itemCount = pkg.ReadInt();
            HashSet<string> hashset = new HashSet<string>();
            for (int i = itemCount; i-- > 0;)
            {
                hashset.Add(pkg.ReadString());
            }
            return hashset;
        }

        private void WriteHashSet(ref ZPackage pkg, ref HashSet<string> hashSet)
        {
            pkg.Write(hashSet.Count);
            foreach (string item in hashSet)
            {
                pkg.Write(item);
            }
        }

    }
}
