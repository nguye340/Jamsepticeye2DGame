using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Systems;

public class FruitInventory : MonoBehaviour
{
    // Event that's triggered when a fruit is removed from the inventory
    public event Action<FruitDefinition> OnFruitRemoved;
    public static FruitInventory Instance { get; private set; }

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

    /// <summary>
    /// Removes all fruits from the inventory
    /// </summary>
    private void Awake()
    {
        // Singleton pattern
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else if (Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        
        // Initialize fruit stacks if empty
        if (fruitStacks == null)
        {
            fruitStacks = new List<FruitStackItem>();
        }
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
    
    public int TotalCount()
    {
        return fruitStacks.Sum(stack => stack.Count);
    }
    
    /// <summary>
    /// Checks if the inventory contains a fruit with the specified name (case insensitive)
    /// </summary>
    public bool HasFruit(string fruitName)
    {
        if (string.IsNullOrEmpty(fruitName)) return false;
        
        return fruitStacks.Any(stack => 
            stack.Fruit != null && 
            !string.IsNullOrEmpty(stack.Fruit.name) &&
            stack.Fruit.name.IndexOf(fruitName, StringComparison.OrdinalIgnoreCase) >= 0 &&
            stack.Count > 0);
    }
    
    public bool RemoveOne(FruitDefinition fruit)
    {
        var stack = fruitStacks.FirstOrDefault(s => s.Fruit == fruit);
        if (stack != null && stack.Count > 0)
        {
            stack.Count--;
            bool wasRemoved = stack.Count <= 0;
            
            // Notify listeners that a fruit was removed
            OnFruitRemoved?.Invoke(fruit);
            
            if (wasRemoved)
            {
                fruitStacks.Remove(stack);
            }
            return true;
        }
        return false;
    }
    
    public FruitDefinition RemoveOneRandomFruit()
    {
        if (fruitStacks.Count == 0) return null;
        
        // Get a random stack with count > 0
        var validStacks = fruitStacks.Where(s => s.Count > 0).ToList();
        if (validStacks.Count == 0) return null;
        
        var randomStack = validStacks[UnityEngine.Random.Range(0, validStacks.Count)];
        var fruit = randomStack.Fruit;
        randomStack.Count--;
        
        // Notify listeners that a fruit was removed
        OnFruitRemoved?.Invoke(fruit);
        
        if (randomStack.Count <= 0)
        {
            fruitStacks.Remove(randomStack);
        }
        
        return fruit;
    }

    /// <summary>
    /// Returns true if the inventory contains at least one of the specified fruit
    /// </summary>
    public bool Contains(FruitDefinition fruit)
    {
        if (fruit == null) return false;
        return fruitStacks.Any(stack => stack.Fruit == fruit && stack.Count > 0);
    }
    
    
    /// <summary>
    /// Gets a fruit definition by its name
    /// </summary>
    public FruitDefinition GetFruitDefinitionByName(string fruitName)
    {
        if (string.IsNullOrEmpty(fruitName)) return null;
        var stack = fruitStacks.FirstOrDefault(s => 
            s.Fruit != null && 
            s.Fruit.name == fruitName && 
            s.Count > 0);
        return stack?.Fruit;
    }
}