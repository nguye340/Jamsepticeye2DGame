using System.Linq;  // Add this at the top
using UnityEngine;

namespace Systems
{
    public static class CorpseImbueResolver
    {
        public static FruitDefinition ChooseFruitForCorpse(
            DeathType type, 
            FruitInventory inventory, 
            bool isLastLife, 
            bool playerChoseSpecific, 
            FruitDefinition chosen)
        {
            if (inventory == null) 
                return null;

            switch (type)
            {
                case DeathType.Unintentional:
                    return inventory.RemoveOneRandomFruit();

                case DeathType.Intentional:
                    // If player specifically chose a fruit, use that
                    if (playerChoseSpecific && chosen != null && inventory.Contains(chosen))
                    {
                        if (inventory.RemoveOne(chosen))
                        {
                            return chosen;
                        }
                    }
                    // If it's the last life with one fruit, use that fruit
                    else if (isLastLife && inventory.TotalCount() == 1)
                    {
                        var lastFruit = inventory.GetAllTypes().FirstOrDefault();
                        if (lastFruit != null && inventory.RemoveOne(lastFruit))
                        {
                            return lastFruit;
                        }
                    }
                    break;
            }

            return null;
        }
    }
}