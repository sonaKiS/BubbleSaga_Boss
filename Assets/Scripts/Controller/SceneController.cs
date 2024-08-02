using Michsky.UI.Shift;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

public class SceneController : MonoBehaviour
{
    public static string nextScene;
    [SerializeField] Transform imgCircle;

    // Start is called before the first frame update
    private void Start()
    {
        StartCoroutine(LoadScene());
    }

    // Update is called once per frame
    void Update()
    {
        imgCircle.Rotate(Vector3.forward * 15f * Time.deltaTime) ;
    }

    public static void LoadScene(string scene)
    {
        nextScene = scene;
        SceneManager.LoadScene("Loading");
    }

    IEnumerator LoadScene()
    {
        AsyncOperation op;
        op = SceneManager.LoadSceneAsync(nextScene);
        op.allowSceneActivation = false;
        
        while(!op.isDone)
        {
            Debug.Log($"Loading:{op.progress}");
            yield return null;

            if(op.progress > 0.89f)
            {
                yield return new WaitForSeconds(1f);
                op.allowSceneActivation = true;
            }
        }

        

        
    }
}
