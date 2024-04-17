using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class LoadLuaElement : MonoBehaviour
{
	[SerializeField] private TMP_Text titleText;
	[SerializeField] private Button button;

	private string luaText;

	private TMP_InputField customLuaInputField;

	void Start()
    {
		button.onClick.AddListener(LoadLua);
		customLuaInputField = GetComponentInChildren<TMP_InputField>();
	}

	public void SetInfo(string title, string lua)
	{
		titleText.text = title;
		luaText = lua;
	}

	public void LoadLua()
	{
		if(customLuaInputField != null)
		{
			luaText = customLuaInputField.text;
		}

		AOConnectManager.main.LoadLua(luaText);
	}
}
