namespace GreylingHunt.Configurations
{
    public struct HnSPlayerConfig
    {
        public int seekers;
        public int hiders;
    }

    public sealed class Configuration
    {
        private static Configuration instance = null;

        private Configuration()
        {
        }

        public static Configuration Instance
        {
            get
            {
                if (instance == null)
                {
                    instance = new Configuration();
                }

                return instance;
            }
        }

        public int minPlayersToStart { get; set; } = 2;

        public HnSPlayerConfig hnsPlayerConfiguration3 = new() {seekers = 1, hiders = 1};
        public HnSPlayerConfig hnsPlayerConfiguration5 = new() {seekers = 2, hiders = 3};
        public HnSPlayerConfig hnsPlayerConfiguration8 = new() {seekers = 3, hiders = 5};

        public int startRoundTimer = 10;
        public int maxSearchTime = 300;
        public int seekerWaitTime = 30;
        public int warmupTime = 60;
        public float hiderSpawnRadius = 30;
    }
}