using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using System.Collections;

public class AutoSceneColorFade : MonoBehaviour
{
    public Image fadeImage;               
    public float fadeDuration = 1.5f;    
    public float waitTime = 5f;           

    private void Start()
    {
        fadeImage.color = Color.black;
        StartCoroutine(SceneTransitionRoutine());
    }

    IEnumerator SceneTransitionRoutine()
    {
        yield return new WaitForSeconds(waitTime);

        yield return StartCoroutine(FadeColor(Color.black, Color.red));

        int currentIndex = SceneManager.GetActiveScene().buildIndex;
        int nextIndex = (currentIndex + 1) % SceneManager.sceneCountInBuildSettings;
        SceneManager.LoadScene(nextIndex);
    }

    IEnumerator FadeColor(Color fromColor, Color toColor)
    {
        float time = 0f;
        while (time < fadeDuration)
        {
            fadeImage.color = Color.Lerp(fromColor, toColor, time / fadeDuration);
            time += Time.deltaTime;
            yield return null;
        }
        fadeImage.color = toColor;
    }
}
