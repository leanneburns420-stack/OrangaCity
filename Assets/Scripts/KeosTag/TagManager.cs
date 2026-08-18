using Photon.Pun;
using UnityEngine;
using System.Collections;
using Photon.VR;
using System.Collections.Generic;

public class TagManager : MonoBehaviour, IPunObservable
{
    public PhotonView PTView;
    public float EndTime;
    public int PeopleNeedToStartRound;
    public string QueueName;
    public PhotonVRManager PTManager;


    [Header("NONO SPOT")]
    public bool IsMaster;
    public TagHitbox[] HitBoxes;
    public int TaggedPeople;
    public bool CanStartNewRound = true;
    public int RandomStartPlayerNumb = 69696969;
    public TagHitbox NewTaggedPlayer;
    public int TotalPlayers;
    public bool IsInTagQueue;
    public List<Tagger> TaggingHands;

    public void CheckIfRoundShouldStart()
    {
        if (TaggedPeople == 0 && CanStartNewRound && TotalPlayers >= PeopleNeedToStartRound)
        {
            StartCoroutine(StartRound());
        }
    }

    public IEnumerator StartRound()
    {
        if (CanStartNewRound)
        {
            if (RandomStartPlayerNumb == 69696969)
            {
                RandomStartPlayerNumb = Random.Range(0, HitBoxes.Length);
                NewTaggedPlayer = HitBoxes[RandomStartPlayerNumb];
            }
            if (NewTaggedPlayer != null)
            {
                PhotonView newTaggedPV = NewTaggedPlayer.GetComponent<PhotonView>();
                if (newTaggedPV != null)
                {
                    newTaggedPV.RPC(nameof(TagHitbox.OnHit), RpcTarget.AllBuffered);
                }
            }
        }
        yield return null;
    }

    private void DoTheThingICanNotComeUpWithAnName(bool State)
    {
        foreach (Tagger t in TaggingHands)
        {
            t.enabled = State;
        }

        HitBoxes = FindObjectsOfType<TagHitbox>();

        foreach (TagHitbox t in HitBoxes)
        {
            t.enabled = State;
        }
    }

    private void FixedUpdate()
    {
        if (IsInTagQueue)
        {
            CheckIfMaster();
            if (IsMaster)
            {
                TotalPlayers = PhotonNetwork.CurrentRoom.Players.Count;
                CheckTaggedPlayers();
                CheckIfRoundShouldEnd();
                CheckIfRoundShouldStart();
            }
        }
        if (PTManager.DefaultQueue == QueueName)
        {
            DoTheThingICanNotComeUpWithAnName(true);
            IsInTagQueue = true;
        }
        else
        {
            DoTheThingICanNotComeUpWithAnName(false);
            IsInTagQueue = false;
        }
    }

    public void CheckTaggedPlayers()
    {
        TaggedPeople = 0;
        HitBoxes = FindObjectsOfType<TagHitbox>();
        foreach (TagHitbox t in HitBoxes)
        {
            if (t.IsTag)
            {
                TaggedPeople++;
            }
        }
    }

    public void CheckIfRoundShouldEnd()
    {
        if (TotalPlayers <= TaggedPeople)
        {
            EndRound();
        }
    }

    public void EndRound()
    {
        RandomStartPlayerNumb = 69696969;
        foreach (TagHitbox t in HitBoxes)
        {
            PhotonView hitboxPV = t.GetComponent<PhotonView>();
            if (hitboxPV != null)
            {
                hitboxPV.RPC(nameof(TagHitbox.EndRound), RpcTarget.AllBuffered);
            }
        }
        StartCoroutine(EndRoundWait());
    }

    public void CheckIfMaster()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            Debug.Log("I Like Men");
            IsMaster = true;
        }
        else
        {
            IsMaster = false;
            Debug.Log("I Dont Like Men");
        }
    }

    // Only there cause of the syncing with IPunObservable
    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        //Oh yeah skibidy
    }

    private IEnumerator EndRoundWait()
    {
        CanStartNewRound = false;
        yield return new WaitForSeconds(EndTime);
        CanStartNewRound = true;
    }
}
