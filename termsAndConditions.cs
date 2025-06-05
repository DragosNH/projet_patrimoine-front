using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Networking;
using System.Collections;
using UnityEngine.UI;
using TMPro;
using Unity.VisualScripting;


public class TermsAndConditions : MonoBehaviour
{
    public void ReturnToSignUp()
    {
        SceneManager.LoadScene("SignUp");
    }
}