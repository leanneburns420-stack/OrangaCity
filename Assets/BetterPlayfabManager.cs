/*
* Copyright (c) Tyegurr 2025
* 
* Permission is hereby granted, free of charge, to any person obtaining a copy
* of this software and associated documentation files (the "Software"), to deal
* in the Software without restriction, including without limitation the rights
* to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
* copies of the Software, and to permit persons to whom the Software is
* furnished to do so, subject to the following conditions:
* 
* The above copyright notice and this permission notice shall be included in all
* copies or substantial portions of the Software.
* 
* THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
* IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
* FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
* AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
* LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
* OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
* SOFTWARE.
*/
using PlayFab;
using PlayFab.ClientModels;
using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct PlayfabLegacyInventoryItem
{
    public string ID;
    public string DisplayName;
    public string ClassName;
    [TextArea]
    public string Description;
    public uint UnitPrice;
}
[Serializable]
public class PlayfabUserClientInfo
{
    public string UserID;
    public string DisplayName;
    public Dictionary<string, int> VirtualCurrency = new Dictionary<string, int>();
}
public class BetterPlayfabManager : MonoBehaviour
{
    public static BetterPlayfabManager Instance { get; private set; }
    public bool InitializeOnAwake = true;
    [Tooltip("How many attempts can be made per API call.")]
    public int MaxAttemptsPerAPICall = 5;
    private int m_CurrentAPICallAttempt = 1;
    private bool m_FirstRun = true;

    public PlayfabUserClientInfo ClientInfo { get; private set; }

    public List<PlayfabLegacyInventoryItem> InventoryItems { get; private set; } = new List<PlayfabLegacyInventoryItem>();

    private void Start()
    {
        if (Instance == null)
        {
            Instance = this;
        } else
        {
            Debug.LogError("Only one BetterPlayfabManager can exist at a time!");
            Destroy(gameObject);
        }
    }

    private void Awake()
    {
        if (InitializeOnAwake) AttemptLogin();
    }

    private void GlobalHandleError(PlayFabError Error)
    {
        if (Error.Error == PlayFabErrorCode.AccountBanned)
        {
            // whatever you want to do when the plyer is banned can go here
            Debug.LogError("FAiled to authenticate because the user is banned.");
        }
    }

    public void AttemptLogin()
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            var LoginRequest = new LoginWithCustomIDRequest
            {
                CustomId = SystemInfo.deviceUniqueIdentifier,
                CreateAccount = true
            };
            PlayFabClientAPI.LoginWithCustomID(LoginRequest, OnLoginSuccess, OnLoginError);
        }
    }
    private void OnLoginSuccess(LoginResult Result)
    {
        m_CurrentAPICallAttempt = 1;
        ClientInfo = new PlayfabUserClientInfo
        {
            UserID = Result.PlayFabId,
            DisplayName = ""
        };
        if (m_FirstRun) AttemptGetAccountInfo();
    }
    private void OnLoginError(PlayFabError Error)
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            m_CurrentAPICallAttempt++;
            AttemptLogin();
        } else
        {
            Debug.LogError("[BetterPlayfabManager] FATAL ERROR || Failed to login. Error Message: " + Error.ErrorMessage);
            GlobalHandleError(Error);
        }
    }


    public void AttemptGetUserInventory()
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            var InventoryRequest = new GetUserInventoryRequest { };
            PlayFabClientAPI.GetUserInventory(InventoryRequest, OnGetUserInventorySuccess, OnGetUserInventoryError);
        }
    }
    private void OnGetUserInventorySuccess(GetUserInventoryResult Result)
    {
        m_CurrentAPICallAttempt = 1;
        for (int i = 0; i < Result.Inventory.Count; i++)
        {
            ItemInstance Item = Result.Inventory[i];

            PlayfabLegacyInventoryItem ItemToAdd = new PlayfabLegacyInventoryItem
            {
                ID = Item.ItemId,
                DisplayName = Item.DisplayName,
                ClassName = Item.ItemClass,
                Description = Item.Annotation,
                UnitPrice = Item.UnitPrice
            };
            if (!InventoryItems.Contains(ItemToAdd)) InventoryItems.Add(ItemToAdd);
        }

        ClientInfo.VirtualCurrency = Result.VirtualCurrency;

        if (m_FirstRun) m_FirstRun = false;
    }
    private void OnGetUserInventoryError(PlayFabError Error)
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            m_CurrentAPICallAttempt++;
            AttemptGetUserInventory();
        }
        else
        {
            Debug.LogError("[BetterPlayfabManager] FATAL ERROR || Failed to get user inventory. Error Message: " + Error.ErrorMessage);
        }
    }

    public void AttemptGetAccountInfo()
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            var AccountInfoRequest = new GetAccountInfoRequest
            {
                PlayFabId = ClientInfo.UserID
            };
            PlayFabClientAPI.GetAccountInfo(AccountInfoRequest, OnAccountInfoSuccess, OnAccountInfoError);
        }
    }
    private void OnAccountInfoSuccess(GetAccountInfoResult Result)
    {
        m_CurrentAPICallAttempt = 1;

        ClientInfo.UserID = Result.AccountInfo.PlayFabId;
        ClientInfo.DisplayName = Result.AccountInfo.Username;

        if (m_FirstRun) AttemptGetUserInventory();
    }
    private void OnAccountInfoError(PlayFabError Error)
    {
        if (m_CurrentAPICallAttempt < MaxAttemptsPerAPICall)
        {
            m_CurrentAPICallAttempt++;
            AttemptGetAccountInfo();
        }
        else
        {
            Debug.LogError("[BetterPlayfabManager] FATAL ERROR || Failed to get account info. Error Message: " + Error.ErrorMessage);
        }
    }

    // static methods
    public static List<PlayfabLegacyInventoryItem> GetItemsByClassName(string ClassName)
    {
        return Instance.InventoryItems.FindAll(x => x.ClassName == ClassName);
    }
    public static PlayfabLegacyInventoryItem GetItemByID(string ID)
    {
        return Instance.InventoryItems.Find(x => x.ID == ID);
    }
}