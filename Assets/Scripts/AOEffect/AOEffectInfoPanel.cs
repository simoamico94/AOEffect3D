using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class AOEffectInfoPanel : MonoBehaviour
{
	public TMP_Text processID;
	public TMP_Text height;
	public TMP_Text width;
	public TMP_Text maxEnergy;
	public TMP_Text energyPerSec;
	public TMP_Text health;
	public TMP_Text averageMaxStrengthHitsToKill;
	public TMP_Text waitTime;
	public TMP_Text gameTime;
	public TMP_Text minimumPlayers;
	public TMP_Text paymentQty;
	public TMP_Text bonusQty;

	public Button loadArenaButton;
    AOEffectInfo info;
    
	private void Start()
	{
        loadArenaButton.onClick.AddListener(LoadArena);
	}

	public void UpdateInfo(AOEffectInfo newInfo)
	{
		info = newInfo;
		processID.text = newInfo.processID;
		
		SetTextValue(height, "Height", newInfo.height);
		SetTextValue(width, "Width", newInfo.width);
		SetTextValue(maxEnergy, "Max Energy", newInfo.maxEnergy);
		SetTextValue(energyPerSec, "Energy per Sec", newInfo.energyPerSec);
		SetTextValue(health, "Health", newInfo.health);
		SetTextValue(averageMaxStrengthHitsToKill, "Average Max Strength Hits to Kill", newInfo.averageMaxStrengthHitsToKill);
		SetTextValue(waitTime, "Wait Time (min)", newInfo.waitTime);
		SetTextValue(gameTime, "Game Time (min)", newInfo.gameTime);
		SetTextValue(minimumPlayers, "Minimum Players", newInfo.minimumPlayers);
		SetTextValue(paymentQty, "Payment Quantity", newInfo.paymentQty);
		SetTextValue(bonusQty, "Bonus Quantity", newInfo.bonusQty);
	}

	public void SetTextValue(TMP_Text textComponent, string label, float value)
	{
		if (value != -1)
		{
			textComponent.text = label + ": " + value.ToString();
			textComponent.gameObject.SetActive(true);
		}
		else
		{
			textComponent.gameObject.SetActive(false);
		}
	}

	public void UpdateInfoOld(AOEffectInfo newInfo)
    {
        info = newInfo;

		processID.text = newInfo.processID;

		if (newInfo.height != -1)
		{
			height.text = newInfo.height.ToString();
		}
		else
		{
			height.gameObject.SetActive(false);
		}

		if (newInfo.width != -1)
		{
			width.text = newInfo.width.ToString();
		}
		else
		{
			width.gameObject.SetActive(false);
		}

		if (newInfo.maxEnergy != -1)
		{
			maxEnergy.text = newInfo.maxEnergy.ToString();
		}
		else
		{
			maxEnergy.gameObject.SetActive(false);
		}

		if (newInfo.energyPerSec != -1)
		{
			energyPerSec.text = newInfo.energyPerSec.ToString();
		}
		else
		{
			energyPerSec.gameObject.SetActive(false);
		}

		if (newInfo.health != -1)
		{
			health.text = newInfo.health.ToString();
		}
		else
		{
			health.gameObject.SetActive(false);
		}

		if (newInfo.averageMaxStrengthHitsToKill != -1)
		{
			averageMaxStrengthHitsToKill.text = newInfo.averageMaxStrengthHitsToKill.ToString();
		}
		else
		{
			averageMaxStrengthHitsToKill.gameObject.SetActive(false);
		}

		if (newInfo.waitTime != -1)
		{
			waitTime.text = newInfo.waitTime.ToString();
		}
		else
		{
			waitTime.gameObject.SetActive(false);
		}

		if (newInfo.gameTime != -1)
		{
			gameTime.text = newInfo.gameTime.ToString();
		}
		else
		{
			gameTime.gameObject.SetActive(false);
		}

		if (newInfo.minimumPlayers != -1)
		{
			minimumPlayers.text = newInfo.minimumPlayers.ToString();
		}
		else
		{
			minimumPlayers.gameObject.SetActive(false);
		}

		if (newInfo.paymentQty != -1)
		{
			paymentQty.text = newInfo.paymentQty.ToString();
		}
		else
		{
			paymentQty.gameObject.SetActive(false);
		}

		if (newInfo.bonusQty != -1)
		{
			bonusQty.text = newInfo.bonusQty.ToString();
		}
		else
		{
			bonusQty.gameObject.SetActive(false);
		}

	}

	public void LoadArena()
    {
        AOEffectManager.main.canvasManager.LoadAvailableArena(info.processID);
    }
}
