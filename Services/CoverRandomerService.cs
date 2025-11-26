using Ongaku.Interfaces;
using Ongaku.Models;

namespace Ongaku.Services {
    public class CoverRandomerService {
        private readonly List<IRandomInterface> randomCovers;

        public CoverRandomerService()
        {
            randomCovers = new List<IRandomInterface> 
            { 
                new RandomItem("assets/RandomCovers/std_cover_01.png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_01 (2).png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_01 (3).png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_01 (4).png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_01 (5).png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_01 (6).png", 0.1),
                new RandomItem("assets/RandomCovers/std_cover_009.png", 0.09),
                new RandomItem("assets/RandomCovers/std_cover_011.png", 0.11),
                new RandomItem("assets/RandomCovers/std_cover_015.png", 0.15),
                new RandomItem("assets/RandomCovers/std_cover_005.png", 0.05),
            };
        }

        public string GetRandomCover()
        {
            var rand = new Random();
            double value = rand.NextDouble();

            double cumulative = 0.0;

            foreach (var item in randomCovers)
            {
                cumulative += item.Chance;
                if (value < cumulative)
                {
                    return item.Obj as string ?? string.Empty;
                }
            }

            return randomCovers.Last().Obj as string ?? string.Empty;
        }
    }
}
