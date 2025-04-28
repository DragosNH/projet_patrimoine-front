using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;


public class SuccessResetPage : MonoBehaviour
{ 

    public void ReturnToMainPage()
    {
        SceneManager.LoadScene("MainLogin");
    }

}