using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

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
    public Button playButton;
    public GameObject chestGrid;
    public GameObject chestPrefab;
    private Tween playButtonTween;
    private List<Tween> gridTweens = new List<Tween>();

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
        float buttonSize = 100f;
        float buttonSpacing = buttonSize/2 + 200f;
        for (int i = 0; i < rows; i++)
        {
            for (int j = 0; j < columns; j++)
            {
                // Instantiate the button prefab
                GameObject newButton = Instantiate(chestPrefab);//new GameObject("Button_" + i + "_" + j);
                Button buttonComponent = newButton.GetComponent<Button>();

                RectTransform buttonRect = newButton.GetComponent<RectTransform>();
                buttonRect.sizeDelta = new Vector2(buttonSize, buttonSize); // Size of the button
                buttonRect.anchoredPosition = new Vector2(
                    chestGrid.transform.position.x + (j - (columns - 1) / 2f) * buttonSpacing, // Horizontal position (centered)
                    chestGrid.transform.position.y - (i - (rows - 1) / 2f) * buttonSpacing  // Vertical position (centered)
                );
                // Set the new button's parent to the panel
                newButton.transform.SetParent(chestGrid.transform, false);
                buttonComponent.onClick.AddListener(() => chestClick(newButton)); // Pass row and column as parameters

                //initial tween fade in
                // Initially set the button scale to 0 (scaled down)
                buttonComponent.transform.localScale = Vector3.zero;

                // Create a sequence to combine both fade and scale animations
                Sequence buttonSequence = DOTween.Sequence();

                // Scale the button to its normal size (1, 1, 1) over 1 second
                buttonSequence.Join(buttonComponent.transform.DOScale(Vector3.one, 1f).SetEase(Ease.OutBack));
                buttonSequence.OnComplete(() => {
                    Tween t = buttonComponent.GetComponent<RectTransform>().DOScale(new Vector3(1.05f, 1.05f, 1), 0.5f) 
                    .SetLoops(-1, LoopType.Yoyo)
                    .SetEase(Ease.InOutSine)
                    .SetTarget(newButton);
                    gridTweens.Add(t);
                });
            }    
        }
    }

    public void resetGridButtons(){
        foreach (Tween t in gridTweens)
        {
            t.Kill();
        }
        foreach (Transform child in chestGrid.transform)
        {
            // child.gameObject.SetActive(true);
            Destroy(child.gameObject);
        }
        initGridButtons();
        gridTweens.Clear();
    }

    public void chestClick(GameObject buttonObj) {
        buttonObj.GetComponent<Button>().interactable = false;
        if (buttonObj != null && buttonObj.GetComponent<Button>() != null && inTurn) {
            buttonObj.GetComponent<Button>().GetComponent<RectTransform>().DOScale(new Vector3(0f, 0f, 1), 0.5f)
            .OnComplete(() => buttonObj.SetActive(false));

            float chestValue = round.getNextChestValue();
            if (chestValue == 0 || chestValue < 0f) {
                inTurn = false;
                enableMenu();
                chestValue = 0;
            }
            changeBalance(chestValue);
            setLastWin(chestValue);
        }
    }   

    public void enableMenu(){
        playButton.interactable = true;
        plusDenomination.interactable = true;
        minusDenomination.interactable = true;
        playButtonTween.Play();
    }

    public void disableMenu(){
        playButton.interactable = false;
        plusDenomination.interactable = false;
        minusDenomination.interactable = false;
        playButtonTween.Pause();
    }

    public void initTween(){
        playButton.GetComponent<RectTransform>().localScale = Vector3.one;
        playButtonTween = playButton.GetComponent<RectTransform>().DOScale(new Vector3(1.05f, 1.05f, 1), 0.5f) 
                .SetLoops(-1, LoopType.Yoyo)
                .SetEase(Ease.InOutSine);
    }

    void Start() {
        balanceText.text = "$" + balance.ToString("F2");
        denominationText.text = "$"+denominationArray[denominationIndex].ToString("F2");
        lastWinText.text = "$" + lastWin.ToString("F2");
        
        // initGridButtons();
        initTween();
    }

    public void PlayRound() {
        disableMenu();

        if(inTurn) return;
        inTurn = true;
        resetGridButtons();
        changeBalance(-1*denominationArray[denominationIndex]); //subtracts denomination from balance
        this.round = new Round();
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


