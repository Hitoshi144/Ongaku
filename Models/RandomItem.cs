using Ongaku.Interfaces;

namespace Ongaku.Models {
    public class RandomItem : IRandomInterface {
        public object Obj { get; set; }
        public double Chance { get; set; }

        public RandomItem(object obj, double chance)
        {
            Obj = obj;
            Chance = chance;
        }
    }
}
