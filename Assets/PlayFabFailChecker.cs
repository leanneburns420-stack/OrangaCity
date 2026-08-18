using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayFabFailChecker : MonoBehaviour
{
    private bool hasChecked = false;

    void Start()
    {
        StartCoroutine(CheckForFailure());
    }

    IEnumerator CheckForFailure()
    {
        yield return new WaitForSeconds(5f);

        if (!hasChecked)
        {
            hasChecked = true;

            if (Playfablogin.instance == null || string.IsNullOrEmpty(Playfablogin.instance.MyPlayFabID))
            {
                Debug.LogWarning("PlayFab Check Failed Reloading Scene");
                SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            }
            else
            {
                Debug.Log("PlayFab ID check passed! No reload needed.");
            }
        }
    }
}
