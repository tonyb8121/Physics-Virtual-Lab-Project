using UnityEngine;
using UnityEngine.SceneManagement;
public class MenuController : MonoBehaviour
{
    public void LoadPendulum()
    {
        SceneManager.LoadScene("Pendulum");
    }
    public void LoadProjectile()
    {
        SceneManager.LoadScene("Projectile");
    }
    public void BackHome(){
        SceneManager.LoadScene("Home");
           }

    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("Game Exited");
    }
}
