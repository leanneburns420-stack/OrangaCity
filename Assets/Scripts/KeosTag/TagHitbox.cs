using GorillaLocomotion;
using Photon.Pun;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class TagHitbox : MonoBehaviour, IPunObservable
{
    [Header("Photon Sync")]
    public PhotonView PTView;

    [Header("On Tag")]
    public List<AudioClip> TagSounds;  //Fixed

    [Header("Tag Freeze")]
    public bool UseTagFreeze;
    [Header("The name that it has in the scene")]
    public string GorillPlayerName;
    public float TagFreezeTime;

    [Header("Speed Boost")]
    public bool UseSpeedBoost;
    public float maxJumpSpeed;
    public float jumpMultiplier;
    public float velocityLimit;
    [Header("When Taged")]
    public List<GameObject> TagEnable;  //Fixed
    public List<GameObject> TagDisable;  //Fixed

    [Header("Material Change")]
    public List<SkinnedMeshRenderer> PlayerRender;  //Fixed
    public Material UnTaggedMaterial;
    public Material TaggedMaterial;

    [Header("When Not Taged")]
    public List<GameObject> UnTagEnable; //Fixed
    public List<GameObject> UnTagDisable; //Fixed

    [Header("NONO SPOT")]
    public Player GorillaPlayer;
    public Tagger[] Hands;
    public bool IsTag;
    public AudioSource Player;
    public float OmaxJumpSpeed;
    public float OjumpMultiplier;
    public float OvelocityLimit;



    void Start()
    {
        Player = GetComponent<AudioSource>();
        PTView = GetComponent<PhotonView>();
        Hands = FindObjectsOfType<Tagger>();
        if (UseTagFreeze)
        {
            GorillaPlayer = GameObject.Find(GorillPlayerName).GetComponent<Player>();
        }
        if (UseSpeedBoost)
        {
            OvelocityLimit = GorillaPlayer.velocityLimit;
            OjumpMultiplier = GorillaPlayer.jumpMultiplier;
            OmaxJumpSpeed = GorillaPlayer.maxJumpSpeed;
        }
    }

    [PunRPC]
    public void OnHit()
    {
        if (!IsTag)
        {

            if (TagSounds != null)
            {
                int RIDX = Random.Range(0, TagSounds.Count);
                AudioClip clip = TagSounds[RIDX];
                Player.clip = clip;
                Player.Play();
            }
            IsTag = true;

            foreach (Tagger t in Hands)
            {
                if (t != null)
                {
                    t.IsTag = true;
                }
            }

            if (PlayerRender != null)
            {
                foreach (SkinnedMeshRenderer PR in PlayerRender)
                {
                    if (PR != null)
                    {
                        PR.material = TaggedMaterial;
                    }
                }
            }

            if (UseTagFreeze)
            {
                StartCoroutine(TagFreeze());
            }

            if (UseSpeedBoost)
            {
                AddSpeedBoost();
            }

            ListED(TagEnable, true);
            ListED(TagDisable, false);
        }
    }

    public void ListED(List<GameObject> list, bool State)
    {
        if (list != null)
        {
            foreach (GameObject obj in list)
            {
                if (obj != null)
                {
                    obj.SetActive(State);
                }
            }
        }
    }

    [PunRPC]
    public void EndRound()
    {
        IsTag = false;
        foreach (SkinnedMeshRenderer PR in PlayerRender)
        {
            PR.material = UnTaggedMaterial;
        }
        foreach (Tagger t in Hands)
        {
            if (t != null)
            {
                t.IsTag = false;
            }
        }
        if (UseSpeedBoost)
        {
            RemoveSpeedBoost();
        }
        ListED(UnTagEnable, true);
        ListED(UnTagDisable, false);
    }

    public void AddSpeedBoost()
    {
        GorillaPlayer.maxJumpSpeed = maxJumpSpeed;
        GorillaPlayer.jumpMultiplier = jumpMultiplier;
        GorillaPlayer.velocityLimit = velocityLimit;
    }

    public void RemoveSpeedBoost()
    {
        GorillaPlayer.maxJumpSpeed = OmaxJumpSpeed;
        GorillaPlayer.jumpMultiplier = OjumpMultiplier;
        GorillaPlayer.velocityLimit = OvelocityLimit;
    }

    public void OnPhotonSerializeView(PhotonStream stream, PhotonMessageInfo info)
    {
        if (stream.IsWriting)
        {
            stream.SendNext(IsTag);
        }
        else
        {
            //r.material = IsTag ? TaggedMaterial : UnTaggedMaterial;
            //Old one btw above this line and i hope the syncing is correct (SKIBIDY)
            IsTag = (bool)stream.ReceiveNext();
            foreach (SkinnedMeshRenderer r in PlayerRender)
            {
                r.material = IsTag ? TaggedMaterial : UnTaggedMaterial;
            }
        }
    }

    public IEnumerator TagFreeze()
    {
        GorillaPlayer.disableMovement = true;
        yield return new WaitForSeconds(TagFreezeTime);
        GorillaPlayer.disableMovement = false;
    }
}
