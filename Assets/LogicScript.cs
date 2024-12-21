using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class LogicScript : MonoBehaviour
{
    public static float balance = 10f;
    public static float lastWin = 0f;
    public static int denominationIndex = 0;
    public static float[] denominationArray = {0.25f, 0.5f, 1f, 5f};
    public static bool inTurn = false;
    public Round round;

    public Text balanceText;
    public Text lastWinText;
    public Text denominationText; 
    public Button plusDenomination;
    public Button minusDenomination;
    public GameObject chestGrid;
    public GameObject chestPrefab;

    public void changeBalance(float amount){
        balance += amount;
        balanceText.text = "$" + balance.ToString("F2");
    }

    public void increaseDenomination(){
        if(denominationIndex < denominationArray.Length - 1 && !inTurn){
            denominationIndex++;
            denominationText.text = "$"+denominationArray[denominationIndex].ToString("F2");
        }
    }

    public void decreaseDenomination(){
        if(denominationIndex > 0 && !inTurn){
            denominationIndex--;
            denominationText.text = "$"+denominationArray[denominationIndex].ToString("F2");
        }
    }

    public void setLastWin(float amount){
        lastWin = amount;
        lastWinText.text = "$" + lastWin.ToString("F2");
    }

    public void initGridButtons(){
        int rows = 3;
        int columns = 3;
        float buttonSize = 150f;
        float buttonSpacing = buttonSize/2 + 150f;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                // Instantiate the button prefab
                GameObject newButton = new GameObject("Button_" + i + "_" + j);
                Button buttonComponent = newButton.AddComponent<Button>();
                Image buttonImage = newButton.AddComponent<Image>();
                buttonImage.color = Color.white;

                RectTransform buttonRect = newButton.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize); // Size of the button
                buttonRect.anchoredPosition = new Vector2(
                    chestGrid.transform.position.x + (j - (columns - 1) / 2f) * buttonSpacing, // Horizontal position (centered)
                    chestGrid.transform.position.y - (i - (rows - 1) / 2f) * buttonSpacing  // Vertical position (centered)
                );
                // Set the new button's parent to the panel
                newButton.transform.SetParent(chestGrid.transform, false);

                buttonComponent.onClick.AddListener(() => chestClick(newButton)); // Pass row and column as parameters
            }
        }
    }

    public void resetGridButtons(){
        foreach (Transform child in chestGrid.transform)
        {
            child.gameObject.SetActive(true);
        }
    }

    public void chestClick(GameObject buttonObj) {
        if (buttonObj != null && buttonObj.GetComponent<Button>() != null && inTurn) {
            buttonObj.SetActive(false);

            float chestValue = round.getNextChestValue();
            if (chestValue == 0) {
                inTurn = false;
            }
            changeBalance(chestValue);
            setLastWin(chestValue);
        }
    }   

    void Start() {
        balanceText.text = "$" + balance.ToString("F2");
        denominationText.text = "$"+denominationArray[denominationIndex].ToString("F2");
        lastWinText.text = "$" + lastWin.ToString("F2");
        initGridButtons();
    }

    public void PlayRound() {
        if(inTurn) return;
        inTurn = true;
        resetGridButtons();
        changeBalance(-1*denominationArray[denominationIndex]); //subtracts denomination from balance
        this.round = new Round();
        changeBalance(round.winAmount);                               //adds win amount to balance
    }

    
    public class Round {
        public static int[][] multipliers = {
            new int[] { 0 },
            new int[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 },
            new int[] { 12, 16, 24, 32, 48, 64 },
            new int[] { 100, 200, 300, 400, 500 }
        };
        public static float[] probabilities = {0.5f, 0.8f, 0.95f, 1.0f};
        public int winMultiplier;
        public float winAmount;
        public List<float> chests;
        public int chestIndex = 0;

        public Round() {
            this.winMultiplier = getWinMultiplier();
            this.winAmount = (LogicScript.denominationArray[LogicScript.denominationIndex] * winMultiplier);
            this.winAmount = Mathf.Round(winAmount / 0.05f) * 0.05f;

            //sets the chest values
            if(winAmount != 0){
                this.chests = DistributePrize(winAmount, Random.Range(1,9));
            } else {
                this.chests = new List<float>();
            }
            this.chests.Add(0f);


            // foreach(float chest in chests){
            //     Debug.Log(chest);
            // }
            // Debug.Log("Won "+winMultiplier+"x for a total of $"+winAmount);
        }

        int getWinMultiplier(){
            float rand = Random.Range(0f, 1f);
            if(rand < probabilities[0]){            //50% chance
                return multipliers[0][0];
            } else if (rand < probabilities[1]){    //30% chance
                return multipliers[1][Random.Range(0, multipliers[1].Length)];
            } else if (rand < probabilities[2]){    //15% chance
                return multipliers[2][Random.Range(0, multipliers[2].Length)];
            } else {                                //5% chance
                return multipliers[3][Random.Range(0, multipliers[3].Length)];
            }
        }

        List<float> DistributePrize(float totalAmount, int numChests)
        {
            // Step 1: Calculate the base amount for each chest
            float baseAmount = totalAmount / numChests;
            List<float> chestValues = new List<float>();

            // Step 2: Generate random deltas and adjust the base amount
            float totalDistributed = 0f;
            for (int i = 0; i < numChests; i++)
            {
                // Small random adjustment, but ensuring it doesn't over-distribute
                float delta = Random.Range(-baseAmount / 4, baseAmount / 4);
                float chestValue = baseAmount + delta;

                // Ensure chest value doesn't go below 0
                chestValue = Mathf.Max(chestValue, 0.05f); // Minimum prize to avoid negative or zero values

                chestValues.Add(RoundToNearest05(chestValue));
                totalDistributed += chestValues[i];
            }

            // Step 3: Adjust the last chest to ensure the total matches exactly
            float roundingError = totalAmount - totalDistributed;
            chestValues[numChests - 1] = RoundToNearest05(chestValues[numChests - 1] + roundingError);

            return chestValues;
        }

        // Helper method to round the value to the nearest 0.05
        float RoundToNearest05(float value)
        {
            return Mathf.Round(value / 0.05f) * 0.05f;
        }
        public float getNextChestValue() {
            float rv = chests[chestIndex];
            chestIndex++;
            return rv;
        }
    }
}


