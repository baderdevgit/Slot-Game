using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class LogicScript : MonoBehaviour
{
    public static float balance = 10f;
    public static float lastWin = 0f;
    public static int denominationIndex = 0;
    public static float[] denominationArray = {0.25f, 0.5f, 1f, 5f};
    public static bool inTurn = false;
    public Round round;

    public TMP_Text balanceText;
    public TMP_Text lastWinText;

    public Text denominationText; 
    public Button plusDenomination;
    public Button minusDenomination;
    public Button playButton;
    public GameObject chestGrid;
    public GameObject chestPrefab;
    public GameObject receiptPrefab;
    public GameObject mainPanel;
    public GameObject balancePanel;
    public GameObject denominationPanel;
    private Tween playButtonTween;
    private List<Tween> gridTweens = new List<Tween>();

    public void changeBalance(float amount){
        balance += amount;
        // balanceText.text = "$" + balance.ToString("F2");
        DOTween.To(() => float.Parse(balanceText.text.Substring(1)), 
                    x => balanceText.text = "$" + x.ToString("F2"), 
                    balance, 
                    1.0f);

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
        
         lastWinText.transform.DOScale(0.5f, 0.25f)
            .OnComplete(() => 
            {
                if(amount != 0){
                    lastWinText.DOColor(Color.green, 0.5f)
                    .OnComplete(() => 
                    {
                        // Once color change to green is complete, change it back to white
                        lastWinText.DOColor(Color.white, 0.5f);
                    });
                } else {
                    lastWinText.DOColor(Color.red, 0.5f)
                    .OnComplete(() => 
                    {
                        // Once color change to green is complete, change it back to white
                        lastWinText.DOColor(Color.white, 0.5f);
                    });
                }
                
                lastWinText.text = "$" + lastWin.ToString("F2");
                // Once scaling down is complete, scale it back up quickly to original size
                lastWinText.transform.DOScale(1f, 0.25f);
            });
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

    public void clearGridButtons(){
        foreach (Tween t in gridTweens)
        {
            t.Kill();
        }
        foreach (Transform child in chestGrid.transform)
        {
            GameObject buttonObj = child.gameObject;
            buttonObj.GetComponent<Button>().GetComponent<RectTransform>().DOScale(new Vector3(0f, 0f, 1), 0.5f)
            .OnComplete(() => {
                Destroy(child.gameObject);
            });
        }
        gridTweens.Clear();
    }
    public void resetGridButtons(){
        clearGridButtons();
        initGridButtons();
    }

    public void spawnReceipt(float amount) {
        string[] losingTexts = {
            "there was a finger in the chili...",
            "u forgot my side enchilada :(",
            "I'm broke",
            "I wasn't even hungry",
            "I have a boyfriend",
            "I don't morally agree with tipping",
            "here's a tip, brush your teeth",
            "I don't want to tip, I want to be rich",
            "Im neva steppin foot back in this establishment",
            "I'd rather eat dog food off the floor, that was aweful",
            "You gave me the wrong order >:(",
            "I found a hair in the salsa",
            "I'm going to chipotle next time",
            "The food took too long",
            "I said no pickles and there was pickles!",
            "Clean the bathrooms",
        };

        string[] winningTexts = {
            "don't spend it all in one place!",
            "great food!",
            "my friends looking so I had to tip",
            "thanks for not spitting in my food again",
            "exccellent service",
            "there's more where that came from ;)",
            "I see a bright future in you",
            "I've got too much money",
            "Im using the company card",
            "I'll be back!",
            "You've earned it",
            "Call me ;)",
            "You're gonna go far kid",
            "That salsa was amazing",
            "Gracias"

        };

        GameObject r = Instantiate(receiptPrefab);
        r.transform.SetParent(mainPanel.transform, true);
        r.transform.localScale = Vector3.one;
        Image bgImage = r.GetComponent<Image>();
        if (bgImage != null) {
            bgImage.enabled = true;
        }

        Text tipValue = r.transform.Find("tip_value").GetComponent<Text>();
        tipValue.text = "$" + amount.ToString("F2");

        TMP_Text handwritting = r.transform.Find("handwritting").GetComponent<TMP_Text>();
        if(amount == 0){
            handwritting.color = Color.red;
            handwritting.text = losingTexts[Random.Range(0, losingTexts.Length)];
        } else {
            handwritting.color = Color.blue;
            handwritting.text = winningTexts[Random.Range(0, winningTexts.Length)]; 
        }
        handwritting.maxVisibleCharacters = 0;

        // Add a CanvasGroup if the panel doesn't already have one
        CanvasGroup canvasGroup = r.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = r.AddComponent<CanvasGroup>();
        }

        // Set initial alpha to 0 (invisible)
        canvasGroup.alpha = 0f;

        // Get the panel's RectTransform (to adjust its position)
        RectTransform rectTransform = r.GetComponent<RectTransform>();

        // Store the initial position of the panel
        Vector3 initialPosition = rectTransform.anchoredPosition;

        // Create a DoTween sequence for fading in, moving up, waiting, and fading out
        Sequence sequence = DOTween.Sequence();

        // Fade-in over 1 second while moving the panel up
        sequence.Append(canvasGroup.DOFade(1f, 1f)); // Fade-in the panel
        sequence.Join(rectTransform.DOAnchorPosY(initialPosition.y + 50f, 1f)); // Move up by 50 units

        // Reveal text at the same time as the panel fade-in and move
        handwritting.ForceMeshUpdate();
        int totalCharacters = handwritting.textInfo.characterCount;

        sequence.Join(DOTween.To(() => handwritting.maxVisibleCharacters, x => handwritting.maxVisibleCharacters = x, totalCharacters, totalCharacters * 0.05f));

        // Add a 2-second delay after the text is fully revealed
        sequence.AppendInterval(1f); // Wait for 2 seconds after typing is done

        // Fade-out over 1 second while moving the panel up by another 50 units
        sequence.Append(canvasGroup.DOFade(0f, 1f)); // Fade-out the panel
        sequence.Join(rectTransform.DOAnchorPosY(initialPosition.y + 100f, 1f)); // Move up another 50 units

        // Destroy the Panel after fading out
        sequence.OnComplete(() => 
        {
            Destroy(r);
            if(amount == 0) {
                inTurn = false;
                clearGridButtons();
                enableMenu();
            }

        });
    }

    public void chestClick(GameObject buttonObj) {
        if (buttonObj != null && buttonObj.GetComponent<Button>() != null && inTurn) {
            buttonObj.GetComponent<Button>().interactable = false;
            buttonObj.GetComponent<Button>().GetComponent<RectTransform>().DOScale(new Vector3(0f, 0f, 1), 0.5f)
            .OnComplete(() => buttonObj.SetActive(false));

            float chestValue = round.getNextChestValue();
            if (chestValue == 0 || chestValue < 0f) {
                inTurn = false;
                chestValue = 0;
            }
            spawnReceipt(chestValue);

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
        float onScreenX = balancePanel.GetComponent<RectTransform>().anchoredPosition.x;
        balancePanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(balancePanel.GetComponent<RectTransform>().anchoredPosition.x - 200f, balancePanel.GetComponent<RectTransform>().anchoredPosition.y);
        balancePanel.GetComponent<RectTransform>().DOAnchorPosX(onScreenX, 1f).SetEase(Ease.OutQuad);

        float onScreenX2 = denominationPanel.GetComponent<RectTransform>().anchoredPosition.x;
        denominationPanel.GetComponent<RectTransform>().anchoredPosition = new Vector2(denominationPanel.GetComponent<RectTransform>().anchoredPosition.x + 200f, denominationPanel.GetComponent<RectTransform>().anchoredPosition.y);
        denominationPanel.GetComponent<RectTransform>().DOAnchorPosX(onScreenX2, 1f).SetEase(Ease.OutQuad);
    }

    void Start() {
        balanceText.text = "$" + balance.ToString("F2");
        denominationText.text = "$"+denominationArray[denominationIndex].ToString("F2");
        lastWinText.text = "$" + lastWin.ToString("F2");
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


