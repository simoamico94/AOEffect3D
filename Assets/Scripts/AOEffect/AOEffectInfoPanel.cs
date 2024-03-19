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
		height.text = newInfo.height.ToString();
		width.text = newInfo.width.ToString();
		maxEnergy.text = newInfo.maxEnergy.ToString();
		energyPerSec.text = newInfo.energyPerSec.ToString();
		health.text = newInfo.health.ToString();
		averageMaxStrengthHitsToKill.text = newInfo.averageMaxStrengthHitsToKill.ToString();
		waitTime.text = newInfo.waitTime.ToString();
		gameTime.text = newInfo.gameTime.ToString();
		minimumPlayers.text = newInfo.minimumPlayers.ToString();
		paymentQty.text = newInfo.paymentQty.ToString();
		bonusQty.text = newInfo.bonusQty.ToString();
	}

	public void LoadArena()
    {
        AOEffectManager.main.canvasManager.LoadAvailableArena(info.processID);
    }
}
