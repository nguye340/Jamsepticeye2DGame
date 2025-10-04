using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using TMPro;

public class SimpleFruitChoiceUI : MonoBehaviour
{
    public static SimpleFruitChoiceUI Instance { get; private set; }

    [Header("UI References")]
    public GameObject choicePanel;
    public Transform buttonContainer;
    public Button buttonPrefab;
    public TextMeshProUGUI titleText;

    private System.Action<FruitDefinition> onFruitSelected;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            choicePanel.SetActive(false);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    public void ShowFruitChoice(List<FruitDefinition> fruits, string title, System.Action<FruitDefinition> callback)
    {
        if (fruits == null || fruits.Count == 0 || callback == null)
        {
            Debug.LogError("Invalid parameters for ShowFruitChoice");
            return;
        }

        // Clear existing buttons
        foreach (Transform child in buttonContainer)
        {
            if (child != buttonContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // Set title
        if (titleText != null)
        {
            titleText.text = title;
        }

        // Create buttons
        foreach (var fruit in fruits)
        {
            if (fruit == null) continue;

            var button = Instantiate(buttonPrefab, buttonContainer);
            var buttonText = button.GetComponentInChildren<TextMeshProUGUI>();
            
            if (buttonText != null)
            {
                buttonText.text = fruit.name;
            }

            // Store fruit in local variable to avoid closure issues
            var currentFruit = fruit;
            button.onClick.AddListener(() => OnFruitSelected(currentFruit));
        }

        onFruitSelected = callback;
        choicePanel.SetActive(true);
        Time.timeScale = 0f;
    }

    private void OnFruitSelected(FruitDefinition fruit)
    {
        choicePanel.SetActive(false);
        Time.timeScale = 1f;
        onFruitSelected?.Invoke(fruit);
    }

    public void Cancel()
    {
        choicePanel.SetActive(false);
        Time.timeScale = 1f;
        onFruitSelected = null;
    }
}