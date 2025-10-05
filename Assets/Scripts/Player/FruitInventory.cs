using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems;

public class FruitInventory : MonoBehaviour
{
    [SerializeField] private List<FruitStackItem> fruitStacks = new List<FruitStackItem>();

    /// <summary>
    /// Adds a fruit to the inventory
    /// </summary>
    public void AddFruit(FruitDefinition fruit)
    {
        if (fruit == null) return;

        var existingStack = fruitStacks.FirstOrDefault(stack => stack.Fruit == fruit);
        if (existingStack != null)
        {
            existingStack.Count++;
        }
        else
        {
            fruitStacks.Add(new FruitStackItem { Fruit = fruit, Count = 1 });
        }
    }

    //////////////// FOR FRUIT INVENTORY UI - Duplicate to be fixed///////////
    public bool HasFruit(string fruitName)
    {
        return fruitStacks.Any(stack => stack.Fruit.name == fruitName && stack.Count > 0);
    }
    public FruitDefinition GetFruitDefinitionByName(string fruitName)
    {
        var stack = fruitStacks.FirstOrDefault(s => s.Fruit.name == fruitName);
        return stack?.Fruit;
    }

    /// <summary>
    /// Checks if the inventory has any fruits
    /// </summary>
    public bool HasAnyFruit()
    {
        return fruitStacks.Any(stack => stack.Count > 0);
    }
    

    /// <summary>
    /// Returns the total count of all fruits
    /// </summary>
    public int TotalCount()
    {
        return fruitStacks.Sum(stack => stack.Count);
    }

    /// <summary>
    /// Removes and returns one random fruit, or null if empty
    /// Picks uniformly among all available fruits
    /// </summary>
    public FruitDefinition RemoveOneRandomFruit()
    {
        if (!HasAnyFruit()) return null;

        // Create a list of all available fruits with their counts
        var availableFruits = new List<FruitDefinition>();
        foreach (var stack in fruitStacks)
        {
            for (int i = 0; i < stack.Count; i++)
            {
                availableFruits.Add(stack.Fruit);
            }
        }

        // Pick a random fruit
        int randomIndex = Random.Range(0, availableFruits.Count);
        FruitDefinition selectedFruit = availableFruits[randomIndex];

        // Remove one from the stack
        RemoveOne(selectedFruit);

        return selectedFruit;
    }

    /// <summary>
    /// Removes one specific fruit if available
    /// </summary>
    /// <returns>True if a fruit was removed, false otherwise</returns>
    public bool RemoveOne(FruitDefinition fruit)
    {
        if (fruit == null) return false;

        var stack = fruitStacks.FirstOrDefault(s => s.Fruit == fruit);
        if (stack != null && stack.Count > 0)
        {
            stack.Count--;
            if (stack.Count <= 0)
            {
                fruitStacks.Remove(stack);
            }
            return true;
        }
        return false;
    }

    /// <summary>
    /// Removes all fruits from the inventory
    /// </summary>
    public void ClearAll()
    {
        fruitStacks.Clear();
    }

    /// <summary>
    /// Returns a list of all unique fruit types in the inventory
    /// </summary>
    public List<FruitDefinition> GetAllTypes()
    {
        return fruitStacks
            .Where(stack => stack.Count > 0)
            .Select(stack => stack.Fruit)
            .ToList();
    }

    /// <summary>
    /// Returns the count of a specific fruit type
    /// </summary>
    public int GetCount(FruitDefinition fruit)
    {
        if (fruit == null) return 0;
        return fruitStacks
            .Where(stack => stack.Fruit == fruit)
            .Sum(stack => stack.Count);
    }

    /// <summary>
    /// Returns true if the inventory contains at least one of the specified fruit
    /// </summary>
    public bool Contains(FruitDefinition fruit)
    {
        if (fruit == null) return false;
        return fruitStacks.Any(stack => stack.Fruit == fruit && stack.Count > 0);
    }
}