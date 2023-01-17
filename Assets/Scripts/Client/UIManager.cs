using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public delegate void OnFindMatchPressedDelegate();
    public OnFindMatchPressedDelegate OnFindMatchPressedHandler;

    private Button _findMatchButton;
    private MatchButtonStatusEnum _matchButtonStatus = MatchButtonStatusEnum.FindMatch;
    private enum MatchButtonStatusEnum
    {
        FindMatch,
        Searching,
        Found,
        HoldLastStatus
    }

    // Kick off the match making service
    public void OnFindMatchPressed()
    {
        _matchButtonStatus = MatchButtonStatusEnum.Searching;

        if (OnFindMatchPressedHandler != null)
        {
            // execute the handler for find match
            OnFindMatchPressedHandler();
        }
        else
        {
            throw new System.Exception("OnFindMatchPressedHandler was null, should be set with Init function.");
        }
    }

    public void SetNonInteractiveFindMatchButtonStatus(string buttonText)
    {
        _matchButtonStatus = MatchButtonStatusEnum.HoldLastStatus;
        _findMatchButton.enabled = false;
        _findMatchButton.GetComponentInChildren<TMPro.TextMeshProUGUI>().text = buttonText;
    }

    public void Init(OnFindMatchPressedDelegate onFindMatchPressedDelegate)
    {
        _findMatchButton = GameObject.Find("FindMatch").GetComponent<Button>();
        _findMatchButton.onClick.AddListener(OnFindMatchPressed);
        OnFindMatchPressedHandler = onFindMatchPressedDelegate;
    }

    void Update()
    {
        if (_matchButtonStatus == MatchButtonStatusEnum.Searching)
        {
            SetNonInteractiveFindMatchButtonStatus("Searching...");
        }
        if (_matchButtonStatus == MatchButtonStatusEnum.Found)
        {
            SetNonInteractiveFindMatchButtonStatus("Match Found!");
        }
    }

    void Start()
    {
        // Debug.Log("UIManager start");

        // NOTE: because this isn't available during the Startup scene, when the ClientGameManager
        // is attempting to access it, we must not rely on the Start to initialize
        // things.  ClientGameManager leverages the local scene change to MainScene
        // to init our delegates and callbacks.
    }
}
